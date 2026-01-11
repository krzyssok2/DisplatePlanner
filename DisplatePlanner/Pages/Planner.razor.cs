using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;

namespace DisplatePlanner.Pages;

public partial class Planner(
    IClipboardService clipboardService,
    ISelectionService selectionService,
    IAlignmentService alignmentService,
    IPlateStateService plateStateService,
    IJsInteropService jsInteropService,
    IRulerService rulerService,
    IColorManagementService colorManagementService,
    IZoomService zoomService)
{
    private const string gridElementId = "grid";
    private const int ExtraSpaceForCanvas = 64;

    private State CurrentState = State.None;
    private const int plateLimit = 100;
    private const double snapValue = 0.25;

    private List<Plate> plates = [];
    private List<Plate> draggingPlates = [];

    private double offsetX = 0;
    private double offsetY = 0;
    private bool wasDragged = false;
    private bool hasLoaded = false;

    private double gridContainerStartX, gridContainerStartY;

    private double GridLength = 0;
    private double GridWidth = 0;

    private bool isSelectionCollapsed = false;

    private void ToggleSearchSelection() => isSelectionCollapsed = !isSelectionCollapsed;

    private bool showLimited = true;

    private void ToggleLimited(bool value) => showLimited = value;

    private void CalculateGridSize()
    {
        var maxPlateY = plates.MaxBy(i => i.Y);
        var maxPlateX = plates.MaxBy(i => i.X);

        var maxValueY = (maxPlateY?.Y + maxPlateY?.Height + ExtraSpaceForCanvas) ?? 0;
        var maxValueX = (maxPlateX?.X + maxPlateX?.Width + ExtraSpaceForCanvas) ?? 0;

        if (maxValueY > GridLength)
        {
            GridLength = maxValueY;
        }

        if (maxValueX > GridWidth)
        {
            GridWidth = maxValueX;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsInteropService.AddZoomPreventingHandler(gridElementId, "wheel");

            if (!hasLoaded)
            {
                plates = await plateStateService.RetrievePreviousSessionPlates();
                await rulerService.Initialize();
                await SetGridContainerOffset();
                hasLoaded = true;
                selectionService.ClearSelection();
                StateHasChanged();
                CalculateGridSize();
            }
        }
    }

    private async Task SetGridContainerOffset()
    {
        var offset = await jsInteropService.GetElementOffset(gridElementId);
        gridContainerStartX = offset?.X ?? 0;
        gridContainerStartY = offset?.Y ?? 0;
    }

    private async void HandleStartSelect(MouseEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        await HandlePointerDown(e.ClientX, e.ClientY);
    }

    private async void HandleStartSelect(TouchEventArgs e)
    {
        if (e.Touches.Length == 0)
        {
            return;
        }

        var touch = e.Touches[0];

        await HandlePointerDown(touch.ClientX, touch.ClientY);
    }

    private async Task HandlePointerDown(double clientX, double clientY)
    {
        var scroll = await GetGridScrollData();

        selectionService.StartSelectionBox(
            (clientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel,
            (clientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel);

        CurrentState = State.Selecting;
    }

    private async void HandleDragOrSelect(MouseEventArgs e)
    {
        await HandlePointerMove(e.ClientX, e.ClientY, e.ShiftKey);
    }

    private async void HandleDragOrSelect(TouchEventArgs e)
    {
        if (e.Touches.Length == 0)
        {
            return;
        }

        var touch = e.Touches[0];
        await HandlePointerMove(touch.ClientX, touch.ClientY);
    }

    private async Task HandlePointerMove(double clientX, double clientY, bool shiftKey = false)
    {
        switch (CurrentState)
        {
            case State.Dragging:
                DragSelectedPlates(clientX, clientY, shiftKey);
                CalculateGridSize();
                break;

            case State.Selecting:
                var scroll = await GetGridScrollData();

                selectionService.UpdateSelectionBox(
                    (clientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel,
                    (clientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel);

                selectionService.SelectPlatesWithinBox(plates);
                break;
        }
    }

    private void HandleStateCancellation(MouseEventArgs e) => HandlePointerUp();

    private void HandleStateCancellation(TouchEventArgs e) => HandlePointerUp();

    private void HandlePointerUp()
    {
        switch (CurrentState)
        {
            case State.Dragging:
                GridLength = 0;
                GridWidth = 0;
                CalculateGridSize();
                draggingPlates.Clear();
                alignmentService.ClearAlignmentLines();
                plateStateService.SaveState(plates);
                break;

            case State.Selecting:
                selectionService.SelectPlatesWithinBox(plates);
                break;
        }

        CurrentState = State.None;
    }

    private async Task<ScrollData> GetGridScrollData() => await jsInteropService.GetScrollPosition(gridElementId) ?? new ScrollData(0, 0);

    private async void HandleOnZoom(WheelEventArgs e)
    {
        if (!e.CtrlKey || CurrentState != State.None) return;

        var scroll = await GetGridScrollData();

        // Calculate the focus point relative to the grid
        double focusX = (e.ClientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel;
        double focusY = (e.ClientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel;

        // Calculate the zoom factor

        if (e.DeltaY > 0)
        {
            zoomService.ZoomOut();
        }
        else
        {
            zoomService.ZoomIn();
        }

        // Adjust scroll to keep the focus point centered
        double newScrollLeft = focusX * zoomService.ZoomLevel - (e.ClientX - gridContainerStartX);
        double newScrollTop = focusY * zoomService.ZoomLevel - (e.ClientY - gridContainerStartY);

        await jsInteropService.SetScrollPosition(gridElementId, newScrollLeft, newScrollTop);

        StateHasChanged();
    }

    private void ZoomOut() => zoomService.ZoomOut();

    private void ZoomIn() => zoomService.ZoomIn();

    private async Task AddPlate(PlateData selectedPlate)
    {
        if (plates.Count >= plateLimit)
        {
            return;
        }

        plateStateService.SaveState(plates);

        var scroll = await GetGridScrollData();

        var newX = scroll.ScrollLeft / zoomService.ZoomLevel;
        var newY = scroll.ScrollTop / zoomService.ZoomLevel;

        while (plates.Any(existing => existing.X == newX && existing.Y == newY))
        {
            newX += 2;
            newY += 2;
        }

        plates.Add(new Plate(selectedPlate.ImageUrl, selectedPlate.Size, selectedPlate.IsHorizontal, newX, newY));
    }

    private void SelectAPlate(MouseEventArgs e, Plate plate)
    {
        if (wasDragged)
        {
            wasDragged = false;
            return;
        }

        if (e.ShiftKey || e.CtrlKey)
        {
            selectionService.InvertSelection(plate);
        }
        else
        {
            selectionService.SelectNewSingle(plate);
        }
    }

    private ScrollData scrollStartDrag = new(0, 0);

    private async void HandleStartDrag(MouseEventArgs e, Plate plate)
    {
        if (e.Button != 0) return;
        await StartDragCommon(e.ClientX, e.ClientY, e.ShiftKey, e.CtrlKey, plate);
    }

    private async void HandleStartDrag(TouchEventArgs e, Plate plate)
    {
        if (e.Touches.Length == 0) return;
        var touch = e.Touches[0];
        await StartDragCommon(touch.ClientX, touch.ClientY, e.ShiftKey, e.CtrlKey, plate);
    }

    private async Task StartDragCommon(double clientX, double clientY, bool shiftKey, bool ctrlKey, Plate plate)
    {
        CurrentState = State.Dragging;
        draggingPlates.Clear();
        plateStateService.SaveState(plates);

        if (!selectionService.ContainsPlate(plate))
        {
            if (!shiftKey && !ctrlKey)
            {
                selectionService.SelectNewSingle(plate);
            }
            else
            {
                return;
            }
        }

        if (selectionService.ContainsPlate(plate))
        {
            draggingPlates.AddRange(selectionService.SelectedPlates);
        }
        else
        {
            draggingPlates.Add(plate);
        }

        offsetX = clientX;
        offsetY = clientY;

        scrollStartDrag = await GetGridScrollData();

        alignmentService.CalculateAlignmentLines(plates, draggingPlates, snapValue);
    }

    private async void DragSelectedPlates(double clientX, double clientY, bool shiftKey)
    {
        if (draggingPlates.Count == 0) return;

        var scroll = await GetGridScrollData();
        var scrollX = scroll.ScrollLeft - scrollStartDrag.ScrollLeft;
        var scrollY = scroll.ScrollTop - scrollStartDrag.ScrollTop;
        scrollStartDrag = scroll;

        var dx = clientX - offsetX + scrollX;
        var dy = clientY - offsetY + scrollY;

        // Move in straight line behaviour
        if (shiftKey)
        {
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                dy = 0;
            }
            else
            {
                dx = 0;
            }
        }

        if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
        {
            var zoomAdjustedX = GetSnappedValue(dx / zoomService.ZoomLevel, snapValue);
            var zoomAdjustedY = GetSnappedValue(dy / zoomService.ZoomLevel, snapValue);

            foreach (var plate in draggingPlates)
            {
                plate.IncrementCoordinates(zoomAdjustedX, zoomAdjustedY);
            }

            offsetX = clientX;
            offsetY = clientY;
            wasDragged = true;

            alignmentService.CalculateAlignmentLines(plates, draggingPlates, snapValue);
        }
    }

    private void RemoveSelectedPlates()
    {
        plateStateService.SaveState(plates);
        foreach (var plate in selectionService.SelectedPlates)
        {
            plates.Remove(plate);
        }

        selectionService.ClearSelection();
    }

    private void MoveSelectedPlates(double dx, double dy)
    {
        plateStateService.SaveState(plates);
        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.IncrementCoordinates(dx, dy);
        }
    }

    private void RotateSelectedPlates()
    {
        plateStateService.SaveState(plates);
        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.Rotate();
        }
    }

    private void ArrangePlatesInOneLine()
    {
        plateStateService.SaveState(plates);
        double currentX = 0;
        double fixedY = 0;

        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.SetCoordinates(currentX, fixedY);
            currentX += plate.Width;
        }
    }

    private void Copy() => clipboardService.CopyPlatesToClipboard(selectionService.SelectedPlates);

    private void Paste()
    {
        var newPlates = clipboardService.CreateNewPlatesFromClipboard();

        if (plates.Count + newPlates.Count > plateLimit)
        {
            return;
        }

        if (newPlates.Count > 0)
        {
            plateStateService.SaveState(plates);
        }

        foreach (var p in newPlates)
        {
            double newX = p.X + 2;
            double newY = p.Y + 2;

            while (plates.Any(existing => existing.X == p.X && existing.Y == p.Y))
            {
                p.IncrementCoordinates(2, 2);
            }

            plates.Add(p);
        }

        selectionService.SelectNewPlates(newPlates);
    }

    private static double GetSnappedValue(double value, double snapValue) => Math.Round(value / snapValue) * snapValue;

    private string GetRotationStyle(Plate plate)
    {
        var isSideways = plate.Rotation == 90 || plate.Rotation == 270;

        var styleWidth = isSideways ? plate.Height : plate.Width;
        var styleHeight = isSideways ? plate.Width : plate.Height;

        return $"""height:{styleHeight * zoomService.ZoomLevel}px; width:{styleWidth * zoomService.ZoomLevel}px;""";
    }

    private void HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (CurrentState != State.None) return;

        switch (e.Key.ToUpper())
        {
            case "1":
                ArrangePlatesInOneLine();
                break;

            case "DELETE":
                RemoveSelectedPlates();
                break;

            case "ESCAPE":
                CollapseAll();
                break;

            case "ARROWUP":
                MoveUp();
                break;

            case "ARROWDOWN":
                MoveDown();
                break;

            case "ARROWLEFT":
                MoveLeft();
                break;

            case "ARROWRIGHT":
                MoveRight();
                break;

            case "A" when e.CtrlKey:
                SelectAll();
                break;

            case "Z" when e.CtrlKey:
                Undo();
                break;

            case "Y" when e.CtrlKey:
                Redo();
                break;

            case "C" when e.CtrlKey:
                Copy();
                break;

            case "V" when e.CtrlKey:
                Paste();
                break;

            case "R" when e.CtrlKey:
                RotateSelectedPlates();
                break;

            case "S" when e.CtrlKey:
                Swap();
                break;
        }
    }

    private void MoveLeft() => MoveSelectedPlates(-snapValue, 0);

    private void MoveRight() => MoveSelectedPlates(snapValue, 0);

    private void MoveDown() => MoveSelectedPlates(0, snapValue);

    private void MoveUp() => MoveSelectedPlates(0, -snapValue);

    private void Undo() => plateStateService.Undo(plates);

    private void Redo() => plateStateService.Redo(plates);

    private void SelectAll() => selectionService.SelectNewPlates(plates);

    private void CollapseAll()
    {
        selectionService.ClearSelection();
        draggingPlates.Clear();
    }

    private async Task OnColorChanged(ChangeEventArgs e) => await colorManagementService.ChangeColor(e.Value?.ToString());

    private async Task ClearColor() => await colorManagementService.ClearColor();

    private void Swap()
    {
        if (selectionService.SelectedPlates.Count != 2)
        {
            return;
        }

        var plate1 = selectionService.SelectedPlates[0];
        var plate2 = selectionService.SelectedPlates[1];

        var plate1X = plate1.X;
        var plate1Y = plate1.Y;
        var plate2X = plate2.X;
        var plate2Y = plate2.Y;

        plate1.SetCoordinates(plate2X, plate2Y);
        plate2.SetCoordinates(plate1X, plate1Y);

        plateStateService.SaveState(plates);
    }

    private async Task Export() => await jsInteropService.ExportFileToUser(plates);

    private async Task Import(InputFileChangeEventArgs args)
    {
        selectionService.ClearSelection();

        if (args.FileCount != 1)
        {
            return;
        }

        try
        {
            var file = args.File;

            using var stream = file.OpenReadStream();

            using var reader = new StreamReader(stream);

            var jsonContent = await reader.ReadToEndAsync();

            var plateList = JsonSerializer.Deserialize<List<Plate>>(jsonContent);

            if (plateList == null || plateList.Count == 0)
            {
                Console.WriteLine("No plates found in the JSON file.");
                return;
            }

            plateStateService.SaveState(plates);

            if (plateList.Count > plateLimit)
            {
                plates.Clear();
                plates.AddRange(plateList.Take(100));
            }
            else
            {
                plates.Clear();
                plates.AddRange(plateList);
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to import with error: {ex.Message}");
        }
    }

    public void SwitchToNextRulerType() => rulerService.SwitchToNextRulerType();
}
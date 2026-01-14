using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Excubo.Blazor.Canvas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;

namespace DisplatePlanner.Pages;

public partial class PlannerV2(
    IClipboardService clipboardService,
    ISelectionService selectionService,
    IAlignmentService alignmentService,
    IPlateStateService plateStateService,
    IJsInteropService jsInteropService,
    IRulerService rulerService,
    IColorManagementService colorManagementService,
    IZoomService zoomService,
    IJSRuntime js)
{
    private const string gridElementId = "grid";
    private const int ExtraSpaceForCanvas = 64;

    private State CurrentState = State.None;
    private const int plateLimit = 100;
    private const double snapValue = 0.25;

    private List<Plate> plates = [];

    private double offsetX = 0;
    private double offsetY = 0;
    private bool wasDragged = false;
    private bool hasLoaded = false;

    private double gridContainerStartX, gridContainerStartY;

    private double GridLength = 0;
    private double GridWidth = 0;

    private double testLenght => _offset.Height;
    private double testWidth => _offset.Width;

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

    private Canvas helper_canvas;
    private bool _needsRerender = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await jsInteropService.AddZoomPreventingHandler(gridElementId, "wheel");

        if (!hasLoaded)
        {
            plates = await plateStateService.RetrievePreviousSessionPlates();
            await rulerService.Initialize();
            await SetGridContainerOffset();
            hasLoaded = true;
            selectionService.ClearSelection();
            CalculateGridSize();
            return;
        }
    }

    private async Task RedrawZoom()
    {
        await using var ctx = await helper_canvas.GetContext2DAsync();
        await using var batch = ctx.CreateBatch();

        await batch.SetTransformAsync(
            zoomService.ZoomLevel, 0,
            0, zoomService.ZoomLevel,
            0, 0
        );
    }

    private bool isFirstDraw = true;

    private Dictionary<string, string> dumbCache = new();

    private async Task Redraw()
    {
        await using var ctx = await helper_canvas.GetContext2DAsync();
        await using var batch = ctx.CreateBatch();

        if (isFirstDraw)
        {
            isFirstDraw = false;
            await batch.SetTransformAsync(zoomService.ZoomLevel, 0, 0, zoomService.ZoomLevel, 0, 0);
        }

        await batch.ClearRectAsync(0, 0, testWidth, testLenght);

        await batch.LineWidthAsync(1 / zoomService.ZoomLevel);
        await batch.StrokeStyleAsync("black");

        await batch.SaveAsync();
        if (selectionService.SelectedPlates.Count > 0)
        {
            await batch.StrokeStyleAsync("red");
            await batch.LineWidthAsync(1.5);
            foreach (var plate in selectionService.SelectedPlates)
            {
                await batch.StrokeRectAsync(plate.X - 0.1, plate.Y - 0.1, plate.Width + 0.1, plate.Height + 0.1);
            }
        }
        await batch.RestoreAsync();

        await batch.ImageSmoothingQualityAsync(ImageSmoothingQuality.High);
        await batch.ImageSmoothingEnabledAsync(true);
        for (int i = 0; i < plates.Count; i++)
        {
            var plate = plates[i];

            if (dumbCache.TryGetValue(plate.ImageUrl, out var cachedImg))
            {
                await batch.DrawImageAsync(cachedImg, plate.X, plate.Y, plate.Width, plate.Height);
            }
            else
            {
                var jsVarName = $"img_{plate.Id.ToString().Replace("-", string.Empty)}";
                await js.InvokeVoidAsync("eval", $"{jsVarName} = document.getElementById('{plate.ImageUrl}')");
                await batch.DrawImageAsync(jsVarName, plate.X, plate.Y, plate.Width, plate.Height);

                dumbCache.Add(plate.ImageUrl, jsVarName);
            }
        }

        if (CurrentState == State.Selecting)
        {
            await batch.SaveAsync();
            await batch.SetLineDashAsync([
                6 / zoomService.ZoomLevel,
                2 / zoomService.ZoomLevel
            ]);
            await batch.StrokeStyleAsync("black");
            await batch.FillStyleAsync("rgba(0, 0, 255, 0.2)");

            await batch.FillRectAsync(
                selectionService.SelectionBox.X,
                selectionService.SelectionBox.Y,
                selectionService.SelectionBox.Width,
                selectionService.SelectionBox.Height
            );
            await batch.StrokeRectAsync(
                selectionService.SelectionBox.X,
                selectionService.SelectionBox.Y,
                selectionService.SelectionBox.Width,
                selectionService.SelectionBox.Height
            );

            await batch.RestoreAsync();
        }

        if (CurrentState == State.Dragging)
        {
            foreach (var line in alignmentService.AlignmentLines)
            {
                await batch.StrokeStyleAsync("blue");
                await batch.BeginPathAsync();
                await batch.MoveToAsync(line.X, line.Y);

                if (line.IsVertical)
                {
                    await batch.LineToAsync(line.X, line.Y + line.Lenght);
                }
                else
                {
                    await batch.LineToAsync(line.X + line.Lenght, line.Y);
                }

                await batch.StrokeAsync();
            }
        }
    }

    private Offset _offset = new Offset();

    private async Task SetGridContainerOffset()
    {
        var offset = await jsInteropService.GetElementOffset(gridElementId);

        if (offset == null)
        {
            gridContainerStartX = 0;
            gridContainerStartY = 0;
        }
        else
        {
            _offset = offset;
            gridContainerStartX = offset.X;
            gridContainerStartY = offset.Y;
        }
    }

    private async void HandleStartSelect(MouseEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        await HandlePointerDown(e.ClientX, e.ClientY, e.ShiftKey, e.CtrlKey);
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

    private async Task HandlePointerDown(double clientX, double clientY, bool shift = false, bool ctrl = false)
    {
        var scroll = await GetGridScrollData();

        var clickedX = (clientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel;
        var clickedY = (clientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel;

        var selectedPlates = selectionService.SelectedPlates;

        var plateExists = plates.LastOrDefault(i => clickedX >= i.X && clickedX <= i.X + i.Width && clickedY >= i.Y && clickedY <= i.Y + i.Height);

        if (plateExists != null)
        {
            await StartDragCommon(clientX, clientY, shift, ctrl, plateExists);
            await Redraw();
        }
        else
        {
            selectionService.StartSelectionBox(clickedX, clickedY);
            await ChangeState(State.Selecting);
        }
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

            default:
                return;
        }

        await Redraw();
    }

    private async Task HandleStateCancellation(MouseEventArgs e) => await HandlePointerUp();

    private async Task HandleStateCancellation(TouchEventArgs e) => await HandlePointerUp();

    private async Task HandlePointerUp()
    {
        switch (CurrentState)
        {
            case State.Dragging:
                GridLength = 0;
                GridWidth = 0;
                CalculateGridSize();
                alignmentService.ClearAlignmentLines();
                plateStateService.SaveState(plates);
                break;

            case State.Selecting:
                selectionService.SelectPlatesWithinBox(plates);
                break;
        }

        await ChangeState(State.None);
    }

    private async Task ChangeState(State currentState)
    {
        CurrentState = currentState;
        await Redraw();
    }

    private async Task<ScrollData> GetGridScrollData() => await jsInteropService.GetScrollPosition(gridElementId) ?? new ScrollData(0, 0);

    private async void HandleOnZoom(WheelEventArgs e)
    {
        if (!e.CtrlKey || CurrentState != State.None)
        {
            return;
        }

        if (e.DeltaY > 0)
        {
            if (!zoomService.ZoomOut())
            {
                return;
            }
        }
        else
        {
            if (!zoomService.ZoomIn())
            {
                return;
            }
        }

        // Calculate the zoom factor
        var scroll = await GetGridScrollData();

        // Calculate the focus point relative to the grid
        double focusX = (e.ClientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel;
        double focusY = (e.ClientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel;

        // Adjust scroll to keep the focus point centered
        double newScrollLeft = focusX * zoomService.ZoomLevel - (e.ClientX - gridContainerStartX);
        double newScrollTop = focusY * zoomService.ZoomLevel - (e.ClientY - gridContainerStartY);

        await jsInteropService.SetScrollPosition(gridElementId, newScrollLeft, newScrollTop);

        await RedrawZoom();
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
        StateHasChanged();
        await Redraw();
    }

    private ScrollData scrollStartDrag = new(0, 0);

    private async Task StartDragCommon(double clientX, double clientY, bool shiftKey, bool ctrlKey, Plate plate)
    {
        plateStateService.SaveState(plates);

        if (!selectionService.ContainsPlate(plate))
        {
            if (shiftKey || ctrlKey)
            {
                selectionService.InvertSelection(plate);
            }
            else
            {
                selectionService.SelectNewSingle(plate);
            }
        }
        else
        {
            if (shiftKey || ctrlKey)
            {
                selectionService.InvertSelection(plate);
                return;
            }
        }

        await ChangeState(State.Dragging);

        offsetX = clientX;
        offsetY = clientY;

        scrollStartDrag = await GetGridScrollData();

        alignmentService.CalculateAlignmentLines(plates, selectionService.SelectedPlates, snapValue);
    }

    private async void DragSelectedPlates(double clientX, double clientY, bool shiftKey)
    {
        if (selectionService.SelectedPlates.Count == 0)
        {
            return;
        }

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

            foreach (var plate in selectionService.SelectedPlates)
            {
                plate.IncrementCoordinates(zoomAdjustedX, zoomAdjustedY);
            }

            offsetX = clientX;
            offsetY = clientY;
            wasDragged = true;

            alignmentService.CalculateAlignmentLines(plates, selectionService.SelectedPlates, snapValue);
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
            rulerService.UpdateRulerBySelectedPlates(selectionService.SelectedPlates);
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

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (CurrentState != State.None)
        {
            return;
        }

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

        await Redraw();
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
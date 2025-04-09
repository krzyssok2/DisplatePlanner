using Blazored.LocalStorage;
using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DisplatePlanner.Pages;

public partial class Planner(
    IClipboardService clipboardService,
    ISelectionService selectionService,
    IAlignmentService alignmentService,
    IPlateStateService plateStateService,
    ILocalStorageService localStorageService,
    IJSRuntime jsRuntime)
{
    private bool isSelectionCollapsed = false;
    private string? selectedColor;

    private State CurrentState = State.None;
    private const int plateLimit = 100;
    private const double snapValue = 0.25;

    private IReadOnlyList<AlignmentLine> AlignmentLines => alignmentService.GetAlignmentLines();
    private IReadOnlyList<Plate> SelectedPlates => selectionService.GetSelectedPlates();
    private Selection SelectionBox => selectionService.GetSelectionBox();

    private List<Plate> plates = [];
    private List<Plate> draggingPlates = [];

    private double zoomLevel = 5.0; // Zoom level
    private double offsetX = 0;
    private double offsetY = 0;
    private bool wasDragged = false;
    private bool hasLoaded = false;
    private bool ShowLimited = true;

    private double gridContainerStartX, gridContainerStartY;

    private double GridLenght = 0;
    private double GridWith = 0;

    private void CalculateGridSize()
    {
        var maxPlateY = plates.MaxBy(i => i.Y);
        var maxPlateX = plates.MaxBy(i => i.X);

        var maxValueY = (maxPlateY?.Y + maxPlateY?.Height + 64) ?? 0;
        var maxValueX = (maxPlateX?.X + maxPlateX?.Width + 64) ?? 0;

        if (maxValueY > GridLenght)
        {
            GridLenght = maxValueY;
        }

        if (maxValueX > GridWith)
        {
            GridWith = maxValueX;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.InvokeVoidAsync("myUtils.addZoomPreventingHandler", "grid", "wheel");

            if (!hasLoaded)
            {
                plates = await plateStateService.RetrievePreviousSessionPlates();
                selectedColor = await localStorageService.GetItemAsync<string>("canvasColor");
                hasLoaded = true;
                StateHasChanged();
                CalculateGridSize();
            }
        }
    }

    private async void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != 0)
        {
            return;
        }

        var scroll = await GetGridScrollData();
        if (scroll == null)
        {
            return;
        }

        // Get the absolute position of the grid container by using the first mouse event
        if (gridContainerStartX == 0 && gridContainerStartY == 0)
        {
            gridContainerStartX = e.ClientX - e.OffsetX;
            gridContainerStartY = e.ClientY - e.OffsetY;
        }

        // Calculate relative start position
        selectionService.StartSelectionBox(
            (e.ClientX - gridContainerStartX + scroll.ScrollLeft) / zoomLevel,
            (e.ClientY - gridContainerStartY + scroll.ScrollTop) / zoomLevel);

        CurrentState = State.Selecting;
    }

    private async void OnMouseMove(MouseEventArgs e)
    {
        switch (CurrentState)
        {
            case State.Dragging:
                DragSelectedPlates(e);
                CalculateGridSize();
                break;

            case State.Selecting:

                var scroll = await GetGridScrollData();

                if (scroll == null)
                {
                    return;
                }

                selectionService.UpdateSelectionBox(
                    (e.ClientX - gridContainerStartX + scroll.ScrollLeft) / zoomLevel,
                    (e.ClientY - gridContainerStartY + scroll.ScrollTop) / zoomLevel);

                selectionService.SelectPlatesWithinBox(plates);
                break;
        }
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        switch (CurrentState)
        {
            case State.Dragging:
                GridLenght = 0;
                GridWith = 0;
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

    private async Task<ScrollData?> GetGridScrollData()
    {
        try
        {
            return await jsRuntime.InvokeAsync<ScrollData>("getScrollPosition", "grid");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return null;
    }

    private const double minZoom = 2;
    private const double maxZoom = 16;

    private async void OnZoom(WheelEventArgs e)
    {
        if (!e.CtrlKey || CurrentState != State.None) return;

        var scroll = await GetGridScrollData();
        if (scroll == null) return;

        // Calculate the zoom factor
        double zoomFactor = e.DeltaY > 0 ? 0.9 : 1.1;
        double newZoomLevel = Math.Clamp(zoomLevel * zoomFactor, minZoom, maxZoom);

        // Calculate the focus point relative to the grid
        double focusX = (e.ClientX - gridContainerStartX + scroll.ScrollLeft) / zoomLevel;
        double focusY = (e.ClientY - gridContainerStartY + scroll.ScrollTop) / zoomLevel;

        zoomLevel = newZoomLevel;

        // Adjust scroll to keep the focus point centered
        double newScrollLeft = focusX * newZoomLevel - (e.ClientX - gridContainerStartX);
        double newScrollTop = focusY * newZoomLevel - (e.ClientY - gridContainerStartY);

        await jsRuntime.InvokeVoidAsync("setScrollPosition", "grid", newScrollLeft, newScrollTop);
        StateHasChanged();
    }

    private void ZoomOut() => zoomLevel = Math.Max(zoomLevel * 0.9, minZoom);

    private void ZoomIn() => zoomLevel = Math.Min(zoomLevel * 1.1, maxZoom);

    private void AddPlate(PlateData selectedPlate)
    {
        if (plates.Count >= plateLimit)
        {
            return;
        }

        plateStateService.SaveState(plates);

        var newX = 0;
        var newY = 0;

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

    private async void StartDrag(MouseEventArgs e, Plate plate)
    {
        if (e.Button != 0) return;

        CurrentState = State.Dragging;
        draggingPlates.Clear();
        plateStateService.SaveState(plates);
        if (!selectionService.ContainsPlate(plate))
        {
            if (!e.ShiftKey && !e.CtrlKey)
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
            draggingPlates.AddRange(SelectedPlates);
        }
        else
        {
            draggingPlates.Add(plate);
        }

        offsetX = e.ClientX;
        offsetY = e.ClientY;

        var scroll = await GetGridScrollData();

        scrollStartDrag = scroll == null ? new(0, 0) : scroll;
        alignmentService.CalculateAlignmentLines(plates, draggingPlates, snapValue);
    }

    private async void DragSelectedPlates(MouseEventArgs e)
    {
        if (draggingPlates.Count == 0) return;

        var scroll = await GetGridScrollData() ?? new ScrollData(0, 0);
        var scrollX = scroll.ScrollLeft - scrollStartDrag.ScrollLeft;
        var scrollY = scroll.ScrollTop - scrollStartDrag.ScrollTop;
        scrollStartDrag = scroll;

        var dx = e.ClientX - offsetX + scrollX;
        var dy = e.ClientY - offsetY + scrollY;

        if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
        {
            var zoomAdjustedX = GetSnappedValue(dx / zoomLevel, snapValue);
            var zoomAdjustedY = GetSnappedValue(dy / zoomLevel, snapValue);

            foreach (var plate in draggingPlates)
            {
                plate.IncrementCoordinates(zoomAdjustedX, zoomAdjustedY);
            }

            offsetX = e.ClientX;
            offsetY = e.ClientY;
            wasDragged = true;

            alignmentService.CalculateAlignmentLines(plates, draggingPlates, snapValue);
        }
    }

    private void RemoveSelectedPlates()
    {
        plateStateService.SaveState(plates);
        foreach (var plate in SelectedPlates)
        {
            plates.Remove(plate);
        }

        selectionService.ClearSelection();
    }

    private void MoveSelectedPlates(double dx, double dy)
    {
        plateStateService.SaveState(plates);
        foreach (var plate in SelectedPlates)
        {
            plate.IncrementCoordinates(dx, dy);
        }
    }

    private void RotateSelectedPlates()
    {
        plateStateService.SaveState(plates);
        foreach (var plate in SelectedPlates)
        {
            plate.Rotate();
        }
    }

    private void ArrangePlatesInOneLine()
    {
        plateStateService.SaveState(plates);
        double currentX = 0;
        double fixedY = 0;

        foreach (var plate in SelectedPlates)
        {
            plate.SetCoordinates(currentX, fixedY);
            currentX += plate.Width;
        }
    }

    private void Copy() => clipboardService.CopyPlatesToClipboard(SelectedPlates);

    private void Paste()
    {
        var pastedPlates = clipboardService.PastePlatesFromClipboard(plates);

        if (pastedPlates != null)
        {
            selectionService.SelectNewPlates(pastedPlates);
        }
    }

    private static double GetSnappedValue(double value, double snapValue) => Math.Round(value / snapValue) * snapValue;

    private static string GetRotationStyle(Plate plate, double zoomLevel)
    {
        var isSideways = plate.Rotation == 90 || plate.Rotation == 270;

        var styleWidth = isSideways ? plate.Height : plate.Width;
        var styleHeight = isSideways ? plate.Width : plate.Height;

        return $"""height:{styleHeight * zoomLevel}px; width:{styleWidth * zoomLevel}px;""";
    }

    private void OnKeyDown(KeyboardEventArgs e)
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
        }
    }

    private void MoveLeft() => MoveSelectedPlates(-snapValue, 0);

    private void MoveRight() => MoveSelectedPlates(snapValue, 0);

    private void MoveDown() => MoveSelectedPlates(0, snapValue);

    private void MoveUp() => MoveSelectedPlates(0, -snapValue);

    private void Undo() => plateStateService.Undo(plates);

    private void Redo() => plateStateService.Redo(plates);

    private void SelectAll()
    {
        selectionService.ClearSelection();
        foreach (var plate in plates)
        {
            selectionService.AddPlate(plate);
        }
    }

    private void CollapseAll()
    {
        selectionService.ClearSelection();
        draggingPlates.Clear();
    }

    private async void OnColorChanged(ChangeEventArgs e)
    {
        selectedColor = e.Value?.ToString();
        await localStorageService.SetItemAsync("canvasColor", selectedColor);
    }

    private async void ClearColor()
    {
        if (selectedColor != null)
        {
            selectedColor = null;

            await localStorageService.RemoveItemAsync("canvasColor");
        }
    }
}
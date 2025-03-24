using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DisplatePlanner.Pages;

public partial class Planner(
    IClipboardService clipboardService,
    ISelectionService selectionService,
    IAlignmentService alignmentService,
    IPlateStateService plateStateService,
    IJSRuntime jsRuntime)
{
    private State CurrentState = State.None;
    private const int plateLimit = 100;
    private const double snapValue = 0.25;

    private IReadOnlyList<AlignmentLine> AlignmentLines => alignmentService.GetAlignmentLines();
    private IReadOnlyList<Plate> SelectedPlates => selectionService.GetSelectedPlates();

    private List<Plate> plates = [];
    private List<Plate> draggingPlates = [];

    private double zoomLevel = 5.0; // Zoom level
    private double offsetX = 0;
    private double offsetY = 0;
    private bool wasDragged = false;
    private bool hasLoaded = false;

    private double selectionBoxStartX = 0;
    private double selectionBoxStartY = 0;
    private double selectionBoxEndX = 0;
    private double selectionBoxEndY = 0;
    private double gridContainerStartX, gridContainerStartY;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.InvokeVoidAsync("myUtils.addZoomPreventingHandler", "grid", "wheel");

            if (!hasLoaded)
            {
                plates = await plateStateService.RetrievePreviousSessionPlates();
                StateHasChanged();
                hasLoaded = true;
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
        selectionBoxStartX = e.ClientX - gridContainerStartX + scroll.ScrollLeft;
        selectionBoxStartY = e.ClientY - gridContainerStartY + scroll.ScrollTop;
        selectionBoxEndX = selectionBoxStartX;
        selectionBoxEndY = selectionBoxStartY;

        CurrentState = State.Selecting;
    }

    private async void OnMouseMove(MouseEventArgs e)
    {
        switch (CurrentState)
        {
            case State.Dragging:
                DragSelectedPlates(e);
                break;

            case State.Selecting:

                var scroll = await GetGridScrollData();

                if (scroll == null)
                {
                    return;
                }

                selectionBoxEndX = e.ClientX - gridContainerStartX + scroll.ScrollLeft;
                selectionBoxEndY = e.ClientY - gridContainerStartY + scroll.ScrollTop;

                //UpdateSelectedPlatesWithinTheBox();
                break;
        }
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        switch (CurrentState)
        {
            case State.Dragging:
                draggingPlates.Clear();
                alignmentService.ClearAlignmentLines();
                plateStateService.SaveState(plates);
                break;

            case State.Selecting:
                UpdateSelectedPlatesWithinTheBox();
                break;
        }

        CurrentState = State.None;
    }

    private bool IsPlateInSelection(Plate plate, Selection selectionRect)
    {
        var plateRect = new Selection(plate.X, plate.Y, plate.Width, plate.Height);
        return selectionRect.IntersectsWith(plateRect);
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

    private void OnZoom(WheelEventArgs e)
    {
        if (!e.CtrlKey || CurrentState != State.None) return;

        zoomLevel = e.DeltaY > 0 ? Math.Max(zoomLevel - 1, 3) : Math.Min(zoomLevel + 1, 15.0);
    }

    private void AddPlate(PlateData selectedPlate)
    {
        if (plates.Count >= plateLimit)
        {
            return;
        }

        plateStateService.SaveState(plates);
        plates.Add(new Plate(selectedPlate.ImageUrl, selectedPlate.Type, selectedPlate.IsHorizontal));
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
                plate.X += zoomAdjustedX;
                plate.Y += zoomAdjustedY;
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
            plate.X += dx;
            plate.Y += dy;
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
        double currentX = 0; // Starting X position for the first plate
        double fixedY = 0;   // Fixed Y position (you can change this if needed)

        foreach (var plate in SelectedPlates)
        {
            plate.X = currentX;
            plate.Y = fixedY;
            currentX += plate.Width; // Add some space between plates
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

    private void UpdateSelectedPlatesWithinTheBox()
    {
        var selectionRect = new Selection(
            Math.Min(selectionBoxStartX, selectionBoxEndX) / zoomLevel,
            Math.Min(selectionBoxStartY, selectionBoxEndY) / zoomLevel,
            Math.Abs(selectionBoxEndX - selectionBoxStartX) / zoomLevel,
            Math.Abs(selectionBoxEndY - selectionBoxStartY) / zoomLevel
        );

        var selectedPlates = plates.Where(plate => IsPlateInSelection(plate, selectionRect)).ToList();
        selectionService.SelectNewPlates(selectedPlates);
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

        switch (e.Key)
        {
            case "1":
                ArrangePlatesInOneLine();
                break;

            case "Delete":
                RemoveSelectedPlates();
                break;

            case "Escape":
                selectionService.ClearSelection();
                draggingPlates.Clear();
                break;

            case "ArrowUp":
                MoveSelectedPlates(0, -snapValue);
                break;

            case "ArrowDown":
                MoveSelectedPlates(0, snapValue);
                break;

            case "ArrowLeft":
                MoveSelectedPlates(-snapValue, 0);
                break;

            case "ArrowRight":
                MoveSelectedPlates(snapValue, 0);
                break;

            case "a" when e.CtrlKey:
                selectionService.ClearSelection();
                foreach (var plate in plates)
                {
                    selectionService.AddPlate(plate);
                }
                break;

            case "z" when e.CtrlKey:
                plateStateService.Undo(plates);
                break;

            case "y" when e.CtrlKey:
                plateStateService.Redo(plates);
                break;

            case "c" when e.CtrlKey:
                Copy();
                break;

            case "v" when e.CtrlKey:
                Paste();
                break;

            case "r" when e.CtrlKey:
                RotateSelectedPlates();
                break;
        }
    }
}
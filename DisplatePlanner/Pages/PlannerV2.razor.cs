using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
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

    private State CurrentState = State.None;
    private const int plateLimit = 100;
    private const double snapValue = 0.25;

    private List<Plate> plates = [];

    private double gridContainerStartX, gridContainerStartY;

    private double TestLenght => _offset.Height;
    private double TestWidth => _offset.Width;

    private bool isSelectionCollapsed = false;

    private void ToggleSearchSelection() => isSelectionCollapsed = !isSelectionCollapsed;

    private bool showLimited = true;

    private void ToggleLimited(bool value) => showLimited = value;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsInteropService.AddZoomPreventingHandler(gridElementId, "wheel");

            plates = await plateStateService.RetrievePreviousSessionPlates();
            await rulerService.Initialize();
            await SetGridContainerOffset();

            selectionService.ClearSelection();

            await RedrawZoom();
            await Redraw();
        }
    }

    private async Task Redraw()
    {
        StateHasChanged();
        await Task.Delay(1);
        var renderData = new
        {
            zoomLevel = zoomService.ZoomLevel,
            scrollLeft = virtualScrollLeft,
            scrollTop = virtualScrollTop,
            canvasWidth = TestWidth,
            canvasHeight = TestLenght,

            plates = plates.Select(p => new
            {
                id = p.Id.ToString(),
                x = p.X,
                y = p.Y,
                width = p.Width,
                height = p.Height,
                imageUrl = p.ImageUrl
            }).ToArray(),

            selectedPlates = selectionService.SelectedPlates.Select(p => new
            {
                id = p.Id.ToString(),
                x = p.X,
                y = p.Y,
                width = p.Width,
                height = p.Height
            }).ToArray(),

            state = CurrentState.ToString(),

            selectionBox = CurrentState == State.Selecting ? new
            {
                x = selectionService.SelectionBox.X,
                y = selectionService.SelectionBox.Y,
                width = selectionService.SelectionBox.Width,
                height = selectionService.SelectionBox.Height
            } : null,

            alignmentLines = CurrentState == State.Dragging
                ? alignmentService.AlignmentLines.Select(line => new
                {
                    x = line.X,
                    y = line.Y,
                    length = line.Lenght, // Note: fixed typo from 'Lenght' to 'length'
                    isVertical = line.IsVertical
                }).ToArray()
                : null
        };

        await js.InvokeVoidAsync("canvasRenderer.render", "helper_canvas", renderData);
    }

    private async Task RedrawZoom()
    {
        StateHasChanged();
        await Task.Delay(1);

        var scroll = VirtualScrollData;
        await js.InvokeVoidAsync("canvasRenderer.setZoom", "helper_canvas", zoomService.ZoomLevel, scroll.ScrollLeft, scroll.ScrollTop);
    }

    // Virtual scroll offsets for canvas infinite scrolling
    private double virtualScrollLeft = 0;

    private double virtualScrollTop = 0;

    // Last pointer position in world coordinates while dragging
    private double lastWorldPointerX = 0;

    private double lastWorldPointerY = 0;

    private Offset _offset = new();

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
        var scroll = VirtualScrollData;

        var clickedX = (clientX - gridContainerStartX) / zoomService.ZoomLevel + scroll.ScrollLeft;
        var clickedY = (clientY - gridContainerStartY) / zoomService.ZoomLevel + scroll.ScrollTop;

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
                break;

            case State.Selecting:
                var scroll = VirtualScrollData;

                selectionService.UpdateSelectionBox(
                    (clientX - gridContainerStartX) / zoomService.ZoomLevel + scroll.ScrollLeft,
                    (clientY - gridContainerStartY) / zoomService.ZoomLevel + scroll.ScrollTop);

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

    private ScrollData VirtualScrollData => new(virtualScrollLeft, virtualScrollTop);

    private async void HandleOnZoom(WheelEventArgs e)
    {
        if (e.CtrlKey && CurrentState == State.None)
        {
            // Zoom: compute focus using the current (old) zoom
            var scroll = VirtualScrollData;
            var oldZoom = zoomService.ZoomLevel;

            double focusX = (e.ClientX - gridContainerStartX) / oldZoom + scroll.ScrollLeft;
            double focusY = (e.ClientY - gridContainerStartY) / oldZoom + scroll.ScrollTop;

            // Apply zoom change
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

            var newZoom = zoomService.ZoomLevel;

            // Compute new virtual scroll so focusX remains under the same screen position
            double newScrollLeft = focusX - (e.ClientX - gridContainerStartX) / newZoom;
            double newScrollTop = focusY - (e.ClientY - gridContainerStartY) / newZoom;

            virtualScrollLeft = newScrollLeft;
            virtualScrollTop = newScrollTop;

            await RedrawZoom();
            await Redraw();
        }
        else
        {
            virtualScrollLeft += (e.DeltaX) / zoomService.ZoomLevel;
            virtualScrollTop += (e.DeltaY) / zoomService.ZoomLevel;

            await Redraw();
            StateHasChanged();
        }
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

        var scroll = VirtualScrollData;

        var newX = scroll.ScrollLeft;
        var newY = scroll.ScrollTop;

        while (plates.Any(existing => existing.X == newX && existing.Y == newY))
        {
            newX += 2;
            newY += 2;
        }

        plates.Add(new Plate(selectedPlate.ImageUrl, selectedPlate.Size, selectedPlate.IsHorizontal, newX, newY));
        await Redraw();
    }

    private ScrollData scrollStartDrag = new(0, 0);

    private double dragOffsetX = 0;
    private double dragOffsetY = 0;

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

        dragOffsetX = clientX;
        dragOffsetY = clientY;

        // initialize last world pointer position
        var scroll = VirtualScrollData;
        lastWorldPointerX = (clientX - gridContainerStartX) / zoomService.ZoomLevel + scroll.ScrollLeft;
        lastWorldPointerY = (clientY - gridContainerStartY) / zoomService.ZoomLevel + scroll.ScrollTop;

        scrollStartDrag = scroll;

        alignmentService.CalculateAlignmentLines(plates, selectionService.SelectedPlates, snapValue);

        await ChangeState(State.Dragging);
    }

    private async void DragSelectedPlates(double clientX, double clientY, bool shiftKey)
    {
        if (selectionService.SelectedPlates.Count == 0)
        {
            return;
        }

        var scroll = VirtualScrollData;
        var scrollX = scroll.ScrollLeft - scrollStartDrag.ScrollLeft;
        var scrollY = scroll.ScrollTop - scrollStartDrag.ScrollTop;
        scrollStartDrag = scroll;

        // Calculate pointer movement in screen pixels
        var screenDx = clientX - dragOffsetX;
        var screenDy = clientY - dragOffsetY;

        // Proceed when either pointer moved or virtual scroll changed
        if (Math.Abs(screenDx) > 3 || Math.Abs(screenDy) > 3 || Math.Abs(scrollX) > double.Epsilon || Math.Abs(scrollY) > double.Epsilon)
        {
            // Compute world pointer now and delta from last world pointer to avoid trailing
            var currentWorldX = (clientX - gridContainerStartX) / zoomService.ZoomLevel + scroll.ScrollLeft;
            var currentWorldY = (clientY - gridContainerStartY) / zoomService.ZoomLevel + scroll.ScrollTop;

            var worldDeltaX = currentWorldX - lastWorldPointerX;
            var worldDeltaY = currentWorldY - lastWorldPointerY;

            foreach (var plate in selectionService.SelectedPlates)
            {
                plate.IncrementCoordinates(worldDeltaX, worldDeltaY);
            }

            dragOffsetX = clientX;
            dragOffsetY = clientY;
            lastWorldPointerX = currentWorldX;
            lastWorldPointerY = currentWorldY;

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
                selectionService.ClearSelection();
                Undo();
                break;

            case "Y" when e.CtrlKey:
                selectionService.ClearSelection();
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
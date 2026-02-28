using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;

namespace DisplatePlanner.Pages;

public partial class Planner(
    IWallService wallService,
    IJsInteropService jsInteropService,
    IColorManagementService colorManagementService,
    IZoomService zoomService)
{
    // cached container size reported by ResizeObserver to avoid repeated JS calls
    private ElementSize? cachedContainerSize;

    [JSInvokable]
    public async Task OnContainerResized(double width, double height)
    {
        cachedContainerSize = new ElementSize(width, height);

        await UpdateGridSizeAsync();
    }

    private const string gridElementId = "grid";
    private const int ExtraSpaceForCanvas = 32;

    private State CurrentState = State.None;

    private double lastPointerX = 0;
    private double lastPointerY = 0;
    private bool wasDragged = false;
    private bool hasLoaded = false;

    private double gridContainerStartX, gridContainerStartY;

    private double GridLength = 0;
    private double GridWidth = 0;

    private bool isSelectionCollapsed = false;

    private void ToggleSearchSelection() => isSelectionCollapsed = !isSelectionCollapsed;

    private bool showLimited = true;

    private void ToggleLimited(bool value) => showLimited = value;

    private async Task UpdateGridSizeAsync(bool allowShrink = true, double? requiredScrollLeftPx = null, double? requiredScrollTopPx = null)
    {
        // Calculate content bounds
        var maxX = wallService.ContentMaxX;
        var maxY = wallService.ContentMaxY;

        var elementSize = cachedContainerSize ??
            await jsInteropService.GetElementClientSize(gridElementId) ??
            new ElementSize(0, 0);

        var zoom = zoomService.ZoomLevel;

        // Minimum size to fit viewport at current zoom
        var minW = elementSize.Width / zoom;
        var minH = elementSize.Height / zoom;

        // Expand minimums if scroll position requires it
        if (requiredScrollLeftPx.HasValue)
        {
            minW = Math.Max(minW, (requiredScrollLeftPx.Value + elementSize.Width) / zoom);
        }

        if (requiredScrollTopPx.HasValue)
        {
            minH = Math.Max(minH, (requiredScrollTopPx.Value + elementSize.Height) / zoom);
        }

        // Desired size: fit content + padding, but at least viewport size
        var desiredW = Math.Max(maxX + ExtraSpaceForCanvas, minW);
        var desiredH = Math.Max(maxY + ExtraSpaceForCanvas, minH);

        // Apply size (grow always, shrink conditionally)
        var newWidth = allowShrink ? desiredW : Math.Max(GridWidth, desiredW);
        var newHeight = allowShrink ? desiredH : Math.Max(GridLength, desiredH);

        if (GridWidth != newWidth || GridLength != newHeight)
        {
            GridWidth = newWidth;
            GridLength = newHeight;
            StateHasChanged();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsInteropService.AddZoomPreventingHandler(gridElementId, "wheel");
            await jsInteropService.RegisterResizeObserver(gridElementId, DotNetObjectReference.Create(this));
            var initialSize = await jsInteropService.GetElementClientSize(gridElementId);
            cachedContainerSize = initialSize ?? new ElementSize(0, 0);

            if (!hasLoaded)
            {
                await wallService.InitializeAsync();
                await SetGridContainerOffset();
                hasLoaded = true;
                StateHasChanged();
                await UpdateGridSizeAsync();
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

        wallService.StartSelectionBox(
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
                await DragSelectedPlates(clientX, clientY, shiftKey);
                await UpdateGridSizeAsync(allowShrink: false);
                return;

            case State.Selecting:
                var scroll = await GetGridScrollData();

                wallService.UpdateSelectionBox(
                    (clientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel,
                    (clientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel);

                wallService.SelectPlatesWithinBox();
                return;

            default:
                return;
        }
    }

    private async void HandleStateCancellation(MouseEventArgs e) => await HandlePointerUpAsync();

    private async void HandleStateCancellation(TouchEventArgs e) => await HandlePointerUpAsync();

    private async Task HandlePointerUpAsync()
    {
        switch (CurrentState)
        {
            case State.Dragging:
                await UpdateGridSizeAsync(allowShrink: false);
                wallService.FinalizeDrag();
                break;

            case State.Selecting:
                wallService.SelectPlatesWithinBox();
                break;
        }

        CurrentState = State.None;
    }

    private async Task<ScrollData> GetGridScrollData() => await jsInteropService.GetScrollPosition(gridElementId) ?? new ScrollData(0, 0);

    private async void HandleOnZoom(WheelEventArgs e)
    {
        if (!e.CtrlKey || CurrentState != State.None)
        {
            return;
        }

        var scroll = await GetGridScrollData();

        // Calculate focus point in grid coordinates (unzoomed space)
        double focusX = (e.ClientX - gridContainerStartX + scroll.ScrollLeft) / zoomService.ZoomLevel;
        double focusY = (e.ClientY - gridContainerStartY + scroll.ScrollTop) / zoomService.ZoomLevel;

        // Apply zoom
        if (e.DeltaY > 0)
        {
            zoomService.ZoomOut();
        }
        else
        {
            zoomService.ZoomIn();
        }

        var newZoom = zoomService.ZoomLevel;

        // Calculate new scroll position to keep focus point under cursor
        double targetScrollLeft = focusX * newZoom - (e.ClientX - gridContainerStartX);
        double targetScrollTop = focusY * newZoom - (e.ClientY - gridContainerStartY);

        // Expand grid if needed to accommodate the target scroll position
        await UpdateGridSizeAsync(
            allowShrink: true,
            requiredScrollLeftPx: Math.Max(0, targetScrollLeft),
            requiredScrollTopPx: Math.Max(0, targetScrollTop)
        );

        // Get container size and calculate scroll bounds
        var containerSize = cachedContainerSize
            ?? await jsInteropService.GetElementClientSize(gridElementId)
            ?? new ElementSize(0, 0);

        double maxScrollLeft = Math.Max(0, GridWidth * newZoom - containerSize.Width);
        double maxScrollTop = Math.Max(0, GridLength * newZoom - containerSize.Height);

        // Clamp and apply scroll position
        await jsInteropService.SetScrollPosition(
            gridElementId,
            Math.Clamp(targetScrollLeft, 0, maxScrollLeft),
            Math.Clamp(targetScrollTop, 0, maxScrollTop)
        );

        StateHasChanged();
    }

    private async Task AddPlate(PlateData selectedPlate)
    {
        var scroll = await GetGridScrollData();
        wallService.AddPlate(selectedPlate, scroll.ScrollLeft / zoomService.ZoomLevel, scroll.ScrollTop / zoomService.ZoomLevel);
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
            wallService.InvertSelection(plate);
        }
        else
        {
            wallService.SelectNewSingle(plate);
        }
    }

    private ScrollData scrollStartDrag = new(0, 0);

    private async void HandleStartDrag(MouseEventArgs e, Plate plate)
    {
        if (e.Button != 0)
        {
            return;
        }

        await StartDragCommon(e.ClientX, e.ClientY, e.ShiftKey, e.CtrlKey, plate);
    }

    private async void HandleStartDrag(TouchEventArgs e, Plate plate)
    {
        if (e.Touches.Length == 0)
        {
            return;
        }

        var touch = e.Touches[0];
        await StartDragCommon(touch.ClientX, touch.ClientY, e.ShiftKey, e.CtrlKey, plate);
    }

    private async Task StartDragCommon(double clientX, double clientY, bool shiftKey, bool ctrlKey, Plate plate)
    {
        CurrentState = State.Dragging;

        if (!wallService.BeginDrag(plate, addToSelection: shiftKey || ctrlKey))
        {
            return;
        }

        lastPointerX = clientX;
        lastPointerY = clientY;

        scrollStartDrag = await GetGridScrollData();
    }

    private async Task DragSelectedPlates(double clientX, double clientY, bool shiftKey)
    {
        if (wallService.SelectedPlates.Count == 0)
        {
            return;
        }

        var scroll = await GetGridScrollData();
        var scrollX = scroll.ScrollLeft - scrollStartDrag.ScrollLeft;
        var scrollY = scroll.ScrollTop - scrollStartDrag.ScrollTop;
        scrollStartDrag = scroll;

        var dx = clientX - lastPointerX + scrollX;
        var dy = clientY - lastPointerY + scrollY;

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
            wallService.ApplyDragDelta(dx / zoomService.ZoomLevel, dy / zoomService.ZoomLevel);

            lastPointerX = clientX;
            lastPointerY = clientY;
            wasDragged = true;
        }
    }

    private string GetRotationStyle(Plate plate)
    {
        var isSideways = plate.Rotation == 90 || plate.Rotation == 270;

        var styleWidth = isSideways ? plate.Height : plate.Width;
        var styleHeight = isSideways ? plate.Width : plate.Height;

        return $"""height:{styleHeight * zoomService.ZoomLevel}px; width:{styleWidth * zoomService.ZoomLevel}px;""";
    }

    private async Task OnColorChanged(ChangeEventArgs e) => await colorManagementService.ChangeColor(e.Value?.ToString());

    private async Task ClearColor() => await colorManagementService.ClearColor();

    private async Task Export() => await jsInteropService.ExportFileToUser(wallService.Plates);

    private async Task Import(InputFileChangeEventArgs args)
    {
        wallService.ClearSelection();

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

            wallService.ImportPlates(plateList);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to import with error: {ex.Message}");
        }
    }

    private void HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (CurrentState != State.None)
        {
            return;
        }

        switch (e.Key.ToUpper())
        {
            case "1":
                wallService.ArrangePlatesInOneLine();
                break;

            case "DELETE":
                wallService.RemoveSelectedPlates();
                break;

            case "ESCAPE":
                wallService.ClearSelection();
                break;

            case "ARROWUP":
                wallService.MoveUp();
                break;

            case "ARROWDOWN":
                wallService.MoveDown();
                break;

            case "ARROWLEFT":
                wallService.MoveLeft();
                break;

            case "ARROWRIGHT":
                wallService.MoveRight();
                break;

            case "A" when e.CtrlKey:
                wallService.SelectAll();
                break;

            case "Z" when e.CtrlKey:
                wallService.Undo();
                break;

            case "Y" when e.CtrlKey:
                wallService.Redo();
                break;

            case "C" when e.CtrlKey:
                wallService.Copy();
                break;

            case "V" when e.CtrlKey:
                wallService.Paste();
                break;

            case "R" when e.CtrlKey:
                wallService.RotateSelectedPlates();
                break;

            case "S" when e.CtrlKey:
                wallService.Swap();
                break;
        }
    }
}
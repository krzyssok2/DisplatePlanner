using Blazored.LocalStorage;
using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace DisplatePlanner.Pages;

public partial class Planner(HttpClient httpClient,
    IAlignmentService alignmentService,
    ILocalStorageService localStorage,
    IHistoryService historyService,
    IJSRuntime jsRuntime)
{
    private State CurrentState = State.None;
    private const int plateLimit = 100;
    private const double snapValue = 0.25;
    private double selectionBoxStartX = 0;
    private double selectionBoxStartY = 0;
    private double selectionBoxEndX = 0;
    private double selectionBoxEndY = 0;
    private double gridContainerStartX, gridContainerStartY;
    private List<PlateData> platesData = [];
    private List<Plate> plates = [];
    private List<Plate> selectedPlates = [];
    private List<Plate> draggingPlates = [];
    private List<Plate> clipboard = [];
    private List<PlateData> filteredPlates = new();
    private string searchTerm = "";
    private double zoomLevel = 5.0; // Zoom level
    private double offsetX = 0;
    private double offsetY = 0;
    private bool wasDragged = false;
    private bool hasLoaded = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadPlates();
        Console.WriteLine("Loaded");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.InvokeVoidAsync("myUtils.addZoomPreventingHandler", "grid", "wheel");
        }

        if (firstRender && !hasLoaded)
        {
            await LoadStateFromLocalStorage();
            Console.WriteLine("Loaded");
            hasLoaded = true;
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
                break;

            case State.Selecting:
                UpdateSelectedPlatesWithinTheBox();
                break;
        }

        CurrentState = State.None;
    }

    private void UpdateSelectedPlatesWithinTheBox()
    {
        var selectionRect = new Selection(
            Math.Min(selectionBoxStartX, selectionBoxEndX) / zoomLevel,
            Math.Min(selectionBoxStartY, selectionBoxEndY) / zoomLevel,
            Math.Abs(selectionBoxEndX - selectionBoxStartX) / zoomLevel,
            Math.Abs(selectionBoxEndY - selectionBoxStartY) / zoomLevel
        );

        selectedPlates = plates.Where(plate => IsPlateInSelection(plate, selectionRect)).ToList();
    }

    private bool IsPlateInSelection(Plate plate, Selection selectionRect)
    {
        var plateRect = new Selection(plate.X, plate.Y, plate.Width, plate.Height);
        return selectionRect.IntersectsWith(plateRect);
    }

    private async Task LoadPlates()
    {
        var limitedEdition = new List<PlateData>();
        var lumino = new List<PlateData>();

        try
        {
            var response = await httpClient.GetFromJsonAsync<LimitedResponse>("https://sapi.displate.com/artworks/limited?miso=US");
            if (response != null)
            {
                limitedEdition = [.. response.Data.Select(x => new PlateData(DateTime.Parse(x.Edition.StartDate), x.Title, x.Images.Main.Url, x.Edition.Type, x.Images.Main.Width > x.Images.Main.Height))];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load limited editions: {ex.Message}");
        }

        try
        {
            var response = await httpClient.GetFromJsonAsync<LuminoResponse>("/lumino.json");
            if (response != null)
            {
                lumino = [.. response.LuminoListings.Data.Select(x => new PlateData(x.StartDate, x.Title, x.Image.X2.Replace("460x640", "560x784"), "standard", false))];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load limited editions: {ex.Message}");
        }

        var combinedLimited = limitedEdition.Concat(lumino).OrderByDescending(x => x.StartDate).ToList();

        platesData = combinedLimited;
        filteredPlates = combinedLimited;
    }

    private async Task LoadStateFromLocalStorage()
    {
        var savedPlates = await localStorage.GetItemAsync<List<Plate>>("savedPlates");
        if (savedPlates != null)
        {
            plates = savedPlates;
        }
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

        historyService.SaveState(plates);
        plates.Add(new Plate(selectedPlate.ImageUrl, selectedPlate.Type, selectedPlate.IsHorizontal));
    }

    private void SelectAPlate(MouseEventArgs e, Plate rect)
    {
        if (wasDragged)
        {
            wasDragged = false;
            return;
        }

        if (e.ShiftKey || e.CtrlKey)
        {
            if (selectedPlates.Contains(rect))
            {
                selectedPlates.Remove(rect);
            }
            else
            {
                selectedPlates.Add(rect);
            }
        }
        else
        {
            selectedPlates.Clear();
            selectedPlates.Add(rect);
        }
    }

    private ScrollData scrollStartDrag = new(0, 0);

    private async void StartDrag(MouseEventArgs e, Plate rect)
    {
        if (e.Button != 0) return;

        CurrentState = State.Dragging;
        draggingPlates.Clear();
        historyService.SaveState(plates);
        if (!selectedPlates.Contains(rect))
        {
            if (!e.ShiftKey && !e.CtrlKey)
            {
                selectedPlates.Clear();
                selectedPlates.Add(rect);
            }
            else
            {
                return;
            }
        }

        if (selectedPlates.Contains(rect))
        {
            draggingPlates.AddRange(selectedPlates);
        }
        else
        {
            draggingPlates.Add(rect);
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
        historyService.SaveState(plates);
        foreach (var plate in selectedPlates)
        {
            plates.Remove(plate);
        }

        selectedPlates.Clear();
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
                selectedPlates.Clear();
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
                selectedPlates.Clear();
                foreach (var plate in plates)
                {
                    selectedPlates.Add(plate);
                }
                break;

            case "z" when e.CtrlKey:
                historyService.Undo(plates);
                break;

            case "y" when e.CtrlKey:
                historyService.Redo(plates);
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

    private void MoveSelectedPlates(double dx, double dy)
    {
        historyService.SaveState(plates);
        foreach (var plate in selectedPlates)
        {
            plate.X += dx;
            plate.Y += dy;
        }
    }

    private void RotateSelectedPlates()
    {
        historyService.SaveState(plates);
        foreach (var plate in selectedPlates)
        {
            plate.Rotate();
        }
    }

    private void ArrangePlatesInOneLine()
    {
        historyService.SaveState(plates);
        double currentX = 0; // Starting X position for the first plate
        double fixedY = 0;   // Fixed Y position (you can change this if needed)

        foreach (var plate in selectedPlates)
        {
            plate.X = currentX;
            plate.Y = fixedY;
            currentX += plate.Width; // Add some space between plates
        }
    }

    private void Copy()
    {
        if (selectedPlates.Count == 0) return;

        clipboard = selectedPlates.Select(p => new Plate(p.ImageUrl, p.Type, p.IsHorizontal)
        {
            X = p.X,
            Y = p.Y,
            Rotation = p.Rotation
        }).ToList();
    }

    private void Paste()
    {
        if (clipboard.Count == 0) return;
        if (plates.Count + clipboard.Count > plateLimit) return;

        historyService.SaveState(plates);
        selectedPlates.Clear();

        var pastedPlates = new List<Plate>();
        foreach (var p in clipboard)
        {
            double newX = p.X + 2;
            double newY = p.Y + 2;

            while (plates.Any(existing => existing.X == newX && existing.Y == newY))
            {
                newX += 2;
                newY += 2;
            }

            var newPlate = new Plate(p.ImageUrl, p.Type, p.IsHorizontal)
            {
                X = newX,
                Y = newY,
                Rotation = p.Rotation
            };

            plates.Add(newPlate);
            pastedPlates.Add(newPlate);
        }

        selectedPlates.AddRange(pastedPlates);
    }

    private void FilterPlates(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? "";
        filteredPlates = [.. platesData.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    private double GetSnappedValue(double value, double snapValue)
    {
        return Math.Round(value / snapValue) * snapValue;
    }

    private string GetRotationStyle(Plate plate, double zoomLevel)
    {
        var isSideways = plate.Rotation == 90 || plate.Rotation == 270;

        var styleWidth = isSideways ? plate.Height : plate.Width;
        var styleHeight = isSideways ? plate.Width : plate.Height;

        return $"""height:{styleHeight * zoomLevel}px; width:{styleWidth * zoomLevel}px;""";
    }
}
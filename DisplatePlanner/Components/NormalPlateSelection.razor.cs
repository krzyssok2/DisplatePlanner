using DisplatePlanner.Enums;
using DisplatePlanner.Models;
using DisplatePlanner.Services;
using Microsoft.AspNetCore.Components;

namespace DisplatePlanner.Components;

public partial class NormalPlateSelection(IndexedDbService indexedDbService)
{
    [Parameter]
    public EventCallback<PlateData> PlateClickedEvent { get; set; }

    private string searchTerm = "";
    private List<PlateData> platesData = [];
    private List<PlateData>? filteredPlates;
    private PlateSize selectedPlateSize = PlateSize.M;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var plates = await indexedDbService.GetAllPlatesAsync();
            platesData = plates;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            platesData = [];
        }

        filteredPlates = platesData;
    }

    private void AddPlate(PlateData plate)
    {
        searchTerm = string.Empty;

        if (platesData.Any(i => i.Id == plate.Id))
        {
            return;
        }
        platesData.Add(plate);
        platesData = platesData.OrderByDescending(i => i.StartDate).ToList();
        Filter();

        _ = indexedDbService.SavePlateAsync(plate);
    }

    private void RemovePlate(PlateData plate)
    {
        platesData.Remove(plate);
        Filter();

        _ = indexedDbService.DeletePlateAsync(plate.Id);
    }

    private void FilterPlates(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? "";
        Filter();
    }

    private void Filter() => filteredPlates = [.. platesData.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];

    private void PlateClicked(PlateData plate)
    {
        var adjustedPlate = new PlateData(plate.Id, plate.StartDate, plate.Name, plate.ImageUrl, plate.Type, plate.IsHorizontal, selectedPlateSize);
        PlateClickedEvent.InvokeAsync(adjustedPlate);
    }
}
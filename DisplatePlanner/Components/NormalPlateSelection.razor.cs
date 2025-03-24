using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;

namespace DisplatePlanner.Components;

public partial class NormalPlateSelection
{
    [Parameter]
    public EventCallback<PlateData> PlateClickedEvent { get; set; }

    private string searchTerm = "";
    private List<PlateData> platesData = [];
    private List<PlateData>? filteredPlates;
    private bool isL = false;

    protected override async Task OnInitializedAsync()
    {
        platesData = [];
        filteredPlates = [];
    }

    private void AddPlate(PlateData plate)
    {
        searchTerm = string.Empty;

        if (!platesData.Any(i => i.ImageUrl.Equals(plate.ImageUrl)))
        {
            platesData.Add(plate);
            platesData = platesData.OrderByDescending(i => i.StartDate).ToList();
            Filter();
        }
    }

    private void FilterPlates(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? "";
        Filter();
    }

    private void Filter() => filteredPlates = [.. platesData.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];

    private void PlateClicked(PlateData plate)
    {
        var adjustedPlate = new PlateData(plate.StartDate, plate.Name, plate.ImageUrl, isL ? "L" : "M", plate.IsHorizontal);
        PlateClickedEvent.InvokeAsync(adjustedPlate);
    }
}
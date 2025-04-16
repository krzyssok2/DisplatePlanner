using DisplatePlanner.Enums;
using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace DisplatePlanner.Components;

public partial class LimitedPlateSelection
{
    [Parameter]
    public EventCallback<PlateData> PlateClickedEvent { get; set; }

    private string searchTerm = "";
    private List<PlateData> platesData = [];
    private List<PlateData>? filteredPlates;

    private readonly Dictionary<PlateType, string> typeDictionary = new()
    {
        { PlateType.LimitedEdition, "LE" },
        { PlateType.UltraLimitedEdition, "ULE" },
        { PlateType.Lumino, "LUM" }
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadPlates();
    }

    private async Task LoadPlates()
    {
        try
        {
            var limitedTask = GetLimitedPlates();
            var luminoTask = GetLuminoPlates();

            var results = await Task.WhenAll(limitedTask, luminoTask);

            platesData = [.. results.SelectMany(r => r).OrderByDescending(x => x.Date)];
            filteredPlates = platesData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load plates: {ex.Message}");
        }
    }

    private async Task<List<PlateData>> GetLimitedPlates()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<LimitedResponse>("https://corsproxy.io/?url=https://sapi.displate.com/artworks/limited?miso=US");
            return response?.Data
                .Select(x => new PlateData(x.ItemCollectionId,
                                 DateTime.Parse(x.Edition.StartDate),
                                 x.Title,
                                 x.Images.Main.Url,
                                 x.Edition.Type == "ultra" ? PlateType.UltraLimitedEdition : PlateType.LimitedEdition,
                                 x.Images.Main.Width > x.Images.Main.Height,
                                 x.Edition.Type == "ultra" ? PlateSize.L : PlateSize.M))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load limited editions: {ex.Message}");
            return [];
        }
    }

    private async Task<List<PlateData>> GetLuminoPlates()
    {
        try
        {
            //Luminos are pulled from Json as they got discontinued, no future updates expected
            var response = await httpClient.GetFromJsonAsync<LuminoResponse>("/lumino.json");
            return response?.LuminoListings.Data
                .Select(x => new PlateData(x.ExternalId, x.StartDate, x.Title, x.Image.X2, PlateType.Lumino, false))
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load lumino editions: {ex.Message}");
            return [];
        }
    }

    private void FilterPlates(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? "";
        filteredPlates = [.. platesData.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))];
    }

    private void PlateClicked(PlateData plate)
    {
        PlateClickedEvent.InvokeAsync(plate);
    }
}
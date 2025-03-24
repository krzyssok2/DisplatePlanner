using DisplatePlanner.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace DisplatePlanner.Components;

public partial class LimitedSearch
{
    [Parameter]
    public EventCallback<PlateData> PlateClickedEvent { get; set; }

    private string searchTerm = "";
    private List<PlateData> platesData = [];
    private List<PlateData>? filteredPlates;

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

            platesData = [.. results.SelectMany(r => r).OrderByDescending(x => x.StartDate)];
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
            var response = await httpClient.GetFromJsonAsync<LimitedResponse>("https://sapi.displate.com/artworks/limited?miso=US");
            return response?.Data
                .Select(x => new PlateData(DateTime.Parse(x.Edition.StartDate), x.Title, x.Images.Main.Url, x.Edition.Type, x.Images.Main.Width > x.Images.Main.Height))
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
                .Select(x => new PlateData(x.StartDate, x.Title, x.Image.X2, "standard", false))
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
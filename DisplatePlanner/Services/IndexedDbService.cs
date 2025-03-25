using DisplatePlanner.Models;
using Microsoft.JSInterop;

namespace DisplatePlanner.Services;

public class IndexedDbService(IJSRuntime jsRuntime)
{
    private bool _isInitialized = false;

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await jsRuntime.InvokeVoidAsync("indexedDbHelper.openDb", "WallPlannerDB", 1);
            _isInitialized = true;
        }
    }

    public async Task SavePlateAsync(PlateData plate)
    {
        await EnsureInitializedAsync();
        await jsRuntime.InvokeVoidAsync("indexedDbHelper.savePlate", plate);
    }

    public async Task<PlateData?> GetPlateAsync(ulong id)
    {
        await EnsureInitializedAsync();
        return await jsRuntime.InvokeAsync<PlateData?>("indexedDbHelper.getPlate", id);
    }

    public async Task<List<PlateData>> GetAllPlatesAsync()
    {
        await EnsureInitializedAsync();
        return await jsRuntime.InvokeAsync<List<PlateData>>("indexedDbHelper.getAllPlates");
    }

    public async Task DeletePlateAsync(ulong id)
    {
        await EnsureInitializedAsync();
        await jsRuntime.InvokeVoidAsync("indexedDbHelper.deletePlate", id);
    }

    public async Task ClearPlatesAsync()
    {
        await EnsureInitializedAsync();
        await jsRuntime.InvokeVoidAsync("indexedDbHelper.clearPlates");
    }
}
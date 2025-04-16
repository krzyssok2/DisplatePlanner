using Blazored.LocalStorage;
using DisplatePlanner.Interfaces;

namespace DisplatePlanner.Services;

public class ColorManagementService : IColorManagementService
{
    private const string ColorStorageKey = "canvasColor";
    private bool _isInitialized = false;

    private string? _selectedColor;
    public string? SelectedColor => _selectedColor;

    private readonly ILocalStorageService localStorageService;

    public ColorManagementService(ILocalStorageService localStorageService)
    {
        this.localStorageService = localStorageService;
        // Retrieval from local storage happens quickly, so we fire and forget
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            try
            {
                _selectedColor = await localStorageService.GetItemAsync<string>(ColorStorageKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch previous session grid color with exception:{ex.Message}");
            }

            _isInitialized = true;
        }
    }

    public async Task ChangeColor(string? newColor)
    {
        _selectedColor = newColor;

        if (newColor != null)
        {
            await localStorageService.SetItemAsync(ColorStorageKey, newColor);
        }
        else
        {
            await ClearColor();
        }
    }

    public async Task ClearColor()
    {
        _selectedColor = null;
        await localStorageService.RemoveItemAsync(ColorStorageKey);
    }
}
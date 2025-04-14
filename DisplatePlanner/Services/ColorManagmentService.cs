using Blazored.LocalStorage;
using DisplatePlanner.Interfaces;

namespace DisplatePlanner.Services;

public class ColorManagementService : IColorManagementService
{
    private const string ColorStorageKey = "canvasColor";
    private bool _isInitialized = false;

    private string? _selectedColor;
    public string? SelectedColor => _selectedColor;

    private ILocalStorageService _localStorageService;

    public ColorManagementService(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            try
            {
                _selectedColor = await _localStorageService.GetItemAsync<string>(ColorStorageKey);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to fetch previous session grid color with exception:{ex.Message}");
            }
            
            _isInitialized = true;
        }
    }

    public async Task ChangeColor(string? newColor)
    {
        _selectedColor = newColor;
        _isInitialized = true;

        if (newColor != null)
        {
            await _localStorageService.SetItemAsync(ColorStorageKey, newColor);
        }
        else
        {
            await ClearColor();
        }
    }

    public async Task ClearColor()
    {
        _selectedColor = null;
        _isInitialized = true;
        await _localStorageService.RemoveItemAsync(ColorStorageKey);
    }
}
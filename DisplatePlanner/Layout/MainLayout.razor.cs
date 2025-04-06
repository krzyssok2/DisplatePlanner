using Blazored.LocalStorage;

namespace DisplatePlanner.Layout;

public partial class MainLayout(ILocalStorageService localStorageService)
{
    private const string storageThemeKey = "theme";

    private bool isDarkMode;
    private string SelectedTheme => isDarkMode ? "dark" : "white";

    protected override async Task OnInitializedAsync()
    {
        var theme = await localStorageService.GetItemAsync<string>(storageThemeKey);

        if (theme == "dark")
        {
            isDarkMode = true;
        }
        else
        {
            isDarkMode = false;
        }
    }

    private async void ToggleTheme()
    {
        isDarkMode = !isDarkMode;

        await localStorageService.SetItemAsync(storageThemeKey, SelectedTheme);
        // LocalStorage.SetItem("darkMode", isDarkMode);
        // UpdateTheme();
    }
}
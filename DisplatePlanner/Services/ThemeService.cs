namespace DisplatePlanner.Services;

public class ThemeService
{
    public event Action? OnThemeChanged;

    private bool _isDarkMode;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnThemeChanged?.Invoke();
            }
        }
    }

    public string CurrentTheme => IsDarkMode ? "dark" : "white";
}
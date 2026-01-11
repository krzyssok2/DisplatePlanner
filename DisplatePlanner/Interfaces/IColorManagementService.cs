namespace DisplatePlanner.Interfaces;

public interface IColorManagementService
{
    string? SelectedColor { get; }

    Task ChangeColor(string? newColor);

    Task ClearColor();
}
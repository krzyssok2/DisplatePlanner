using Blazored.LocalStorage;
using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class RulerService(ILocalStorageService localStorageService) : IRulerService
{
    public RulerType CurrentRulerType { get; private set; } = RulerType.Metric;

    public double Width => GetDimenion(WidthPixels);

    public double Height => GetDimenion(HeightPixels);

    private double WidthPixels = 0;

    private double HeightPixels = 0;

    private readonly RulerType[] _availabelTypes = [.. Enum.GetValues<RulerType>().Where(t => t != RulerType.None)];
    private bool _isInitialized = false;
    private const string RulerTypeLocalStorageKey = "rulerType";

    public async Task Initialize()
    {
        if (!_isInitialized)
        {
            var ruler = await localStorageService.GetItemAsync<RulerType?>(RulerTypeLocalStorageKey);

            if (ruler is null || ruler is RulerType.None)
            {
                return;
            }

            CurrentRulerType = ruler.Value;
            _isInitialized = true;
        }
    }

    public async Task SwitchToNextRulerType()
    {
        if (!_availabelTypes.Contains(CurrentRulerType))
        {
            CurrentRulerType = _availabelTypes.First();
        }
        else
        {
            var idx = Array.IndexOf(_availabelTypes, CurrentRulerType);
            var nextIdx = (idx + 1) % _availabelTypes.Length;
            CurrentRulerType = _availabelTypes[nextIdx];
        }

        await localStorageService.SetItemAsync(RulerTypeLocalStorageKey, CurrentRulerType);
    }

    public void UpdateRulerBySelectedPlates(IReadOnlyCollection<Plate>? selectedPlates)
    {
        if (selectedPlates == null || selectedPlates.Count == 0)
        {
            WidthPixels = 0;
            HeightPixels = 0;
            return;
        }

        double minX = selectedPlates.Min(p => p.X);
        double minY = selectedPlates.Min(p => p.Y);
        double maxX = selectedPlates.Max(p => p.X + p.Width);
        double maxY = selectedPlates.Max(p => p.Y + p.Height);

        WidthPixels = maxX - minX;
        HeightPixels = maxY - minY;

        switch (CurrentRulerType)
        {
            case RulerType.Metric:
                WidthPixels = Math.Round(WidthPixels, 2);
                HeightPixels = Math.Round(HeightPixels, 2);
                return;

            case RulerType.Imperial:
                WidthPixels = Math.Round(WidthPixels * 0.393701, 2);
                HeightPixels = Math.Round(HeightPixels * 0.393701, 2);
                return;
        }
    }

    private double GetDimenion(double value)
    {
        switch (CurrentRulerType)
        {
            case RulerType.Metric:
                return Math.Round(value, 2);

            case RulerType.Imperial:
                return Math.Round(value * 0.393701, 2);

            default:
                return 0;
        }
    }
}
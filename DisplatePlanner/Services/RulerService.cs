using Blazored.LocalStorage;
using DisplatePlanner.Enums;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class RulerService(ILocalStorageService localStorageService) : IRulerService
{
    public RulerType CurrentRulerType { get; private set; } = RulerType.MetricCentimeter;

    public string CurrentRulerTypeDisplayName
    {
        get
        {
            return CurrentRulerType switch
            {
                RulerType.MetricCentimeter => "cm",
                RulerType.MetricMeter => "m",
                RulerType.ImperialInch => "in",
                RulerType.ImperialFoot => "ft",
                _ => string.Empty,
            };
        }
    }

    public bool IsAproximated => CurrentRulerType is not RulerType.MetricCentimeter and not RulerType.MetricMeter;

    public string Width => GetDimension(WidthPixels).ToString($"F{RoundingDigits[CurrentRulerType]}");

    public string Height => GetDimension(HeightPixels).ToString($"F{RoundingDigits[CurrentRulerType]}");

    private double WidthPixels = 0;

    private double HeightPixels = 0;

    public static readonly Dictionary<RulerType, int> RoundingDigits = new()
    {
        { RulerType.MetricCentimeter, 2 },
        { RulerType.MetricMeter, 4 },
        { RulerType.ImperialInch, 2 },
        { RulerType.ImperialFoot, 2 }
    };

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
    }

    private const double CmPerInch = 2.54;
    private const double CmPerFoot = 30.48;

    private double GetDimension(double value)
    {
        return CurrentRulerType switch
        {
            RulerType.MetricCentimeter => value,
            RulerType.MetricMeter => Math.Round(value / 100, RoundingDigits[CurrentRulerType], MidpointRounding.AwayFromZero),
            RulerType.ImperialInch => Math.Round(value / CmPerInch, RoundingDigits[CurrentRulerType], MidpointRounding.AwayFromZero),
            RulerType.ImperialFoot => Math.Round(value / CmPerFoot, RoundingDigits[CurrentRulerType], MidpointRounding.AwayFromZero),
            _ => 0,
        };
    }
}
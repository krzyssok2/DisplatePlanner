using DisplatePlanner.Enums;
using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IRulerService
{
    public RulerType CurrentRulerType { get; }
    public string CurrentRulerTypeDisplayName { get; }
    public bool IsAproximated { get; }
    public string Width { get; }
    public string Height { get; }

    public Task Initialize();

    public Task SwitchToNextRulerType();

    public void UpdateRulerBySelectedPlates(IReadOnlyCollection<Plate>? selectedPlates);
}
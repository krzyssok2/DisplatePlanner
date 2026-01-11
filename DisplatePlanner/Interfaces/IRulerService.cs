using DisplatePlanner.Enums;
using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IRulerService
{
    public RulerType CurrentRulerType { get; }

    public double Width { get; }

    public double Height { get; }

    public Task Initialize();

    public Task SwitchToNextRulerType();

    public void UpdateRulerBySelectedPlates(IReadOnlyCollection<Plate>? selectedPlates);
}
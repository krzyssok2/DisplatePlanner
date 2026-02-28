using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IWallState
{
    List<Plate> Plates { get; }
    double ContentMaxX { get; }
    double ContentMaxY { get; }

    IReadOnlyList<Plate> SelectedPlates { get; }
    Selection SelectionBox { get; }

    IReadOnlyList<AlignmentLine> AlignmentLines { get; }

    string RulerWidth { get; }
    string RulerHeight { get; }
    string RulerTypeDisplayName { get; }
    bool IsRulerApproximated { get; }
}

using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface ISelectionService
{
    List<Plate> SelectedPlates { get; }
    Selection SelectionBox { get; }

    bool ContainsPlate(Plate plate);

    void SelectNewSingle(Plate plate);

    void SelectNewPlates(ICollection<Plate> plates);

    void ClearSelection();

    void AddPlate(Plate plate);

    void AddPlates(ICollection<Plate> plates);

    void InvertSelection(Plate plate);

    void SelectPlatesWithinBox(IEnumerable<Plate> plates);

    void StartSelectionBox(double startX, double startY);

    void UpdateSelectionBox(double endX, double endY);
}
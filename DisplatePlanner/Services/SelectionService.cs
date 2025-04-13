using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class SelectionService : ISelectionService
{
    public List<Plate> SelectedPlates { get; } = [];
    public Selection SelectionBox { get; private set; } = new Selection();

    public bool ContainsPlate(Plate plate) => SelectedPlates.Contains(plate);

    public void SelectNewSingle(Plate plate)
    {
        ClearSelection();
        SelectedPlates.Add(plate);
    }

    public void SelectNewPlates(ICollection<Plate> plates)
    {
        ClearSelection();
        AddPlates(plates);
    }

    public void ClearSelection() => SelectedPlates.Clear();

    public void AddPlate(Plate plate)
    {
        if (!SelectedPlates.Contains(plate))
        {
            SelectedPlates.Add(plate);
        }
    }

    public void AddPlates(ICollection<Plate> plates)
    {
        foreach (var plate in plates)
        {
            AddPlate(plate);
        }
    }

    public void InvertSelection(Plate plate)
    {
        if (!SelectedPlates.Remove(plate))
        {
            SelectedPlates.Add(plate);
        }
    }

    public void StartSelectionBox(double startX, double startY)
    {
        SelectionBox.SetStart(startX, startY);
        SelectionBox.SetEnd(startX, startY);
    }

    public void UpdateSelectionBox(double endX, double endY)
    {
        SelectionBox.SetEnd(endX, endY);
    }

    public void SelectPlatesWithinBox(IEnumerable<Plate> plates)
    {
        SelectNewPlates(plates.Where(plate => IsPlateInSelection(plate, SelectionBox)).ToList());
    }

    private static bool IsPlateInSelection(Plate plate, Selection selection)
    {
        return selection.IntersectsWith(plate);
    }
}
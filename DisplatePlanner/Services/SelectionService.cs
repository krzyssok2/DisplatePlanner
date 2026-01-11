using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class SelectionService(IRulerService rulerService) : ISelectionService
{
    private readonly List<Plate> _selectedPlates = [];
    public IReadOnlyList<Plate> SelectedPlates => _selectedPlates;
    public Selection SelectionBox { get; private set; } = new Selection();

    public bool ContainsPlate(Plate plate) => _selectedPlates.Contains(plate);

    public void SelectNewSingle(Plate plate)
    {
        ClearSelection();
        AddPlate(plate);
    }

    public void SelectNewPlates(ICollection<Plate> plates)
    {
        ClearSelection();
        AddPlates(plates);
    }

    public void ClearSelection()
    {
        _selectedPlates.Clear();
        rulerService.UpdateRulerBySelectedPlates(_selectedPlates);
    }

    public void AddPlate(Plate plate)
    {
        if (!_selectedPlates.Contains(plate))
        {
            _selectedPlates.Add(plate);
            rulerService.UpdateRulerBySelectedPlates(_selectedPlates);
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
        if (!_selectedPlates.Remove(plate))
        {
            AddPlate(plate);
        }
        else
        {
            rulerService.UpdateRulerBySelectedPlates(_selectedPlates);
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
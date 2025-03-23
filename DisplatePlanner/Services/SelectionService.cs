using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class SelectionService : ISelectionService
{
    private readonly List<Plate> SelectedPlates = [];
    private Selection Selection = new(0, 0, 0, 0);

    public IReadOnlyList<Plate> GetSelectedPlates() => SelectedPlates;

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
}
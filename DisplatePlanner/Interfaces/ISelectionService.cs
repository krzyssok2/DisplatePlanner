using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface ISelectionService
{
    public IReadOnlyList<Plate> GetSelectedPlates();

    public bool ContainsPlate(Plate plate);

    public void SelectNewSingle(Plate plate);

    public void SelectNewPlates(ICollection<Plate> plates);

    public void ClearSelection();

    public void AddPlate(Plate plate);

    public void AddPlates(ICollection<Plate> plates);

    public void InvertSelection(Plate plate);
}
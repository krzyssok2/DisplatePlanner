using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IPlateStateService
{
    public void SaveState(ICollection<Plate> plates);

    public void Undo(List<Plate> plates);

    public void Redo(List<Plate> plates);

    public Task<List<Plate>> RetrievePreviousSessionPlates();
}
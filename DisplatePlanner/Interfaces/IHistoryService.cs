using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IHistoryService
{
    public void SaveState(List<Plate> plates);

    public void Undo(List<Plate> plates);

    public void Redo(List<Plate> plates);
}
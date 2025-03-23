using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class HistoryService : IHistoryService
{
    private const int historyLimit = 50;

    private readonly LinkedList<List<Plate>> PlatesHistory = new();
    private readonly LinkedList<List<Plate>> RedoHistory = new();

    public void SaveState(List<Plate> plates)
    {
        // Save the current state
        var lastHistory = PlatesHistory.Last?.Value;

        if (lastHistory != null && plates.SequenceEqual(lastHistory))
        {
            return;
        }

        var clonedPlates = ClonePlates(plates);
        PlatesHistory.AddLast(clonedPlates);

        // Trim history if it exceeds the limit
        if (PlatesHistory.Count > historyLimit)
        {
            PlatesHistory.RemoveFirst();
        }

        // Clear redo history when a new state is saved (breaking the redo chain)
        RedoHistory.Clear();

        //SaveStateToLocalStorage();
    }

    public void Undo(List<Plate> plates)
    {
        if (PlatesHistory.Count > 0)
        {
            // Move the current state to redo history
            RedoHistory.AddLast(ClonePlates(plates));

            // Restore the previous state
            plates = PlatesHistory.Last.Value;
            PlatesHistory.RemoveLast();

            //SaveStateToLocalStorage();
        }
    }

    public void Redo(List<Plate> plates)
    {
        if (RedoHistory.Count > 0)
        {
            // Move the current state to undo history
            PlatesHistory.AddLast(ClonePlates(plates));

            // Restore the most recent redo state
            plates = RedoHistory.Last.Value;
            RedoHistory.RemoveLast();

            //SaveStateToLocalStorage();
        }
    }

    private static List<Plate> ClonePlates(List<Plate> plates)
    {
        return plates.Select(p => new Plate(p.ImageUrl, p.Type, p.IsHorizontal) { X = p.X, Y = p.Y, Rotation = p.Rotation, Height = p.Height, Width = p.Width }).ToList();
    }
}
using Blazored.LocalStorage;
using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class PlateStateService(ILocalStorageService localStorageService) : IPlateStateService
{
    private const int historyLimit = 50;

    private readonly LinkedList<List<Plate>> PlatesHistory = new();
    private readonly LinkedList<List<Plate>> RedoHistory = new();

    public void SaveState(ICollection<Plate> plates)
    {
        var lastHistory = PlatesHistory.Last?.Value;

        if (lastHistory != null && plates.SequenceEqual(lastHistory))
        {
            return;
        }

        var clonedPlates = ClonePlates(plates);
        PlatesHistory.AddLast(clonedPlates);

        if (PlatesHistory.Count > historyLimit)
        {
            PlatesHistory.RemoveFirst();
        }

        RedoHistory.Clear();

        _ = SaveStateToLocalStorage(plates);
    }

    public void Undo(List<Plate> plates)
    {
        if (PlatesHistory.Count > 0)
        {
            RedoHistory.AddLast(ClonePlates(plates));

            plates.Clear();
            plates.AddRange(PlatesHistory.Last!.Value);
            PlatesHistory.RemoveLast();

            _ = SaveStateToLocalStorage(plates);
        }
    }

    public void Redo(List<Plate> plates)
    {
        if (RedoHistory.Count > 0)
        {
            PlatesHistory.AddLast(ClonePlates(plates));

            plates.Clear();
            plates.AddRange(RedoHistory.Last!.Value);
            RedoHistory.RemoveLast();

            _ = SaveStateToLocalStorage(plates);
        }
    }

    public async Task<List<Plate>> RetrievePreviousSessionPlates()
    {
        var savedPlates = await localStorageService.GetItemAsync<List<Plate>>("savedPlates");

        return savedPlates ?? [];
    }

    private async Task SaveStateToLocalStorage(ICollection<Plate> plates)
    {
        await localStorageService.SetItemAsync("savedPlates", plates);
    }

    private static List<Plate> ClonePlates(IEnumerable<Plate> plates)
    {
        return [.. plates.Select(p => new Plate(p.ImageUrl, p.Size, p.IsHorizontal, p.X, p.Y, p.Rotation))];
    }
}
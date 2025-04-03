﻿using Blazored.LocalStorage;
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

        _ = SaveStateToLocalStorage(plates);
    }

    public void Undo(List<Plate> plates)
    {
        if (PlatesHistory.Count > 0)
        {
            // Move the current state to redo history
            RedoHistory.AddLast(ClonePlates(plates));

            // Restore the previous state
            plates.Clear();
            plates.AddRange(PlatesHistory.Last!.Value); // Modify the original list contents
            PlatesHistory.RemoveLast();

            _ = SaveStateToLocalStorage(plates);
        }
    }

    public void Redo(List<Plate> plates)
    {
        if (RedoHistory.Count > 0)
        {
            // Move the current state to undo history
            PlatesHistory.AddLast(ClonePlates(plates));

            // Restore the most recent redo state
            plates.Clear();
            plates.AddRange(RedoHistory.Last!.Value); // Modify the original list contents
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
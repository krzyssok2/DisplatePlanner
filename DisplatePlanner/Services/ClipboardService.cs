using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class ClipboardService(IPlateStateService plateStateService) : IClipboardService
{
    private readonly List<Plate> Clipboard = [];

    private const int plateLimit = 100;

    public void CopyPlatesToClipboard(IEnumerable<Plate> plates)
    {
        if (!plates.Any()) return;

        Clipboard.Clear();
        Clipboard.AddRange(ClonePlates(plates));
    }

    public ICollection<Plate>? PastePlatesFromClipboard(ICollection<Plate> plates)
    {
        if (plates.Count + Clipboard.Count > plateLimit)
        {
            return null;
        }

        if (Clipboard.Count != 0)
        {
            plateStateService.SaveState(plates);
        }

        var pastedPlates = new List<Plate>();
        foreach (var p in Clipboard)
        {
            double newX = p.X + 2;
            double newY = p.Y + 2;

            while (plates.Any(existing => existing.X == newX && existing.Y == newY))
            {
                newX += 2;
                newY += 2;
            }

            var newPlate = new Plate(p.ImageUrl, p.Size, p.IsHorizontal, p.X, p.Y, p.Rotation);

            plates.Add(newPlate);
            pastedPlates.Add(newPlate);
        }

        return pastedPlates;
    }

    private static List<Plate> ClonePlates(IEnumerable<Plate> plates)
    {
        return [.. plates.Select(p => new Plate(p.ImageUrl, p.Size, p.IsHorizontal, p.X, p.Y, p.Rotation))];
    }
}
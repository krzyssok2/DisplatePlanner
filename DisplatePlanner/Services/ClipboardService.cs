using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class ClipboardService : IClipboardService
{
    private readonly List<Plate> Clipboard = [];

    public void CopyPlatesToClipboard(IEnumerable<Plate> plates)
    {
        if (!plates.Any()) return;

        Clipboard.Clear();
        Clipboard.AddRange(ClonePlates(plates));
    }

    public List<Plate> CreateNewPlatesFromClipboard()
    {
        return ClonePlates(Clipboard);
    }

    private static List<Plate> ClonePlates(IEnumerable<Plate> plates)
    {
        return [.. plates.Select(p => new Plate(p.ImageUrl, p.Size, p.IsHorizontal, p.X, p.Y, p.Rotation))];
    }
}
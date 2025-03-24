using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IClipboardService
{
    public void CopyPlatesToClipboard(IEnumerable<Plate> plates);

    public ICollection<Plate>? PastePlatesFromClipboard(ICollection<Plate> plates);
}
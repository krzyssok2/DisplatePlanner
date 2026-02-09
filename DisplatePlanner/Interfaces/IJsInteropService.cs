using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IJsInteropService
{
    Task<Offset?> GetElementOffset(string elementId);

    Task<ScrollData?> GetScrollPosition(string elementId);

    Task<ElementSize?> GetElementClientSize(string elementId);
    Task RegisterResizeObserver(string elementId, object dotNetRef);

    Task SetScrollPosition(string elementId, double scrollLeft, double scrollTop);

    Task AddZoomPreventingHandler(string elementId, string eventName);

    Task ExportFileToUser(IReadOnlyList<Plate> plates);
}
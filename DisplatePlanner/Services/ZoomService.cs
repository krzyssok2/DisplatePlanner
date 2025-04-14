using DisplatePlanner.Interfaces;

namespace DisplatePlanner.Services;

public class ZoomService : IZoomService
{
    private const double minZoom = 2;
    private const double maxZoom = 16;

    private double _zoomLevel = 5;
    public double ZoomLevel => _zoomLevel;

    public void ZoomIn() => ZoomAdjustment(1.1);

    public void ZoomOut() => ZoomAdjustment(0.9);

    private void ZoomAdjustment(double zoomFactor) => _zoomLevel = Math.Clamp(_zoomLevel * zoomFactor, minZoom, maxZoom);
}
using DisplatePlanner.Interfaces;

namespace DisplatePlanner.Services;

public class ZoomService : IZoomService
{
    private const double minZoom = 2;
    private const double maxZoom = 16;

    private double _zoomLevel = 5;
    public double ZoomLevel => _zoomLevel;

    public bool ZoomIn()
    {
        if (_zoomLevel == maxZoom)
        {
            return false;
        }

        ZoomAdjustment(1.1);

        return true;
    }

    public bool ZoomOut()
    {
        if (_zoomLevel == minZoom)
        {
            return false;
        }

        ZoomAdjustment(0.9);

        return true;
    }

    private void ZoomAdjustment(double zoomFactor) => _zoomLevel = Math.Clamp(_zoomLevel * zoomFactor, minZoom, maxZoom);
}
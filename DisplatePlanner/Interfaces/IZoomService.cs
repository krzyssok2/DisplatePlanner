namespace DisplatePlanner.Interfaces;

public interface IZoomService
{
    double ZoomLevel { get; }

    bool ZoomIn();

    bool ZoomOut();
}
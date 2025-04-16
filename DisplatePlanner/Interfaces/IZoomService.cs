namespace DisplatePlanner.Interfaces;

public interface IZoomService
{
    double ZoomLevel { get; }

    void ZoomIn();

    void ZoomOut();
}
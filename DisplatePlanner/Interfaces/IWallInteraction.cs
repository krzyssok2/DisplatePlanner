using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IWallInteraction
{
    void StartSelectionBox(double startX, double startY);
    void UpdateSelectionBox(double endX, double endY);
    void SelectPlatesWithinBox();
    void SelectNewSingle(Plate plate);
    void InvertSelection(Plate plate);
    void ClearSelection();
    void SelectAll();

    bool BeginDrag(Plate plate, bool addToSelection);
    void ApplyDragDelta(double dx, double dy);
    void FinalizeDrag();
}

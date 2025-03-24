using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class SelectionService : ISelectionService
{
    private readonly List<Plate> SelectedPlates = [];
    private double selectionBoxStartX, selectionBoxStartY, selectionBoxEndX, selectionBoxEndY;

    private Selection SelectionBox => new(
        Math.Min(selectionBoxStartX, selectionBoxEndX),
        Math.Min(selectionBoxStartY, selectionBoxEndY),
        Math.Abs(selectionBoxEndX - selectionBoxStartX),
        Math.Abs(selectionBoxEndY - selectionBoxStartY)
    );

    public IReadOnlyList<Plate> GetSelectedPlates() => SelectedPlates;

    public bool ContainsPlate(Plate plate) => SelectedPlates.Contains(plate);

    public Selection GetSelectionBox() => SelectionBox;

    public void SelectNewSingle(Plate plate)
    {
        ClearSelection();
        SelectedPlates.Add(plate);
    }

    public void SelectNewPlates(ICollection<Plate> plates)
    {
        ClearSelection();
        AddPlates(plates);
    }

    public void ClearSelection() => SelectedPlates.Clear();

    public void AddPlate(Plate plate)
    {
        if (!SelectedPlates.Contains(plate))
        {
            SelectedPlates.Add(plate);
        }
    }

    public void AddPlates(ICollection<Plate> plates)
    {
        foreach (var plate in plates)
        {
            AddPlate(plate);
        }
    }

    public void InvertSelection(Plate plate)
    {
        if (!SelectedPlates.Remove(plate))
        {
            SelectedPlates.Add(plate);
        }
    }

    public void StartSelectionBox(double startX, double startY)
    {
        selectionBoxStartX = startX;
        selectionBoxStartY = startY;
        selectionBoxEndX = startX;
        selectionBoxEndY = startY;
    }

    public void UpdateSelectionBox(double endX, double endY)
    {
        selectionBoxEndX = endX;
        selectionBoxEndY = endY;
    }

    public void SelectPlatesWithinBox(IEnumerable<Plate> plates)
    {
        SelectNewPlates(plates.Where(plate => IsPlateInSelection(plate, SelectionBox)).ToList());
    }

    private static bool IsPlateInSelection(Plate plate, Selection selection)
    {
        var plateRect = new Selection(plate.X, plate.Y, plate.Width, plate.Height);
        return selection.IntersectsWith(plateRect);
    }
}
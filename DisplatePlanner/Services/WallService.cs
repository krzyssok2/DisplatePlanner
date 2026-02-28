using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class WallService(
    IClipboardService clipboardService,
    ISelectionService selectionService,
    IAlignmentService alignmentService,
    IPlateStateService plateStateService,
    IRulerService rulerService) : IWallService
{
    private const int plateLimit = 100;
    private const double snapValue = 0.25;

    private readonly List<Plate> _plates = [];

    // IWallState – read-only properties for rendering

    public List<Plate> Plates => _plates;
    public double ContentMaxX => _plates.Count != 0 ? _plates.Max(p => p.X + p.Width) : 0;
    public double ContentMaxY => _plates.Count != 0 ? _plates.Max(p => p.Y + p.Height) : 0;

    public IReadOnlyList<Plate> SelectedPlates => selectionService.SelectedPlates;
    public Selection SelectionBox => selectionService.SelectionBox;
    public IReadOnlyList<AlignmentLine> AlignmentLines => alignmentService.AlignmentLines;

    public string RulerWidth => rulerService.Width;
    public string RulerHeight => rulerService.Height;
    public string RulerTypeDisplayName => rulerService.CurrentRulerTypeDisplayName;
    public bool IsRulerApproximated => rulerService.IsAproximated;

    // IWallService – lifecycle

    public async Task InitializeAsync()
    {
        var savedPlates = await plateStateService.RetrievePreviousSessionPlates();
        _plates.Clear();
        _plates.AddRange(savedPlates);
        await rulerService.Initialize();
        selectionService.ClearSelection();
    }

    public Task SwitchToNextRulerType() => rulerService.SwitchToNextRulerType();

    // IWallInteraction – selection

    public void StartSelectionBox(double startX, double startY) => selectionService.StartSelectionBox(startX, startY);
    public void UpdateSelectionBox(double endX, double endY) => selectionService.UpdateSelectionBox(endX, endY);
    public void SelectPlatesWithinBox() => selectionService.SelectPlatesWithinBox(_plates);
    public void SelectNewSingle(Plate plate) => selectionService.SelectNewSingle(plate);
    public void InvertSelection(Plate plate) => selectionService.InvertSelection(plate);
    public void ClearSelection() => selectionService.ClearSelection();
    public void SelectAll() => selectionService.SelectNewPlates(_plates);

    // IWallInteraction – drag

    public bool BeginDrag(Plate plate, bool addToSelection)
    {
        plateStateService.SaveState(_plates);

        if (!selectionService.ContainsPlate(plate))
        {
            if (!addToSelection)
            {
                selectionService.SelectNewSingle(plate);
            }
            else
            {
                return false;
            }
        }

        alignmentService.CalculateAlignmentLines(_plates, selectionService.SelectedPlates, snapValue);
        return true;
    }

    public void ApplyDragDelta(double dx, double dy)
    {
        var snappedDx = GetSnappedValue(dx, snapValue);
        var snappedDy = GetSnappedValue(dy, snapValue);

        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.SetX(GetSnappedValue(plate.X + snappedDx, snapValue));
            plate.SetY(GetSnappedValue(plate.Y + snappedDy, snapValue));
        }

        alignmentService.CalculateAlignmentLines(_plates, selectionService.SelectedPlates, snapValue);
    }

    public void FinalizeDrag()
    {
        alignmentService.ClearAlignmentLines();
        plateStateService.SaveState(_plates);
    }

    // IWallCommands – simple operations

    public void MoveLeft() => MoveSelectedPlates(-snapValue, 0);
    public void MoveRight() => MoveSelectedPlates(snapValue, 0);
    public void MoveUp() => MoveSelectedPlates(0, -snapValue);
    public void MoveDown() => MoveSelectedPlates(0, snapValue);
    public void Copy() => clipboardService.CopyPlatesToClipboard(selectionService.SelectedPlates);
    public void Undo() => plateStateService.Undo(_plates);
    public void Redo() => plateStateService.Redo(_plates);

    // IWallCommands – plate mutations

    public void AddPlate(PlateData selectedPlate, double x, double y)
    {
        if (_plates.Count >= plateLimit)
        {
            return;
        }

        plateStateService.SaveState(_plates);

        var newX = GetSnappedValue(x, snapValue);
        var newY = GetSnappedValue(y, snapValue);

        while (_plates.Any(existing => existing.X == newX && existing.Y == newY))
        {
            newX += 2;
            newY += 2;
        }

        _plates.Add(new Plate(selectedPlate.ImageUrl, selectedPlate.Size, selectedPlate.IsHorizontal, newX, newY));
    }

    public void RemoveSelectedPlates()
    {
        plateStateService.SaveState(_plates);
        foreach (var plate in selectionService.SelectedPlates)
        {
            _plates.Remove(plate);
        }

        selectionService.ClearSelection();
    }

    public void RotateSelectedPlates()
    {
        plateStateService.SaveState(_plates);
        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.Rotate();
        }
    }

    public void ArrangePlatesInOneLine()
    {
        plateStateService.SaveState(_plates);
        double currentX = 0;
        double fixedY = 0;

        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.SetCoordinates(currentX, fixedY);
            rulerService.UpdateRulerBySelectedPlates(selectionService.SelectedPlates);
            currentX += plate.Width;
        }
    }

    public void Paste()
    {
        var newPlates = clipboardService.CreateNewPlatesFromClipboard();

        if (_plates.Count + newPlates.Count > plateLimit)
        {
            return;
        }

        if (newPlates.Count > 0)
        {
            plateStateService.SaveState(_plates);
        }

        foreach (var p in newPlates)
        {
            double newX = p.X + 2;
            double newY = p.Y + 2;

            while (_plates.Any(existing => existing.X == p.X && existing.Y == p.Y))
            {
                p.IncrementCoordinates(2, 2);
            }

            _plates.Add(p);
        }

        selectionService.SelectNewPlates(newPlates);
    }

    public void Swap()
    {
        if (selectionService.SelectedPlates.Count != 2)
        {
            return;
        }

        var plate1 = selectionService.SelectedPlates[0];
        var plate2 = selectionService.SelectedPlates[1];

        var plate1X = plate1.X;
        var plate1Y = plate1.Y;
        var plate2X = plate2.X;
        var plate2Y = plate2.Y;

        plate1.SetCoordinates(plate2X, plate2Y);
        plate2.SetCoordinates(plate1X, plate1Y);

        plateStateService.SaveState(_plates);
    }

    public void ImportPlates(List<Plate> importedPlates)
    {
        plateStateService.SaveState(_plates);

        _plates.Clear();
        if (importedPlates.Count > plateLimit)
        {
            _plates.AddRange(importedPlates.Take(plateLimit));
        }
        else
        {
            _plates.AddRange(importedPlates);
        }
    }

    // Private helpers

    private static double GetSnappedValue(double value, double snapValue) => Math.Round(value / snapValue) * snapValue;

    private void MoveSelectedPlates(double dx, double dy)
    {
        plateStateService.SaveState(_plates);
        foreach (var plate in selectionService.SelectedPlates)
        {
            plate.IncrementCoordinates(dx, dy);
        }
    }
}
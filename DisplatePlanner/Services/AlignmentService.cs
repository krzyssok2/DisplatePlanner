using DisplatePlanner.Interfaces;
using DisplatePlanner.Models;

namespace DisplatePlanner.Services;

public class AlignmentService : IAlignmentService
{
    private readonly List<AlignmentLine> _alignmentLines = [];
    public IReadOnlyList<AlignmentLine> AlignmentLines => _alignmentLines;

    public void CalculateAlignmentLines(List<Plate> plates, IReadOnlyCollection<Plate> draggingPlates, double snapValue)
    {
        _alignmentLines.Clear();

        foreach (var plate in plates)
        {
            if (draggingPlates.Contains(plate)) continue;

            foreach (var draggingPlate in draggingPlates)
            {
                CheckAlignment(plate, draggingPlate, true, snapValue);
                CheckAlignment(plate, draggingPlate, false, snapValue);
            }
        }
    }

    public void ClearAlignmentLines()
    {
        _alignmentLines.Clear();
    }

    private void CheckAlignment(Plate plate, Plate selectedPlate, bool isVertical, double snapValue)
    {
        double plateCenter = isVertical ? plate.X + plate.Width / 2 : plate.Y + plate.Height / 2;
        double draggingCenter = isVertical ? selectedPlate.X + selectedPlate.Width / 2 : selectedPlate.Y + selectedPlate.Height / 2;

        if (IsAligned(plateCenter, draggingCenter, snapValue))
        {
            AddAlignmentLine(isVertical, plateCenter, plate, selectedPlate); // Middle alignment
        }
        else
        {
            double plateStart = isVertical ? plate.X : plate.Y;
            double plateEnd = isVertical ? plate.X + plate.Width : plate.Y + plate.Height;
            double selectedStart = isVertical ? selectedPlate.X : selectedPlate.Y;
            double selectedEnd = isVertical ? selectedPlate.X + selectedPlate.Width : selectedPlate.Y + selectedPlate.Height;

            TryAddAlignment(isVertical, plate, selectedPlate, plateStart, selectedStart, snapValue); // Start alignment
            TryAddAlignment(isVertical, plate, selectedPlate, plateEnd, selectedStart, snapValue); // End to start alignment
            TryAddAlignment(isVertical, plate, selectedPlate, plateStart, selectedEnd, snapValue); // Start to end alignment
            TryAddAlignment(isVertical, plate, selectedPlate, plateEnd, selectedEnd, snapValue); // End alignment
        }
    }

    private static bool IsAligned(double pos1, double pos2, double snapValue) => Math.Abs(pos1 - pos2) < snapValue;

    private void TryAddAlignment(bool isVertical, Plate plate, Plate selectedPlate, double pos1, double pos2, double snapValue)
    {
        if (IsAligned(pos1, pos2, snapValue))
        {
            AddAlignmentLine(isVertical, pos1, plate, selectedPlate);
        }
    }

    private void AddAlignmentLine(bool isVertical, double position, Plate plate, Plate selectedPlate)
    {
        _alignmentLines.Add(new AlignmentLine(isVertical,
            isVertical ? position : Math.Min(plate.X, selectedPlate.X),
            isVertical ? Math.Min(plate.Y, selectedPlate.Y) : position,
            isVertical
                ? Math.Max(plate.Y + plate.Height, selectedPlate.Y + selectedPlate.Height) - Math.Min(plate.Y, selectedPlate.Y)
                : Math.Max(plate.X + plate.Width, selectedPlate.X + selectedPlate.Width) - Math.Min(plate.X, selectedPlate.X)));
    }
}
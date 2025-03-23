using DisplatePlanner.Models;

namespace DisplatePlanner.Interfaces;

public interface IAlignmentService
{
    public IReadOnlyList<AlignmentLine> GetAlignmentLines();

    public void CalculateAlignmentLines(List<Plate> plates, List<Plate> draggingPlates, double snapValue);

    public void ClearAlignmentLines();
}
namespace DisplatePlanner.Models;

public class Selection()
{
    public double X => Math.Min(XStart, XEnd);
    public double Y => Math.Min(YStart, YEnd);
    public double Width => Math.Abs(XEnd - XStart);
    public double Height => Math.Abs(YEnd - YStart);

    private double XStart = 0;
    private double YStart = 0;
    private double XEnd = 0;
    private double YEnd = 0;

    public void SetStart(double xStart, double yStart)
    {
        XStart = xStart;
        YStart = yStart;
    }

    public void SetEnd(double xEnd, double yEnd)
    {
        XEnd = xEnd;
        YEnd = yEnd;
    }

    public bool IntersectsWith(Plate other)
    {
        return !(X > other.X + other.Width || X + Width < other.X || Y > other.Y + other.Height || Y + Height < other.Y);
    }
}
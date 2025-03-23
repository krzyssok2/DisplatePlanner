namespace DisplatePlanner.Models;

public record Selection(double X, double Y, double Width, double Height)
{
    public bool IntersectsWith(Selection other)
    {
        return !(X > other.X + other.Width || X + Width < other.X || Y > other.Y + other.Height || Y + Height < other.Y);
    }
}
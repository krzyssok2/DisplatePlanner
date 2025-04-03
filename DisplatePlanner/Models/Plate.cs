using DisplatePlanner.Enums;

namespace DisplatePlanner.Models;

public class Plate
{
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;

    public double Width { get; set; }
    public double Height { get; set; }

    public PlateSize Size { get; set; }
    public bool IsHorizontal { get; set; }
    public int Rotation { get; set; } = 0;

    public string ImageUrl { get; set; }

    public Plate(string imageUrl, PlateSize size, bool isHorizontal)
    {
        ImageUrl = imageUrl;
        Size = size;
        IsHorizontal = isHorizontal;

        SetDimensions();
    }

    public void Rotate()
    {
        (Width, Height) = (Height, Width);
        Rotation = (Rotation + 90) % 360;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Plate other) return false;

        return ImageUrl == other.ImageUrl &&
               Size == other.Size &&
               IsHorizontal == other.IsHorizontal &&
               Width == other.Width &&
               Height == other.Height &&
               X == other.X &&
               Y == other.Y &&
               Rotation == other.Rotation;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ImageUrl, Size, IsHorizontal, Width, Height, X, Y, Rotation);
    }

    private void SetDimensions()
    {
        var sizeMap = new Dictionary<PlateSize, (double Width, double Height)>
        {
            { PlateSize.L, (67.5, 48) },
            { PlateSize.M, (45, 32) },
            { PlateSize.S, (15, 10) }
        };

        var (w, h) = sizeMap.GetValueOrDefault(Size, (45, 32)); // Default to M size
        (Width, Height) = IsHorizontal ? (w, h) : (h, w);
    }
}
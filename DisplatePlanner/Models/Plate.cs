using DisplatePlanner.Enums;
using System.Text.Json.Serialization;

namespace DisplatePlanner.Models;

public class Plate
{
    [JsonIgnore]
    public double Width { get; private set; }

    [JsonIgnore]
    public double Height { get; private set; }

    public double X { get; private set; }
    public double Y { get; private set; }  
    public int Rotation { get; private set; }

    public PlateSize Size { get; }
    public bool IsHorizontal { get; }
    public string ImageUrl { get; }

    public Plate(string imageUrl, PlateSize size, bool isHorizontal, double x = 0, double y = 0, int rotation = 0)
    {
        if (rotation % 90 != 0)
        {
            throw new ArgumentException("Rotation must be a multiple of 90 degrees.");
        }

        X = x;
        Y = y;

        ImageUrl = imageUrl;
        Size = size;
        IsHorizontal = isHorizontal;
        SetDimensions();

        if (rotation != 0)
        {
            int rotations = (rotation / 90) % 4;
            for (int i = 0; i < rotations; i++)
            {
                Rotate();
            }
        }
    }

    public void IncrementCoordinates(double x, double y)
    {
        X += x;
        Y += y;
    }

    public void SetCoordinates(double? x, double? y)
    {
        if (x != null)
        {
            SetX(x.Value);
        }

        if (y != null)
        {
            SetY(y.Value);
        }
    }

    public void SetX(double x) => X = x;

    public void SetY(double y) => Y = y;

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

        var (w, h) = sizeMap.GetValueOrDefault(Size, (45, 32));
        (Width, Height) = IsHorizontal ? (w, h) : (h, w);
    }
}
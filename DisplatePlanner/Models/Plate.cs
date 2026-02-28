using DisplatePlanner.Enums;
using System.Text.Json.Serialization;

namespace DisplatePlanner.Models;

public class Plate
{
    [JsonIgnore]
    public double Width => IsSideways ? BaseHeight : BaseWidth;

    [JsonIgnore]
    public double Height => IsSideways ? BaseWidth : BaseHeight;

    [JsonIgnore]
    public double BaseWidth { get; private set; }

    [JsonIgnore]
    public double BaseHeight { get; private set; }

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

        Rotation = ((rotation % 360) + 360) % 360;
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
        var (w, h) = Size switch
        {
            PlateSize.L => (67.5, 48d),
            PlateSize.M => (45d, 32d),
            PlateSize.S => (15d, 10d),
            _ => (45d, 32d),
        };

        (BaseWidth, BaseHeight) = IsHorizontal ? (w, h) : (h, w);
    }

    private bool IsSideways => Rotation is 90 or 270;
}
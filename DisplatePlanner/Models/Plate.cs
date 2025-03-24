namespace DisplatePlanner.Models;

public class Plate
{
    public Plate(string imageUrl, string type, bool isHorizontal)
    {
        ImageUrl = imageUrl;
        Type = type;
        IsHorizontal = isHorizontal;
        switch (type)
        {
            case "standard":
            case "M":
                if (isHorizontal)
                {
                    Width = 45;
                    Height = 32;
                }
                else
                {
                    Width = 32;
                    Height = 45;
                }
                break;

            case "ultra":
            case "L":
                if (isHorizontal)
                {
                    Width = 67.5;
                    Height = 48;
                }
                else
                {
                    Width = 48;
                    Height = 67.5;
                }
                break;
        }
    }

    public void Rotate()
    {
        var width = Width;
        var height = Height;

        Height = width;
        Width = height;
        Rotation = (Rotation + 90) % 360;
    }

    public double Width { get; set; } = 32;
    public double Height { get; set; } = 45;
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public string ImageUrl { get; set; }
    public string Type { get; set; }
    public bool IsHorizontal { get; set; }
    public int Rotation { get; set; } = 0;

    public override bool Equals(object? obj)
    {
        if (obj is not Plate other) return false;

        return ImageUrl == other.ImageUrl &&
               Type == other.Type &&
               IsHorizontal == other.IsHorizontal &&
               Width == other.Width &&
               Height == other.Height &&
               X == other.X &&
               Y == other.Y &&
               Rotation == other.Rotation;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ImageUrl, Type, IsHorizontal, Width, Height, X, Y, Rotation);
    }
}
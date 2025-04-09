using DisplatePlanner.Enums;

namespace DisplatePlanner.Models;

public record PlateData(ulong Id, DateTime Date, string Name, string ImageUrl, PlateType Type, bool IsHorizontal, PlateSize Size = PlateSize.M);
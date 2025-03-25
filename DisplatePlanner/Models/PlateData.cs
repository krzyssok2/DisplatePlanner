namespace DisplatePlanner.Models;

public record PlateData(ulong Id, DateTime StartDate, string Name, string ImageUrl, string Type, bool IsHorizontal);
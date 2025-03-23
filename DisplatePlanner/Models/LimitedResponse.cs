namespace DisplatePlanner.Models;

public record LimitedResponse(
    List<PlateDetails> Data);

public record PlateDetails(
    int Id,
    string Sku,
    int ItemCollectionId,
    string Title,
    string Url,
    Edition Edition,
    Images Images);

public record Edition(
    string StartDate,
    string EndDate,
    string Status,
    int Available,
    int Size,
    string Type,
    string Format
);

public record Images(
    Main Main);

public record Main(
    string Url,
    int Width,
    int Height);
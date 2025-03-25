namespace DisplatePlanner.Models;

public record RegularResponse
(
    Data Data
);

public record Data
(
    ulong ItemCollectionId,
    string Title,
    string ImageUrl,
    string Orientation
);
namespace DisplatePlanner.Models;

public record RegularResponse
(
    Data Data
);

public record Data
(
    string Title,
    string ImageUrl,
    string Orientation
);
namespace DisplatePlanner.Models;
public record LuminoResponse(LuminoListings LuminoListings);

public record LuminoListings
(
    List<PlateInfo> Data
);

public record PlateInfo
(
    DateTime StartDate,
    string Title,
    Image Image
);

public record Author
(
    int Id,
    string FullName,
    string Nick,
    string Avatar,
    string Url
);

public record Image(
    //string x1,
    string X2
);
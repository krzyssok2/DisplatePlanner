namespace DisplatePlanner.Models;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

public record LuminoResponse(LuminoListings LuminoListings);

public record LuminoListings
(
    List<PlateInfo> Data
);

public record PlateInfo
(
    //int internalId,
    //int externalId,
    DateTime StartDate,
    //DateTime finishDate ,
    //int availableQuantity ,
    //string status ,
    //Author author
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
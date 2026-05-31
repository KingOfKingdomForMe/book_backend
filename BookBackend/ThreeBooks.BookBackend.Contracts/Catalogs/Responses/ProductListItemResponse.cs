namespace ThreeBooks.BookBackend.Contracts.Catalogs.Responses;

public sealed record ProductListItemResponse(
    int Id,
    int CategoryId,
    string CategoryName,
    string CategorySlug,
    string SpuCode,
    string? DefaultAlbumCode,
    string Name,
    string? Subtitle,
    string? ContentSource,
    decimal StartingPrice,
    int UploadImageCount,
    int SortOrder);

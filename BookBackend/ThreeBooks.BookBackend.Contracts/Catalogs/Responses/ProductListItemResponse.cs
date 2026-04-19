namespace ThreeBooks.BookBackend.Contracts.Catalogs.Responses;

public sealed record ProductListItemResponse(
    int Id,
    int CategoryId,
    string CategoryName,
    string CategorySlug,
    string SpuCode,
    string Name,
    string? Subtitle,
    string? ContentSource,
    decimal StartingPrice,
    int SortOrder);

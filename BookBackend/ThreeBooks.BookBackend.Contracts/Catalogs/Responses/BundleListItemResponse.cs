namespace ThreeBooks.BookBackend.Contracts.Catalogs.Responses;

public sealed record BundleListItemResponse(
    int Id,
    string BundleCode,
    string Name,
    string? Description,
    decimal BundlePrice,
    decimal OriginalPrice,
    int SortOrder);

namespace ThreeBooks.BookBackend.Contracts.Catalogs.Responses;

public sealed record ProductDetailResponse(
    int Id,
    int CategoryId,
    string SpuCode,
    string Name,
    string? Subtitle,
    string? ContentSource,
    IReadOnlyCollection<SkuResponse> Skus);

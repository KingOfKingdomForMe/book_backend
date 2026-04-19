namespace ThreeBooks.BookBackend.Contracts.Catalogs.Responses;

public sealed record SkuResponse(
    int Id,
    string SkuCode,
    string? SizeLabel,
    string? BindingLabel,
    string? LayoutLabel,
    int MinPages,
    int? MaxPages,
    decimal BasePrice,
    decimal PageUnitPrice);

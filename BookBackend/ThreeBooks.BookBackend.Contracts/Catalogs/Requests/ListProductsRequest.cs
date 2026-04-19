namespace ThreeBooks.BookBackend.Contracts.Catalogs.Requests;

public sealed record ListProductsRequest(
    int? CategoryId,
    string? CategorySlug,
    string? Keyword,
    int PageNumber = 1,
    int PageSize = 20);

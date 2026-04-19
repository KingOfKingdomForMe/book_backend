namespace ThreeBooks.BookBackend.Application.Modules.Catalogs.Models;

public sealed record ProductListFilter(
    int? CategoryId,
    string? CategorySlug,
    string? Keyword,
    int PageNumber,
    int PageSize);

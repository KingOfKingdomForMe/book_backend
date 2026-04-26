namespace ThreeBooks.BookBackend.Contracts.Unboxings.Requests;

public sealed record AdminListUnboxingsRequest(
    string? Keyword = null,
    string? LevelCode = null,
    bool? IsFeatured = null,
    int? Status = null,
    int PageNumber = 1,
    int PageSize = 20);
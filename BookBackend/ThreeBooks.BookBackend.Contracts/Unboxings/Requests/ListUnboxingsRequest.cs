namespace ThreeBooks.BookBackend.Contracts.Unboxings.Requests;

public sealed record ListUnboxingsRequest(
    string? LevelCode = null,
    bool? IsFeatured = null,
    int PageNumber = 1,
    int PageSize = 20);
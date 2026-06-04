namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

public sealed record DefaultAlbumListItemResponse(
    long AlbumId,
    string AlbumCode,
    string ProductCode,
    string Name,
    string? Description,
    string? BookType,
    string? Category,
    string? ThemeCode,
    IReadOnlyCollection<string> ExtraProperties,
    string? PreviewUrl,
    int TemplateCount,
    bool IsActive,
    int SortOrder,
    DateTime UpdatedAtUtc);
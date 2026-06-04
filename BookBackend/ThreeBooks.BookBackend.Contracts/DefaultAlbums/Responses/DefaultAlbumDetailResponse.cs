namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

public sealed record DefaultAlbumDetailResponse(
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
    long? PreviewFileId,
    long? CreatedByUserId,
    int TemplateCount,
    bool IsActive,
    int SortOrder,
    IReadOnlyCollection<DefaultAlbumTemplateItemResponse> Templates,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record AlbumPreviewDetailResponse(
    string ShareCode,
    string Title,
    string? Subtitle,
    int PageCount,
    long ViewCount,
    long ShareCount,
    string ProductCode,
    string ShareUrl,
    IReadOnlyCollection<AlbumPreviewPageSummaryResponse> Pages);

public sealed record AlbumPreviewPageSummaryResponse(
    int PageNo,
    string PageLabel,
    string PageType,
    string? ThumbnailUrl,
    string JsonSource,
    bool HasImages);
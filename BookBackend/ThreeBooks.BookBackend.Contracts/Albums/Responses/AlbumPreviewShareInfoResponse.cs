namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record AlbumPreviewShareInfoResponse(
    string ShareCode,
    string ShareUrl,
    string Title,
    string? Subtitle,
    string? CoverThumbnailUrl,
    int PageCount,
    long ViewCount,
    long ShareCount,
    string ProductCode);
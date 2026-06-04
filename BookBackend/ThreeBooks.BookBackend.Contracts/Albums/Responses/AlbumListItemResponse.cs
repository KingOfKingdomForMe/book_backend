namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record AlbumListItemResponse(
    long ProjectId,
    string? ShareCode,
    string Title,
    string? Subtitle,
    string BookType,
    string ProductCode,
    bool IsPublic,
    int PageCount,
    int ImageCount,
    int UploadedImageCount,
    IReadOnlyCollection<string> ExtraProperties,
    long ViewCount,
    long ShareCount,
    string? PreviewUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
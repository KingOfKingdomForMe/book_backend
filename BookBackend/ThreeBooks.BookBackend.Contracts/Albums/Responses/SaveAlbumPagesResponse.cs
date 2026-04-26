namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record SaveAlbumPagesResponse(
    long ProjectId,
    long VersionId,
    string ShareCode,
    int PageCount,
    int ImageCount,
    string? PreviewUrl);
namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record CreateAlbumResponse(
    long ProjectId,
    long VersionId,
    string ShareCode,
    string? PreviewUrl);
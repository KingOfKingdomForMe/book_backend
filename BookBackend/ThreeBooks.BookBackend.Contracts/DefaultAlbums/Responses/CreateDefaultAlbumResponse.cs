namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

public sealed record CreateDefaultAlbumResponse(
    long AlbumId,
    string AlbumCode,
    bool IsActive,
    string? PreviewUrl);
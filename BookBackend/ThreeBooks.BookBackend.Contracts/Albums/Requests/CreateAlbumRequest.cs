namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

public sealed record CreateAlbumRequest(
    long UserId,
    string? ShareCode,
    string Title,
    string? Subtitle,
    bool IsPublic = true);
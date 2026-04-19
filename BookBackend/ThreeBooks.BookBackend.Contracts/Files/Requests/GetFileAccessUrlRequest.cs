namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

public sealed record GetFileAccessUrlRequest(
    string? Bucket,
    string ObjectKey,
    int ExpiresInMinutes = 60);
namespace ThreeBooks.BookBackend.Contracts.Files.Responses;

public sealed record FileAccessUrlResponse(
    string Bucket,
    string ObjectKey,
    string Url,
    DateTimeOffset ExpiresAtUtc);
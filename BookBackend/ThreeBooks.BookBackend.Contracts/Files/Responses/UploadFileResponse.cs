namespace ThreeBooks.BookBackend.Contracts.Files.Responses;

public sealed record UploadFileResponse(
    string Bucket,
    string ObjectKey,
    string FileName,
    string? ContentType,
    long ContentLength,
    string? ProxyUrl = null,
    string? AccessUrl = null);
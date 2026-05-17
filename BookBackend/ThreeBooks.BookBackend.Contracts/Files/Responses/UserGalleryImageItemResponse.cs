namespace ThreeBooks.BookBackend.Contracts.Files.Responses;

public sealed record UserGalleryImageItemResponse(
    long FileId,
    string Bucket,
    string ObjectKey,
    string FileName,
    string? OriginalFileName,
    string? ContentType,
    long ContentLength,
    DateTime UploadedAtUtc,
    string? ProxyUrl = null,
    string? AccessUrl = null);
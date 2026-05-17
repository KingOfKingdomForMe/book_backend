namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

public sealed record UploadUserGalleryImageRequest(
    string? Bucket,
    string? FileName = null);
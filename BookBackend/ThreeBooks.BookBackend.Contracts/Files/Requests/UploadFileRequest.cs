namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

public sealed record UploadFileRequest(
    string? Bucket,
    string? Directory,
    string? ObjectKey,
    string? FileName);
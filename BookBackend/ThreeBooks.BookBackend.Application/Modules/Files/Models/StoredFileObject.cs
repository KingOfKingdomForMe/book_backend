namespace ThreeBooks.BookBackend.Application.Modules.Files.Models;

public sealed record StoredFileObject(
    string Bucket,
    string ObjectKey,
    string FileName,
    string? ContentType,
    long ContentLength);
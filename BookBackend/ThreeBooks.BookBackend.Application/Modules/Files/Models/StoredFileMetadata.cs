namespace ThreeBooks.BookBackend.Application.Modules.Files.Models;

public sealed record StoredFileMetadata(
    string? OriginalFileName,
    string FileName,
    string? FileExtension,
    string? ContentType,
    long ContentLength);
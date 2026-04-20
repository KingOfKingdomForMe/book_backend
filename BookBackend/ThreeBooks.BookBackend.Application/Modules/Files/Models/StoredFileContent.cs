namespace ThreeBooks.BookBackend.Application.Modules.Files.Models;

public sealed record StoredFileContent(
    Stream Content,
    string? ContentType,
    long? ContentLength,
    string FileName,
    IDisposable? Lease = null);
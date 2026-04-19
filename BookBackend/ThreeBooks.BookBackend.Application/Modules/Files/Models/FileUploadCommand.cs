namespace ThreeBooks.BookBackend.Application.Modules.Files.Models;

public sealed record FileUploadCommand(
    string Bucket,
    string ObjectKey,
    string FileName,
    string? ContentType,
    long ContentLength,
    Stream Content);
namespace ThreeBooks.BookBackend.Api.Controllers.Files;

public sealed class UploadFileForm
{
    public string? Bucket { get; init; }

    public string? Directory { get; init; }

    public string? ObjectKey { get; init; }

    public string? FileName { get; init; }

    public IFormFile? File { get; init; }
}
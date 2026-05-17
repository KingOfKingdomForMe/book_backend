namespace ThreeBooks.BookBackend.Api.Controllers.Files;

public sealed class UploadUserGalleryImageForm
{
    public string? Bucket { get; init; }

    public string? FileName { get; init; }

    public IFormFile? File { get; init; }
}
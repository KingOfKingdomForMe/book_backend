namespace ThreeBooks.BookBackend.Application.Modules.Files.Models;

public sealed record UserGalleryImageListFilter(
    long UserId,
    int PageNumber,
    int PageSize);

public sealed record UserGalleryImageQueryModel(
    long FileId,
    string Bucket,
    string ObjectKey,
    string FileName,
    string? OriginalFileName,
    string? ContentType,
    long ContentLength,
    DateTime UploadedAtUtc);

public static class UserGalleryPathBuilder
{
    public static string BuildDirectoryPrefix(long userId)
    {
        return $"users/{userId}/gallery/images";
    }
}
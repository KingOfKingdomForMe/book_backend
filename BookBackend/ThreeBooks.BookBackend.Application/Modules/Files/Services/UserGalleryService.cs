using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Services;

public sealed class UserGalleryService(
    IUserGalleryQueryStore queryStore,
    IFileStorageService fileStorageService) : IUserGalleryService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> AllowedImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
        ".bmp",
        ".svg",
        ".heic",
        ".heif",
        ".avif"
    ];

    public async Task<UserGalleryImageItemResponse> UploadImageAsync(
        long userId,
        UploadUserGalleryImageRequest request,
        Stream content,
        string originalFileName,
        string? contentType,
        long contentLength,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);

        var normalizedUserId = NormalizePositiveId(userId, nameof(userId));
        await EnsureUserExistsAsync(normalizedUserId, cancellationToken);
        ValidateImageUpload(originalFileName, contentType);

        var uploaded = await fileStorageService.UploadAsync(
            new UploadFileRequest(
                request.Bucket,
                UserGalleryPathBuilder.BuildDirectoryPrefix(normalizedUserId),
                null,
                request.FileName),
            content,
            originalFileName,
            contentType,
            contentLength,
            context,
            cancellationToken);

        var image = await queryStore.GetByBucketObjectKeyAsync(
            normalizedUserId,
            uploaded.Bucket,
            uploaded.ObjectKey,
            cancellationToken);

        return image is null
            ? throw new InvalidOperationException("Uploaded gallery image metadata could not be loaded.")
            : MapItem(image);
    }

    public async Task<PagedResult<UserGalleryImageItemResponse>> GetImagesAsync(
        long userId,
        ListUserGalleryImagesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedUserId = NormalizePositiveId(userId, nameof(userId));
        await EnsureUserExistsAsync(normalizedUserId, cancellationToken);

        var filter = new UserGalleryImageListFilter(
            normalizedUserId,
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetListAsync(filter, cancellationToken);

        return new PagedResult<UserGalleryImageItemResponse>(
            result.Items.Select(MapItem).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<bool> DeleteImageAsync(
        long userId,
        long fileId,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedUserId = NormalizePositiveId(userId, nameof(userId));
        var normalizedFileId = NormalizePositiveId(fileId, nameof(fileId));

        await EnsureUserExistsAsync(normalizedUserId, cancellationToken);

        var image = await queryStore.GetByIdAsync(normalizedUserId, normalizedFileId, cancellationToken);
        if (image is null)
        {
            return false;
        }

        return await fileStorageService.DeleteAsync(
            image.Bucket,
            image.ObjectKey,
            context,
            cancellationToken);
    }

    private async Task EnsureUserExistsAsync(long userId, CancellationToken cancellationToken)
    {
        if (!await queryStore.UserExistsAsync(userId, cancellationToken))
        {
            throw new ArgumentException("User does not exist.", nameof(userId));
        }
    }

    private static UserGalleryImageItemResponse MapItem(UserGalleryImageQueryModel item)
    {
        return new UserGalleryImageItemResponse(
            item.FileId,
            item.Bucket,
            item.ObjectKey,
            item.FileName,
            item.OriginalFileName,
            item.ContentType,
            item.ContentLength,
            item.UploadedAtUtc);
    }

    private static long NormalizePositiveId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException("A positive identifier is required.", parameterName);
        }

        return value;
    }

    private static int NormalizePageNumber(int pageNumber)
    {
        return pageNumber <= 0 ? DefaultPageNumber : pageNumber;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }

    private static void ValidateImageUpload(string originalFileName, string? contentType)
    {
        var normalizedContentType = contentType?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedContentType)
            && normalizedContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var extension = Path.GetExtension(originalFileName);
        if (AllowedImageExtensions.Contains(extension))
        {
            return;
        }

        throw new ArgumentException("Only image files can be uploaded to the user gallery.", nameof(contentType));
    }
}
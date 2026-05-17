using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;

public interface IUserGalleryService
{
    Task<UserGalleryImageItemResponse> UploadImageAsync(
        long userId,
        UploadUserGalleryImageRequest request,
        Stream content,
        string originalFileName,
        string? contentType,
        long contentLength,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<PagedResult<UserGalleryImageItemResponse>> GetImagesAsync(
        long userId,
        ListUserGalleryImagesRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<bool> DeleteImageAsync(
        long userId,
        long fileId,
        RequestContext context,
        CancellationToken cancellationToken);
}
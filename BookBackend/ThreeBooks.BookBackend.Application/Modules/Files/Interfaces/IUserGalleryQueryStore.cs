using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;

public interface IUserGalleryQueryStore
{
    Task<bool> UserExistsAsync(
        long userId,
        CancellationToken cancellationToken);

    Task<PagedResult<UserGalleryImageQueryModel>> GetListAsync(
        UserGalleryImageListFilter filter,
        CancellationToken cancellationToken);

    Task<UserGalleryImageQueryModel?> GetByIdAsync(
        long userId,
        long fileId,
        CancellationToken cancellationToken);

    Task<UserGalleryImageQueryModel?> GetByBucketObjectKeyAsync(
        long userId,
        string bucket,
        string objectKey,
        CancellationToken cancellationToken);
}
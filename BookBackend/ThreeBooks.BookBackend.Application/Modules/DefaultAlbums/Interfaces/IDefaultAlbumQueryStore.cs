using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;

public interface IDefaultAlbumQueryStore
{
    Task<PagedResult<DefaultAlbumListItemQueryModel>> GetListAsync(
        DefaultAlbumListFilter filter,
        CancellationToken cancellationToken);

    Task<DefaultAlbumDetailQueryModel?> GetDetailAsync(
        string albumCode,
        CancellationToken cancellationToken);

    Task<DefaultAlbumDetailQueryModel?> GetActiveByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken);

    Task<DefaultAlbumCreateResultModel> CreateAsync(
        DefaultAlbumCreateCommandModel command,
        CancellationToken cancellationToken);

    Task<DefaultAlbumDetailQueryModel?> UpdateAsync(
        string albumCode,
        DefaultAlbumUpdateCommandModel command,
        CancellationToken cancellationToken);
}
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;

public interface IDefaultAlbumService
{
    Task<PagedResult<DefaultAlbumListItemResponse>> GetListAsync(
        ListDefaultAlbumsRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<DefaultAlbumDetailResponse?> GetDetailAsync(
        string albumCode,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<CreateDefaultAlbumResponse> CreateAsync(
        CreateDefaultAlbumRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<DefaultAlbumDetailResponse?> UpdateAsync(
        string albumCode,
        UpdateDefaultAlbumRequest request,
        RequestContext context,
        CancellationToken cancellationToken);
}
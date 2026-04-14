using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;

public interface IAlbumTemplateQueryStore
{
    Task<PagedResult<AlbumTemplateListItemResponse>> GetListAsync(
        AlbumTemplateListFilter filter,
        CancellationToken cancellationToken);
}
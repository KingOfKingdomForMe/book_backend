using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;

public interface IAlbumTemplateQueryStore
{
    Task<PagedResult<AlbumTemplateListItemQueryModel>> GetListAsync(
        AlbumTemplateListFilter filter,
        CancellationToken cancellationToken);

    Task<AlbumTemplateDetailQueryModel?> GetDetailAsync(
        string templateCode,
        CancellationToken cancellationToken);

    Task<AlbumTemplateCreateResultModel> CreateAsync(
        AlbumTemplateCreateCommandModel command,
        CancellationToken cancellationToken);
}
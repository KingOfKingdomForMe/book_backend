using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.AlbumTemplates;

public sealed class AlbumTemplateQueryStore : IAlbumTemplateQueryStore
{
    public Task<PagedResult<AlbumTemplateListItemResponse>> GetListAsync(
        AlbumTemplateListFilter filter,
        CancellationToken cancellationToken)
    {
        var response = new PagedResult<AlbumTemplateListItemResponse>(
            Array.Empty<AlbumTemplateListItemResponse>(),
            filter.PageNumber,
            filter.PageSize,
            0);

        return Task.FromResult(response);
    }
}
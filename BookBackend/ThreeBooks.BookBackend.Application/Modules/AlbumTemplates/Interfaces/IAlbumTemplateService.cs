using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;

public interface IAlbumTemplateService
{
    Task<PagedResult<AlbumTemplateListItemResponse>> GetListAsync(
        ListAlbumTemplatesRequest request,
        RequestContext context,
        CancellationToken cancellationToken);
}
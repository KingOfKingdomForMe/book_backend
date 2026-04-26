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

    Task<AlbumTemplateDetailResponse?> GetDetailAsync(
        string templateCode,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<CreateAlbumTemplateResponse> CreateAsync(
        CreateAlbumTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<AlbumTemplateDetailResponse?> UpdateAsync(
        string templateCode,
        UpdateAlbumTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken);
}
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Interfaces;

public interface ICoverTemplateService
{
    Task<PagedResult<CoverTemplateListItemResponse>> GetListAsync(
        ListCoverTemplatesRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<CoverTemplateDetailResponse?> GetDetailAsync(
        string templateCode,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<CreateCoverTemplateResponse> CreateAsync(
        CreateCoverTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<CoverTemplateDetailResponse?> UpdateAsync(
        string templateCode,
        UpdateCoverTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<GeneratedCoverImageResponse?> GenerateAsync(
        string templateCode,
        GenerateCoverImageRequest request,
        RequestContext context,
        CancellationToken cancellationToken);
}
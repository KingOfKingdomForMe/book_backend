using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Services;

public sealed class AlbumTemplateService(IAlbumTemplateQueryStore queryStore) : IAlbumTemplateService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public Task<PagedResult<AlbumTemplateListItemResponse>> GetListAsync(
        ListAlbumTemplatesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var filter = new AlbumTemplateListFilter(
            NormalizeKeyword(request.Keyword),
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        return queryStore.GetListAsync(filter, cancellationToken);
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        var normalized = keyword?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int NormalizePageNumber(int pageNumber)
    {
        return pageNumber < 1 ? DefaultPageNumber : pageNumber;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1)
        {
            return DefaultPageSize;
        }

        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }
}
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Models;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.Catalogs.Services;

public sealed class CatalogService(ICatalogQueryStore queryStore) : ICatalogService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public Task<PagedResult<CategoryResponse>> GetCategoriesAsync(
        ListCategoriesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var filter = new CategoryListFilter(
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        return queryStore.GetCategoriesAsync(filter, cancellationToken);
    }

    public Task<PagedResult<ProductListItemResponse>> GetProductsAsync(
        ListProductsRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var filter = new ProductListFilter(
            request.CategoryId,
            NormalizeSlug(request.CategorySlug),
            NormalizeKeyword(request.Keyword),
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        return queryStore.GetProductsAsync(filter, cancellationToken);
    }

    public Task<ProductDetailResponse?> GetProductDetailAsync(
        int spuId,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return queryStore.GetProductDetailAsync(spuId, cancellationToken);
    }

    public Task<PagedResult<BundleListItemResponse>> GetBundlesAsync(
        ListBundlesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var filter = new BundleListFilter(
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        return queryStore.GetBundlesAsync(filter, cancellationToken);
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        var normalized = keyword?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeSlug(string? slug)
    {
        var normalized = slug?.Trim().ToLowerInvariant();
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

using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;

public interface ICatalogService
{
    Task<PagedResult<CategoryResponse>> GetCategoriesAsync(
        ListCategoriesRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<PagedResult<ProductListItemResponse>> GetProductsAsync(
        ListProductsRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<ProductDetailResponse?> GetProductDetailAsync(
        int spuId,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<PagedResult<BundleListItemResponse>> GetBundlesAsync(
        ListBundlesRequest request,
        RequestContext context,
        CancellationToken cancellationToken);
}

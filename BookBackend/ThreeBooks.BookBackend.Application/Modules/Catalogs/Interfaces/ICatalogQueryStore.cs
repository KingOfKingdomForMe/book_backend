using ThreeBooks.BookBackend.Application.Modules.Catalogs.Models;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;

public interface ICatalogQueryStore
{
    Task<PagedResult<CategoryResponse>> GetCategoriesAsync(
        CategoryListFilter filter,
        CancellationToken cancellationToken);

    Task<PagedResult<ProductListItemResponse>> GetProductsAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken);

    Task<ProductDetailResponse?> GetProductDetailAsync(
        int spuId,
        CancellationToken cancellationToken);

    Task<PagedResult<BundleListItemResponse>> GetBundlesAsync(
        BundleListFilter filter,
        CancellationToken cancellationToken);
}

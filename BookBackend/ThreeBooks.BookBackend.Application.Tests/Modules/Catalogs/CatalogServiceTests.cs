using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Models;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Services;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;
using Xunit;

namespace ThreeBooks.BookBackend.Application.Tests.Modules.Catalogs;

public sealed class CatalogServiceTests
{
    private static readonly RequestContext Context = new(null, null);

    [Fact]
    public async Task GetProductsAsync_ReturnsDefaultAlbumCode()
    {
        var queryStore = new FakeCatalogQueryStore
        {
            GetProductsHandler = filter => Task.FromResult(new PagedResult<ProductListItemResponse>(
            [
                new ProductListItemResponse(
                    9,
                    2,
                    "照片书",
                    "photo-book",
                    "xcalbum",
                    "default-xcalbum",
                    "A4轻奢杂志册",
                    "大气杂志画册",
                    "any",
                    48,
                    48,
                    9)
            ],
            filter.PageNumber,
            filter.PageSize,
            1))
        };

        var service = new CatalogService(queryStore);

        var response = await service.GetProductsAsync(
            new ListProductsRequest(null, null, null),
            Context,
            CancellationToken.None);

        var item = Assert.Single(response.Items);
        Assert.Equal("xcalbum", item.SpuCode);
        Assert.Equal("default-xcalbum", item.DefaultAlbumCode);
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsDefaultAlbumCode()
    {
        var queryStore = new FakeCatalogQueryStore
        {
            GetProductDetailHandler = spuId => Task.FromResult<ProductDetailResponse?>(new ProductDetailResponse(
                spuId,
                2,
                "xcalbum",
                "default-xcalbum",
                "A4轻奢杂志册",
                "大气杂志画册",
                "any",
                []))
        };

        var service = new CatalogService(queryStore);

        var response = await service.GetProductDetailAsync(9, Context, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("default-xcalbum", response!.DefaultAlbumCode);
    }

    private sealed class FakeCatalogQueryStore : ICatalogQueryStore
    {
        public Func<CategoryListFilter, Task<PagedResult<CategoryResponse>>>? GetCategoriesHandler { get; init; }

        public Func<ProductListFilter, Task<PagedResult<ProductListItemResponse>>>? GetProductsHandler { get; init; }

        public Func<int, Task<ProductDetailResponse?>>? GetProductDetailHandler { get; init; }

        public Func<BundleListFilter, Task<PagedResult<BundleListItemResponse>>>? GetBundlesHandler { get; init; }

        public Task<PagedResult<CategoryResponse>> GetCategoriesAsync(CategoryListFilter filter, CancellationToken cancellationToken)
        {
            return GetCategoriesHandler is null
                ? Task.FromResult(new PagedResult<CategoryResponse>([], filter.PageNumber, filter.PageSize, 0))
                : GetCategoriesHandler(filter);
        }

        public Task<PagedResult<ProductListItemResponse>> GetProductsAsync(ProductListFilter filter, CancellationToken cancellationToken)
        {
            return GetProductsHandler is null
                ? Task.FromResult(new PagedResult<ProductListItemResponse>([], filter.PageNumber, filter.PageSize, 0))
                : GetProductsHandler(filter);
        }

        public Task<ProductDetailResponse?> GetProductDetailAsync(int spuId, CancellationToken cancellationToken)
        {
            return GetProductDetailHandler is null
                ? Task.FromResult<ProductDetailResponse?>(null)
                : GetProductDetailHandler(spuId);
        }

        public Task<PagedResult<BundleListItemResponse>> GetBundlesAsync(BundleListFilter filter, CancellationToken cancellationToken)
        {
            return GetBundlesHandler is null
                ? Task.FromResult(new PagedResult<BundleListItemResponse>([], filter.PageNumber, filter.PageSize, 0))
                : GetBundlesHandler(filter);
        }
    }
}
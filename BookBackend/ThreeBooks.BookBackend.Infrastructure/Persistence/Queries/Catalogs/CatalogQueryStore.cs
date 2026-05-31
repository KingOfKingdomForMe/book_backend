using System.Data;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Models;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Catalogs;

public sealed class CatalogQueryStore(string connectionString) : ICatalogQueryStore
{
    private const string GetCategoriesCountProcedure = "usp_Catalog_GetCategories_Count";
    private const string GetCategoriesListProcedure = "usp_Catalog_GetCategories_List";
    private const string GetProductsCountProcedure = "usp_Catalog_GetProducts_Count";
    private const string GetProductsListProcedure = "usp_Catalog_GetProducts_List";
    private const string GetProductDetailHeaderProcedure = "usp_Catalog_GetProductDetail_Header";
    private const string GetProductDetailSkusProcedure = "usp_Catalog_GetProductDetail_Skus";
    private const string GetBundlesCountProcedure = "usp_Catalog_GetBundles_Count";
    private const string GetBundlesListProcedure = "usp_Catalog_GetBundles_List";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Catalog database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<PagedResult<CategoryResponse>> GetCategoriesAsync(
        CategoryListFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            p_page_size = filter.PageSize,
            p_offset = (filter.PageNumber - 1) * filter.PageSize
        };

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                GetCategoriesCountProcedure,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<CategoryRow>(
            new CommandDefinition(
                GetCategoriesListProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var categoryRows = rows.ToArray();

        var items = categoryRows
            .Select(row => new CategoryResponse(
                checked((int)row.Id),
                row.Name,
                row.Slug,
                row.SortOrder,
                checked((int)row.ProductCount)))
            .ToArray();

        return new PagedResult<CategoryResponse>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    public async Task<PagedResult<ProductListItemResponse>> GetProductsAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            p_category_id = filter.CategoryId,
            p_category_slug = filter.CategorySlug,
            p_keyword = filter.Keyword,
            p_page_size = filter.PageSize,
            p_offset = (filter.PageNumber - 1) * filter.PageSize
        };

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                GetProductsCountProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<ProductListItemRow>(
            new CommandDefinition(
                GetProductsListProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var items = rows
            .Select(MapProduct)
            .ToArray();

        return new PagedResult<ProductListItemResponse>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    public async Task<ProductDetailResponse?> GetProductDetailAsync(
        int spuId,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductDetailHeader>(
            new CommandDefinition(
                GetProductDetailHeaderProcedure,
                new { p_spu_id = spuId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var skuRows = await connection.QueryAsync<SkuRow>(
            new CommandDefinition(
                GetProductDetailSkusProcedure,
                new { p_spu_id = spuId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var skus = skuRows
            .Select(row => new SkuResponse(
                checked((int)row.Id),
                row.SkuCode,
                row.SizeLabel,
                row.BindingLabel,
                row.LayoutLabel,
                row.MinPages,
                row.MaxPages,
                row.BasePrice,
                row.PageUnitPrice))
            .ToArray();

        return new ProductDetailResponse(
            checked((int)header.Id),
            checked((int)header.CategoryId),
            header.SpuCode,
            header.DefaultAlbumCode,
            header.Name,
            header.Subtitle,
            header.ContentSource,
            skus);
    }

    public async Task<PagedResult<BundleListItemResponse>> GetBundlesAsync(
        BundleListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                GetBundlesCountProcedure,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<BundleRow>(
            new CommandDefinition(
                GetBundlesListProcedure,
                new
                {
                    p_page_size = filter.PageSize,
                    p_offset = (filter.PageNumber - 1) * filter.PageSize
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var items = rows
            .Select(row => new BundleListItemResponse(
                checked((int)row.Id),
                row.BundleCode,
                row.Name,
                row.Description,
                row.BundlePrice,
                row.OriginalPrice,
                row.SortOrder))
            .ToArray();

        return new PagedResult<BundleListItemResponse>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static ProductListItemResponse MapProduct(ProductListItemRow row)
    {
        return new ProductListItemResponse(
            checked((int)row.Id),
            checked((int)row.CategoryId),
            row.CategoryName,
            row.CategorySlug,
            row.SpuCode,
            row.DefaultAlbumCode,
            row.Name,
            row.Subtitle,
            row.ContentSource,
            row.StartingPrice,
                checked((int)row.UploadImageCount),
            row.SortOrder);
    }

    private sealed class CategoryRow
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public long ProductCount { get; set; }
    }

    private sealed record ProductListItemRow(
        long Id,
        long CategoryId,
        string CategoryName,
        string CategorySlug,
        string SpuCode,
        string? DefaultAlbumCode,
        string Name,
        string? Subtitle,
        string? ContentSource,
        decimal StartingPrice,
        long UploadImageCount,
        int SortOrder);

    private sealed record ProductDetailHeader(
        long Id,
        long CategoryId,
        string SpuCode,
        string? DefaultAlbumCode,
        string Name,
        string? Subtitle,
        string? ContentSource);

    private sealed record SkuRow(
        long Id,
        string SkuCode,
        string? SizeLabel,
        string? BindingLabel,
        string? LayoutLabel,
        int MinPages,
        int? MaxPages,
        decimal BasePrice,
        decimal PageUnitPrice);

    private sealed record BundleRow(
        long Id,
        string BundleCode,
        string Name,
        string? Description,
        decimal BundlePrice,
        decimal OriginalPrice,
        int SortOrder);
}

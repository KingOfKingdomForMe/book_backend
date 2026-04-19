using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Models;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Catalogs;

public sealed class CatalogQueryStore(string connectionString) : ICatalogQueryStore
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Catalog database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<PagedResult<CategoryResponse>> GetCategoriesAsync(
        CategoryListFilter filter,
        CancellationToken cancellationToken)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM catalog_category c
            WHERE c.is_active = 1;
            """;

        const string listSql = """
            SELECT
                c.id AS Id,
                c.name AS Name,
                c.slug AS Slug,
                c.sort_order AS SortOrder,
                COUNT(spu.id) AS ProductCount
            FROM catalog_category c
            LEFT JOIN catalog_product_spu spu ON spu.category_id = c.id AND spu.is_active = 1
            WHERE c.is_active = 1
            GROUP BY c.id, c.name, c.slug, c.sort_order
            ORDER BY c.sort_order, c.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        const string productSql = """
            SELECT
                spu.id AS Id,
                spu.category_id AS CategoryId,
                category.name AS CategoryName,
                category.slug AS CategorySlug,
                spu.spu_code AS SpuCode,
                spu.name AS Name,
                spu.subtitle AS Subtitle,
                spu.content_source AS ContentSource,
                COALESCE(price.starting_price, 0) AS StartingPrice,
                spu.sort_order AS SortOrder
            FROM catalog_product_spu spu
            INNER JOIN catalog_category category ON category.id = spu.category_id AND category.is_active = 1
            LEFT JOIN (
                SELECT
                    sku.spu_id,
                    MIN(pr.base_price + (pr.page_unit_price * sku.min_pages)) AS starting_price
                FROM catalog_product_sku sku
                INNER JOIN catalog_price_rule pr ON pr.sku_id = sku.id
                    AND pr.is_active = 1
                    AND pr.effective_from <= UTC_TIMESTAMP()
                    AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
                WHERE sku.is_active = 1
                GROUP BY sku.spu_id
            ) price ON price.spu_id = spu.id
            WHERE spu.is_active = 1
              AND spu.category_id IN @CategoryIds
            ORDER BY category.sort_order, spu.sort_order, spu.id;
            """;

        var parameters = new
        {
            filter.PageSize,
            Offset = (filter.PageNumber - 1) * filter.PageSize
        };

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<CategoryRow>(
            new CommandDefinition(
                listSql,
                parameters,
                cancellationToken: cancellationToken));

        var categoryRows = rows.ToArray();
        var categoryIds = categoryRows.Select(row => row.Id).ToArray();

        IReadOnlyDictionary<int, IReadOnlyCollection<ProductListItemResponse>> productsByCategory;

        if (categoryIds.Length == 0)
        {
            productsByCategory = new Dictionary<int, IReadOnlyCollection<ProductListItemResponse>>();
        }
        else
        {
            var productRows = await connection.QueryAsync<ProductListItemRow>(
                new CommandDefinition(
                    productSql,
                    new { CategoryIds = categoryIds },
                    cancellationToken: cancellationToken));

            productsByCategory = productRows
                .Select(MapProduct)
                .GroupBy(product => product.CategoryId)
                .ToDictionary(group => group.Key, group => (IReadOnlyCollection<ProductListItemResponse>)group.ToArray());
        }

        var items = categoryRows
            .Select(row => new CategoryResponse(
                checked((int)row.Id),
                row.Name,
                row.Slug,
                row.SortOrder,
                checked((int)row.ProductCount),
                productsByCategory.GetValueOrDefault(checked((int)row.Id), Array.Empty<ProductListItemResponse>())))
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
        const string countSql = """
            SELECT COUNT(*)
                        FROM catalog_product_spu spu
                        INNER JOIN catalog_category category ON category.id = spu.category_id AND category.is_active = 1
            WHERE spu.is_active = 1
              AND (@CategoryId IS NULL OR spu.category_id = @CategoryId)
                            AND (@CategorySlug IS NULL OR category.slug = @CategorySlug)
              AND (
                    @Keyword IS NULL
                    OR spu.spu_code LIKE CONCAT('%', @Keyword, '%')
                    OR spu.name LIKE CONCAT('%', @Keyword, '%')
                    OR spu.subtitle LIKE CONCAT('%', @Keyword, '%')
                  );
            """;

        const string listSql = """
            SELECT
                spu.id AS Id,
                spu.category_id AS CategoryId,
                category.name AS CategoryName,
                category.slug AS CategorySlug,
                spu.spu_code AS SpuCode,
                spu.name AS Name,
                spu.subtitle AS Subtitle,
                spu.content_source AS ContentSource,
                COALESCE(price.starting_price, 0) AS StartingPrice,
                spu.sort_order AS SortOrder
            FROM catalog_product_spu spu
            INNER JOIN catalog_category category ON category.id = spu.category_id AND category.is_active = 1
            LEFT JOIN (
                SELECT
                    sku.spu_id,
                    MIN(pr.base_price + (pr.page_unit_price * sku.min_pages)) AS starting_price
                FROM catalog_product_sku sku
                INNER JOIN catalog_price_rule pr ON pr.sku_id = sku.id
                    AND pr.is_active = 1
                    AND pr.effective_from <= UTC_TIMESTAMP()
                    AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
                WHERE sku.is_active = 1
                GROUP BY sku.spu_id
            ) price ON price.spu_id = spu.id
            WHERE spu.is_active = 1
              AND (@CategoryId IS NULL OR spu.category_id = @CategoryId)
              AND (@CategorySlug IS NULL OR category.slug = @CategorySlug)
              AND (
                    @Keyword IS NULL
                    OR spu.spu_code LIKE CONCAT('%', @Keyword, '%')
                    OR spu.name LIKE CONCAT('%', @Keyword, '%')
                    OR spu.subtitle LIKE CONCAT('%', @Keyword, '%')
                  )
            ORDER BY category.sort_order, spu.sort_order, spu.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        var parameters = new
        {
            filter.CategoryId,
            filter.CategorySlug,
            filter.Keyword,
            filter.PageSize,
            Offset = (filter.PageNumber - 1) * filter.PageSize
        };

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<ProductListItemRow>(
            new CommandDefinition(listSql, parameters, cancellationToken: cancellationToken));

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
        const string detailSql = """
            SELECT
                spu.id AS Id,
                spu.category_id AS CategoryId,
                spu.spu_code AS SpuCode,
                spu.name AS Name,
                spu.subtitle AS Subtitle,
                spu.content_source AS ContentSource
            FROM catalog_product_spu spu
            WHERE spu.id = @SpuId
              AND spu.is_active = 1;
            """;

        const string skuSql = """
            SELECT
                sku.id AS Id,
                sku.sku_code AS SkuCode,
                size_value.value_label AS SizeLabel,
                binding_value.value_label AS BindingLabel,
                layout_value.value_label AS LayoutLabel,
                sku.min_pages AS MinPages,
                sku.max_pages AS MaxPages,
                COALESCE(price.base_price, 0) AS BasePrice,
                COALESCE(price.page_unit_price, 0) AS PageUnitPrice
            FROM catalog_product_sku sku
            LEFT JOIN catalog_spec_value size_value ON size_value.id = sku.size_value_id
            LEFT JOIN catalog_spec_value binding_value ON binding_value.id = sku.binding_value_id
            LEFT JOIN catalog_spec_value layout_value ON layout_value.id = sku.layout_value_id
            LEFT JOIN catalog_price_rule price ON price.id = (
                SELECT pr.id
                FROM catalog_price_rule pr
                WHERE pr.sku_id = sku.id
                  AND pr.is_active = 1
                  AND pr.effective_from <= UTC_TIMESTAMP()
                  AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
                ORDER BY pr.effective_from DESC, pr.id DESC
                LIMIT 1
            )
            WHERE sku.spu_id = @SpuId
              AND sku.is_active = 1
            ORDER BY sku.id;
            """;

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductDetailHeader>(
            new CommandDefinition(detailSql, new { SpuId = spuId }, cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var skuRows = await connection.QueryAsync<SkuRow>(
            new CommandDefinition(skuSql, new { SpuId = spuId }, cancellationToken: cancellationToken));

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
            header.Name,
            header.Subtitle,
            header.ContentSource,
            skus);
    }

    public async Task<PagedResult<BundleListItemResponse>> GetBundlesAsync(
        BundleListFilter filter,
        CancellationToken cancellationToken)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM catalog_bundle
            WHERE is_active = 1;
            """;

        const string listSql = """
            SELECT
                id AS Id,
                bundle_code AS BundleCode,
                name AS Name,
                description AS Description,
                bundle_price AS BundlePrice,
                COALESCE(original_price, bundle_price) AS OriginalPrice,
                sort_order AS SortOrder
            FROM catalog_bundle
            WHERE is_active = 1
            ORDER BY sort_order, id
            LIMIT @PageSize OFFSET @Offset;
            """;

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<BundleRow>(
            new CommandDefinition(
                listSql,
                new
                {
                    filter.PageSize,
                    Offset = (filter.PageNumber - 1) * filter.PageSize
                },
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
            row.Name,
            row.Subtitle,
            row.ContentSource,
            row.StartingPrice,
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
        string Name,
        string? Subtitle,
        string? ContentSource,
        decimal StartingPrice,
        int SortOrder);

    private sealed record ProductDetailHeader(
        long Id,
        long CategoryId,
        string SpuCode,
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

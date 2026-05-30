namespace ThreeBooks.BookBackend.Contracts.Catalogs.Requests;

/// <summary>
/// 商品列表查询参数。
/// </summary>
/// <param name="CategoryId">分类主键标识。</param>
/// <param name="CategorySlug">分类别名。</param>
/// <param name="Keyword">关键字。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListProductsRequest(
    int? CategoryId,
    string? CategorySlug,
    string? Keyword,
    int PageNumber = 1,
    int PageSize = 20);

namespace ThreeBooks.BookBackend.Contracts.Catalogs.Requests;

/// <summary>
/// 分类列表查询参数。
/// </summary>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListCategoriesRequest(int PageNumber = 1, int PageSize = 20);

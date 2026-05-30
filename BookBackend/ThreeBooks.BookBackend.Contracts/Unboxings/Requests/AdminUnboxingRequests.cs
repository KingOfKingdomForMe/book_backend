namespace ThreeBooks.BookBackend.Contracts.Unboxings.Requests;

/// <summary>
/// 后台晒单列表查询参数。
/// </summary>
/// <param name="Keyword">关键字。</param>
/// <param name="LevelCode">晒单等级编码。</param>
/// <param name="IsFeatured">是否精选。</param>
/// <param name="Status">内容状态值。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record AdminListUnboxingsRequest(
    string? Keyword = null,
    string? LevelCode = null,
    bool? IsFeatured = null,
    int? Status = null,
    int PageNumber = 1,
    int PageSize = 20);
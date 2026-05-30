namespace ThreeBooks.BookBackend.Contracts.Unboxings.Requests;

/// <summary>
/// 前台晒单列表查询参数。
/// </summary>
/// <param name="LevelCode">晒单等级编码。</param>
/// <param name="IsFeatured">是否仅查询精选内容。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListUnboxingsRequest(
    string? LevelCode = null,
    bool? IsFeatured = null,
    int PageNumber = 1,
    int PageSize = 20);
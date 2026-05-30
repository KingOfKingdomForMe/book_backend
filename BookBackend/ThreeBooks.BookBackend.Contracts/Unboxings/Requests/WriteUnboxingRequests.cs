namespace ThreeBooks.BookBackend.Contracts.Unboxings.Requests;

/// <summary>
/// 创建晒单请求参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="Title">晒单标题。</param>
/// <param name="ContentText">晒单正文。</param>
/// <param name="PostNo">晒单业务编号，可为空。</param>
/// <param name="AuthorName">作者展示名称。</param>
/// <param name="AuthorAvatarUrl">作者头像地址。</param>
/// <param name="BookTitle">关联书册标题。</param>
/// <param name="ProductLabel">商品标签或名称。</param>
/// <param name="LevelCode">晒单等级编码。</param>
/// <param name="OrderItemId">关联订单项标识。</param>
/// <param name="ProjectVersionId">关联项目版本标识。</param>
/// <param name="IsFeatured">是否精选。</param>
/// <param name="Status">内容状态值。</param>
/// <param name="PublishedAtUtc">发布时间，UTC。</param>
/// <param name="Media">晒单媒体集合。</param>
/// <param name="Tags">晒单标签集合。</param>
public sealed record CreateUnboxingRequest(
    long UserId,
    string Title,
    string ContentText,
    string? PostNo = null,
    string? AuthorName = null,
    string? AuthorAvatarUrl = null,
    string? BookTitle = null,
    string? ProductLabel = null,
    string? LevelCode = null,
    long? OrderItemId = null,
    long? ProjectVersionId = null,
    bool IsFeatured = false,
    int Status = 0,
    DateTime? PublishedAtUtc = null,
    IReadOnlyCollection<WriteUnboxingMediaRequest>? Media = null,
    IReadOnlyCollection<string>? Tags = null);

/// <summary>
/// 更新晒单请求参数。
/// </summary>
/// <param name="Title">晒单标题。</param>
/// <param name="ContentText">晒单正文。</param>
/// <param name="AuthorName">作者展示名称。</param>
/// <param name="AuthorAvatarUrl">作者头像地址。</param>
/// <param name="BookTitle">关联书册标题。</param>
/// <param name="ProductLabel">商品标签或名称。</param>
/// <param name="LevelCode">晒单等级编码。</param>
/// <param name="OrderItemId">关联订单项标识。</param>
/// <param name="ProjectVersionId">关联项目版本标识。</param>
/// <param name="IsFeatured">是否精选。</param>
/// <param name="Status">内容状态值。</param>
/// <param name="PublishedAtUtc">发布时间，UTC。</param>
/// <param name="Media">晒单媒体集合。</param>
/// <param name="Tags">晒单标签集合。</param>
public sealed record UpdateUnboxingRequest(
    string Title,
    string ContentText,
    string? AuthorName = null,
    string? AuthorAvatarUrl = null,
    string? BookTitle = null,
    string? ProductLabel = null,
    string? LevelCode = null,
    long? OrderItemId = null,
    long? ProjectVersionId = null,
    bool IsFeatured = false,
    int Status = 0,
    DateTime? PublishedAtUtc = null,
    IReadOnlyCollection<WriteUnboxingMediaRequest>? Media = null,
    IReadOnlyCollection<string>? Tags = null);

/// <summary>
/// 晒单媒体资源参数。
/// </summary>
/// <param name="MediaType">媒体类型，例如 `image`、`video`。</param>
/// <param name="StorageUrl">媒体存储地址。</param>
/// <param name="ThumbnailUrl">缩略图地址。</param>
/// <param name="SortOrder">排序值。</param>
/// <param name="Width">媒体宽度。</param>
/// <param name="Height">媒体高度。</param>
public sealed record WriteUnboxingMediaRequest(
    string MediaType,
    string StorageUrl,
    string? ThumbnailUrl = null,
    int? SortOrder = null,
    int? Width = null,
    int? Height = null);
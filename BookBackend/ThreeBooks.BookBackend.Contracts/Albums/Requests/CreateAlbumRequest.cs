namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

/// <summary>
/// 创建相册请求参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="ShareCode">相册分享码，可为空，为空时建议由服务端生成。</param>
/// <param name="Title">相册标题。</param>
/// <param name="Subtitle">相册副标题，可为空。</param>
/// <param name="IsPublic">是否公开可访问。</param>
/// <param name="Pages">可选的初始页面集合。传入后会在创建相册时一并保存。</param>
/// <param name="ProductCode">可选的产品编码。传入后且未显式提供 <c>Pages</c> 时，服务端会尝试按该产品绑定的默认相册初始化页面。</param>
public sealed record CreateAlbumRequest(
    long UserId,
    string? ShareCode,
    string Title,
    string? Subtitle,
    bool IsPublic = true,
    IReadOnlyCollection<SaveAlbumPageRequest>? Pages = null,
    string? ProductCode = null);
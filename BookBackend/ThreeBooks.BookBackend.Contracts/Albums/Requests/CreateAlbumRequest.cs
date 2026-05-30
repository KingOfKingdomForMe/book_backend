namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

/// <summary>
/// 创建相册请求参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="ShareCode">相册分享码，可为空，为空时建议由服务端生成。</param>
/// <param name="Title">相册标题。</param>
/// <param name="Subtitle">相册副标题，可为空。</param>
/// <param name="IsPublic">是否公开可访问。</param>
public sealed record CreateAlbumRequest(
    long UserId,
    string? ShareCode,
    string Title,
    string? Subtitle,
    bool IsPublic = true);
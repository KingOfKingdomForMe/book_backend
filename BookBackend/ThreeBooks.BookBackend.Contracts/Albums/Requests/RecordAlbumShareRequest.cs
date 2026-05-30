namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

/// <summary>
/// 记录相册分享行为的请求参数。
/// </summary>
/// <param name="Channel">分享渠道编码，例如 `wechat_session`、`wechat_timeline`。</param>
public sealed record RecordAlbumShareRequest(string Channel);
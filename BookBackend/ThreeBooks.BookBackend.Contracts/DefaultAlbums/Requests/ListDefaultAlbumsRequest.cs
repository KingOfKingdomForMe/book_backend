namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;

/// <summary>
/// 默认相册列表查询参数。
/// </summary>
public sealed record ListDefaultAlbumsRequest(
    string? Keyword = null,
    string? ProductCode = null,
    string? BookType = null,
    string? Category = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20);
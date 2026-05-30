namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

/// <summary>
/// 用户图库列表查询参数。
/// </summary>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListUserGalleryImagesRequest(
    int PageNumber = 1,
    int PageSize = 20);
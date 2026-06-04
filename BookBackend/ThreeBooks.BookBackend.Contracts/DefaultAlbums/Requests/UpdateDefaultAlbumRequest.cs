namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;

/// <summary>
/// 更新默认相册请求参数。
/// </summary>
public sealed record UpdateDefaultAlbumRequest(
    string ProductCode,
    string Name,
    string? Description,
    string? BookType,
    string? Category,
    string? ThemeCode,
    long? PreviewFileId,
    long? CreatedByUserId,
    bool IsActive = true,
    int SortOrder = 0,
    IReadOnlyCollection<DefaultAlbumTemplateAssignmentRequest>? Templates = null,
    IReadOnlyCollection<string>? ExtraProperties = null);
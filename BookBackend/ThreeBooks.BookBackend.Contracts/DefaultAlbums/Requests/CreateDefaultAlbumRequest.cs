namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;

/// <summary>
/// 默认相册模板子项配置。
/// </summary>
public sealed record DefaultAlbumTemplateAssignmentRequest(
    string TemplateCode,
    int SortOrder = 0);

/// <summary>
/// 创建默认相册请求参数。
/// </summary>
public sealed record CreateDefaultAlbumRequest(
    string AlbumCode,
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
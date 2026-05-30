namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;

/// <summary>
/// 更新相册模板请求参数。
/// </summary>
/// <param name="Name">模板名称。</param>
/// <param name="Description">模板描述。</param>
/// <param name="BookType">成册类型编码。</param>
/// <param name="PageType">页面类型编码。</param>
/// <param name="Category">模板分类。</param>
/// <param name="ThemeCode">主题编码。</param>
/// <param name="SchemaVersion">模板结构版本号。</param>
/// <param name="JsonSource">模板布局 JSON 源数据。</param>
/// <param name="PreviewFileId">预览图文件标识。</param>
/// <param name="CreatedByUserId">创建人业务用户标识。</param>
/// <param name="IsBuiltIn">是否为内置模板。</param>
/// <param name="IsActive">是否启用。</param>
/// <param name="SortOrder">排序值。</param>
public sealed record UpdateAlbumTemplateRequest(
    string Name,
    string? Description,
    string? BookType,
    string PageType,
    string? Category,
    string? ThemeCode,
    string? SchemaVersion,
    string JsonSource,
    long? PreviewFileId,
    long? CreatedByUserId,
    bool IsBuiltIn = true,
    bool IsActive = true,
    int SortOrder = 0);
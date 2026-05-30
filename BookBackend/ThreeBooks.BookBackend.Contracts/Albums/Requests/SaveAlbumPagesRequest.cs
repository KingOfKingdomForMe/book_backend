using System.Text.Json;

namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

/// <summary>
/// 保存整本相册页面的请求参数。
/// </summary>
/// <param name="Pages">页面集合，建议按阅读顺序传入。</param>
public sealed record SaveAlbumPagesRequest(
    IReadOnlyCollection<SaveAlbumPageRequest> Pages);

/// <summary>
/// 单个相册页面的保存参数。
/// </summary>
/// <param name="PageNo">页面页码，建议从 1 开始。</param>
/// <param name="JsonSource">页面完整布局 JSON 源数据。</param>
/// <param name="PageLabel">页面标签，如封面、内页、封底。</param>
/// <param name="PageType">页面类型编码。</param>
/// <param name="SortOrder">页面排序值。</param>
/// <param name="HtmlFileId">页面 HTML 产物对应的文件标识。</param>
/// <param name="PageWidth">页面宽度。</param>
/// <param name="PageHeight">页面高度。</param>
/// <param name="Images">页面中使用的图片资源集合。</param>
public sealed record SaveAlbumPageRequest(
    int PageNo,
    string JsonSource,
    string? PageLabel = null,
    string? PageType = null,
    int? SortOrder = null,
    long? HtmlFileId = null,
    int? PageWidth = null,
    int? PageHeight = null,
    IReadOnlyCollection<SaveAlbumPageAssetRequest>? Images = null);

/// <summary>
/// 页面中图片素材的保存参数。
/// </summary>
/// <param name="SortOrder">素材显示顺序。</param>
/// <param name="Role">素材用途角色，例如主图、背景图。</param>
/// <param name="FileId">已上传文件的标识。</param>
/// <param name="Width">素材显示宽度。</param>
/// <param name="Height">素材显示高度。</param>
/// <param name="AltText">图片替代文本。</param>
/// <param name="Caption">图片说明文字。</param>
/// <param name="CropData">裁剪信息 JSON。</param>
public sealed record SaveAlbumPageAssetRequest(
    int SortOrder,
    string Role,
    long FileId,
    int? Width,
    int? Height,
    string? AltText,
    string? Caption,
    JsonElement? CropData = null);
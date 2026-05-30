namespace ThreeBooks.BookBackend.Contracts.CoverTemplates.Requests;

/// <summary>
/// 封面模板列表查询参数。
/// </summary>
/// <param name="Keyword">关键字，通常匹配模板名称或描述。</param>
/// <param name="IsActive">是否仅查询启用模板。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListCoverTemplatesRequest(
    string? Keyword,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// 封面模板字段定义参数。
/// </summary>
/// <param name="FieldId">字段唯一标识。</param>
/// <param name="DisplayName">字段显示名称。</param>
/// <param name="Placeholder">输入提示文本。</param>
/// <param name="X">字段左上角 X 坐标。</param>
/// <param name="Y">字段左上角 Y 坐标。</param>
/// <param name="Width">字段区域宽度。</param>
/// <param name="Height">字段区域高度。</param>
/// <param name="FontFamily">字体名称。</param>
/// <param name="FontSize">字体大小。</param>
/// <param name="FontColor">字体颜色。</param>
/// <param name="IsRequired">是否必填。</param>
/// <param name="SortOrder">字段排序值。</param>
/// <param name="DefaultValue">默认值。</param>
/// <param name="HorizontalAlignment">水平对齐方式。</param>
/// <param name="VerticalAlignment">垂直对齐方式。</param>
/// <param name="MaxLength">最大长度限制。</param>
public sealed record CoverTemplateFieldDefinitionRequest(
    string FieldId,
    string DisplayName,
    string? Placeholder,
    float X,
    float Y,
    float Width,
    float Height,
    string FontFamily,
    float FontSize,
    string FontColor,
    bool IsRequired = false,
    int SortOrder = 0,
    string? DefaultValue = null,
    string HorizontalAlignment = "left",
    string VerticalAlignment = "top",
    int? MaxLength = null);

/// <summary>
/// 创建封面模板请求参数。
/// </summary>
/// <param name="TemplateCode">模板唯一编码。</param>
/// <param name="Name">模板名称。</param>
/// <param name="Description">模板描述。</param>
/// <param name="BackgroundFileId">背景图文件标识。</param>
/// <param name="Fields">封面字段定义集合。</param>
/// <param name="CreatedByUserId">创建人业务用户标识。</param>
/// <param name="IsBuiltIn">是否为内置模板。</param>
/// <param name="IsActive">是否启用。</param>
/// <param name="SortOrder">排序值。</param>
public sealed record CreateCoverTemplateRequest(
    string TemplateCode,
    string Name,
    string? Description,
    long BackgroundFileId,
    IReadOnlyCollection<CoverTemplateFieldDefinitionRequest> Fields,
    long? CreatedByUserId = null,
    bool IsBuiltIn = false,
    bool IsActive = true,
    int SortOrder = 0);

/// <summary>
/// 更新封面模板请求参数。
/// </summary>
/// <param name="Name">模板名称。</param>
/// <param name="Description">模板描述。</param>
/// <param name="BackgroundFileId">背景图文件标识。</param>
/// <param name="Fields">封面字段定义集合。</param>
/// <param name="CreatedByUserId">创建人业务用户标识。</param>
/// <param name="IsBuiltIn">是否为内置模板。</param>
/// <param name="IsActive">是否启用。</param>
/// <param name="SortOrder">排序值。</param>
public sealed record UpdateCoverTemplateRequest(
    string Name,
    string? Description,
    long BackgroundFileId,
    IReadOnlyCollection<CoverTemplateFieldDefinitionRequest> Fields,
    long? CreatedByUserId = null,
    bool IsBuiltIn = false,
    bool IsActive = true,
    int SortOrder = 0);

/// <summary>
/// 封面生成时单个字段的取值参数。
/// </summary>
/// <param name="FieldId">字段标识，对应模板字段定义中的 `FieldId`。</param>
/// <param name="Value">字段值。</param>
public sealed record CoverTemplateFieldValueRequest(
    string FieldId,
    string? Value);

/// <summary>
/// 生成封面图请求参数。
/// </summary>
/// <param name="FieldValues">封面字段值集合。</param>
/// <param name="Bucket">结果文件存储桶，可为空。</param>
/// <param name="Directory">结果文件目录，可为空。</param>
/// <param name="FileName">结果文件名称，可为空。</param>
public sealed record GenerateCoverImageRequest(
    IReadOnlyCollection<CoverTemplateFieldValueRequest> FieldValues,
    string? Bucket = null,
    string? Directory = null,
    string? FileName = null);
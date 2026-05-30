namespace ThreeBooks.BookBackend.Contracts.CoverTemplates.Responses;

public sealed record CoverTemplateFieldResponse(
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
    bool IsRequired,
    int SortOrder,
    string? DefaultValue,
    string HorizontalAlignment,
    string VerticalAlignment,
    int? MaxLength);

public sealed record CoverTemplateListItemResponse(
    long TemplateId,
    string TemplateCode,
    string Name,
    string? Description,
    string? BackgroundUrl,
    bool IsBuiltIn,
    bool IsActive,
    int SortOrder,
    DateTime UpdatedAtUtc);

public sealed record CoverTemplateDetailResponse(
    long TemplateId,
    string TemplateCode,
    string Name,
    string? Description,
    long BackgroundFileId,
    string? BackgroundUrl,
    IReadOnlyCollection<CoverTemplateFieldResponse> Fields,
    long? CreatedByUserId,
    bool IsBuiltIn,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateCoverTemplateResponse(
    long TemplateId,
    string TemplateCode,
    bool IsActive,
    string? BackgroundUrl);

public sealed record GeneratedCoverImageResponse(
    long FileId,
    string Bucket,
    string ObjectKey,
    string ProxyUrl);
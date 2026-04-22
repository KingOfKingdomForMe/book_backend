namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;

public sealed record AlbumTemplateDetailResponse(
    long TemplateId,
    string TemplateCode,
    string Name,
    string? Description,
    string? BookType,
    string PageType,
    string? Category,
    string? ThemeCode,
    string SchemaVersion,
    string JsonSource,
    string? PreviewUrl,
    long? PreviewFileId,
    long? CreatedByUserId,
    bool IsBuiltIn,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
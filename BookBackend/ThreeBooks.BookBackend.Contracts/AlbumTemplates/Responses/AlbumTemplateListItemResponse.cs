namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;

public sealed record AlbumTemplateListItemResponse(
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
    bool IsBuiltIn,
    bool IsActive,
    int SortOrder,
    DateTime UpdatedAtUtc);
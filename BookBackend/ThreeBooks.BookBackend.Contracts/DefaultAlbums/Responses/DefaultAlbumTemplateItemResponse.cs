namespace ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

public sealed record DefaultAlbumTemplateItemResponse(
    long ItemId,
    long TemplateId,
    string TemplateCode,
    string Name,
    string? Description,
    string PageType,
    string? Category,
    string? ThemeCode,
    string SchemaVersion,
    string JsonSource,
    string? PreviewUrl,
    int SortOrder);
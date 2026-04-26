namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;

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
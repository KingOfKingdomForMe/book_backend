namespace ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;

public sealed record DefaultAlbumListFilter(
    string? Keyword,
    string? ProductCode,
    string? BookType,
    string? Category,
    bool? IsActive,
    int PageNumber,
    int PageSize);

public sealed record DefaultAlbumStoredFileReference(string Bucket, string ObjectKey);

public sealed record DefaultAlbumTemplateAssignmentModel(string TemplateCode, int SortOrder);

public sealed record DefaultAlbumListItemQueryModel(
    long AlbumId,
    string AlbumCode,
    string ProductCode,
    string Name,
    string? Description,
    string? BookType,
    string? Category,
    string? ThemeCode,
    IReadOnlyCollection<string> ExtraProperties,
    DefaultAlbumStoredFileReference? PreviewFile,
    int TemplateCount,
    bool IsActive,
    int SortOrder,
    DateTime UpdatedAtUtc);

public sealed record DefaultAlbumTemplateItemQueryModel(
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
    DefaultAlbumStoredFileReference? PreviewFile,
    int SortOrder);

public sealed record DefaultAlbumDetailQueryModel(
    long AlbumId,
    string AlbumCode,
    string ProductCode,
    string Name,
    string? Description,
    string? BookType,
    string? Category,
    string? ThemeCode,
    IReadOnlyCollection<string> ExtraProperties,
    long? PreviewFileId,
    DefaultAlbumStoredFileReference? PreviewFile,
    long? CreatedByUserId,
    bool IsActive,
    int SortOrder,
    IReadOnlyCollection<DefaultAlbumTemplateItemQueryModel> Templates,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record DefaultAlbumCreateCommandModel(
    string AlbumCode,
    string ProductCode,
    string Name,
    string? Description,
    string? BookType,
    string? Category,
    string? ThemeCode,
    IReadOnlyCollection<string> ExtraProperties,
    long? PreviewFileId,
    long? CreatedByUserId,
    bool IsActive,
    int SortOrder,
    IReadOnlyCollection<DefaultAlbumTemplateAssignmentModel> Templates);

public sealed record DefaultAlbumUpdateCommandModel(
    string ProductCode,
    string Name,
    string? Description,
    string? BookType,
    string? Category,
    string? ThemeCode,
    IReadOnlyCollection<string> ExtraProperties,
    long? PreviewFileId,
    long? CreatedByUserId,
    bool IsActive,
    int SortOrder,
    IReadOnlyCollection<DefaultAlbumTemplateAssignmentModel> Templates);

public sealed record DefaultAlbumCreateResultModel(
    long AlbumId,
    string AlbumCode,
    bool IsActive,
    DefaultAlbumStoredFileReference? PreviewFile);
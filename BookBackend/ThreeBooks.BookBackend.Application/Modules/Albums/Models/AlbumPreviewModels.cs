namespace ThreeBooks.BookBackend.Application.Modules.Albums.Models;

public sealed record AlbumStoredFileReference(
    long FileId,
    string Bucket,
    string ObjectKey);

public sealed record AlbumPreviewQueryModel(
    long ProjectId,
    string ShareCode,
    string Title,
    string? Subtitle,
    string BookType,
    string? ProductCode,
    int PageCount,
    long ViewCount,
    long ShareCount,
    long SharedVersionId,
    IReadOnlyCollection<AlbumPreviewPageSummaryModel> Pages);

public sealed record AlbumPreviewPageSummaryModel(
    int PageNo,
    string PageLabel,
    string PageType,
    string? JsonSource,
    AlbumStoredFileReference? JsonFile,
    AlbumStoredFileReference? HtmlFile,
    AlbumStoredFileReference? ThumbnailFile,
    bool HasImages);

public sealed record AlbumPreviewPageQueryModel(
    int PageNo,
    string PageLabel,
    string PageType,
    string SchemaVersion,
    string? JsonSource,
    AlbumStoredFileReference? JsonFile,
    AlbumStoredFileReference? HtmlFile,
    IReadOnlyCollection<AlbumPreviewPageAssetModel> Images);

public sealed record AlbumPreviewPageAssetModel(
    int SortOrder,
    string Role,
    AlbumStoredFileReference File,
    int? Width,
    int? Height,
    string? AltText,
    string? Caption);

public sealed record AlbumCreateCommandModel(
    long UserId,
    string ShareCode,
    string Title,
    string? Subtitle,
    string BookType,
    string ProductCode,
    int Status,
    bool IsPublic,
    int VersionNo,
    string SnapshotSchemaVersion,
    string? RenderVersion);

public sealed record AlbumCreateResultModel(
    long ProjectId,
    long VersionId,
    string ShareCode,
    string Title,
    string? Subtitle,
    string BookType,
    string ProductCode,
    bool IsPublic,
    int PageCount,
    int ImageCount);

public sealed record AlbumPageAssetWriteModel(
    int SortOrder,
    string Role,
    long FileId,
    int? Width,
    int? Height,
    string? AltText,
    string? Caption,
    string? CropJson);

public sealed record AlbumPageWriteCommandModel(
    int PageNo,
    string PageLabel,
    string PageType,
    int SortOrder,
    string JsonSource,
    long? HtmlFileId,
    long? ThumbnailFileId,
    int? PageWidth,
    int? PageHeight,
    string SchemaVersion,
    IReadOnlyCollection<AlbumPageAssetWriteModel> Images);

public sealed record AlbumPagesWriteCommandModel(
    IReadOnlyCollection<AlbumPageWriteCommandModel> Pages,
    string SnapshotSchemaVersion);

public sealed record AlbumPagesWriteResultModel(
    long ProjectId,
    long VersionId,
    string ShareCode,
    bool IsPublic,
    int PageCount,
    int ImageCount,
    string SchemaVersion);
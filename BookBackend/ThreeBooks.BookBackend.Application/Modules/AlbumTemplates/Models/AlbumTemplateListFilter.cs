namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;

public sealed record AlbumTemplateListFilter(
	string? Keyword,
	string? BookType,
	string? PageType,
	string? Category,
	bool? IsActive,
	int PageNumber,
	int PageSize);

public sealed record AlbumTemplateCreateCommandModel(
	string TemplateCode,
	string Name,
	string? Description,
	string? BookType,
	string PageType,
	string? Category,
	string? ThemeCode,
	string SchemaVersion,
	string JsonSource,
	long? PreviewFileId,
	long? CreatedByUserId,
	bool IsBuiltIn,
	bool IsActive,
	int SortOrder);

public sealed record AlbumTemplateUpdateCommandModel(
	string Name,
	string? Description,
	string? BookType,
	string PageType,
	string? Category,
	string? ThemeCode,
	string SchemaVersion,
	string JsonSource,
	long? PreviewFileId,
	long? CreatedByUserId,
	bool IsBuiltIn,
	bool IsActive,
	int SortOrder);

public sealed record AlbumTemplateCreateResultModel(
	long TemplateId,
	string TemplateCode,
	bool IsActive,
	AlbumTemplateStoredFileReference? PreviewFile);

public sealed record AlbumTemplateListItemQueryModel(
	long TemplateId,
	string TemplateCode,
	string Name,
	string? Description,
	string? BookType,
	string PageType,
	string? Category,
	string? ThemeCode,
	string SchemaVersion,
	AlbumTemplateStoredFileReference? PreviewFile,
	bool IsBuiltIn,
	bool IsActive,
	int SortOrder,
	DateTime UpdatedAtUtc);

public sealed record AlbumTemplateDetailQueryModel(
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
	long? PreviewFileId,
	AlbumTemplateStoredFileReference? PreviewFile,
	long? CreatedByUserId,
	bool IsBuiltIn,
	bool IsActive,
	int SortOrder,
	DateTime CreatedAtUtc,
	DateTime UpdatedAtUtc);

public sealed record AlbumTemplateStoredFileReference(
	string Bucket,
	string ObjectKey);
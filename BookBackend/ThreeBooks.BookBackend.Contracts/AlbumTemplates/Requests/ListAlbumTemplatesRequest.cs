namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;

public sealed record ListAlbumTemplatesRequest(
	string? Keyword,
	string? BookType = null,
	string? PageType = null,
	string? Category = null,
	bool? IsActive = true,
	int PageNumber = 1,
	int PageSize = 20);
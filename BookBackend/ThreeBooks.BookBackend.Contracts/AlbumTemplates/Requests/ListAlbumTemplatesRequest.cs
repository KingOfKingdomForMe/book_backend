namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;

public sealed record ListAlbumTemplatesRequest(string? Keyword, int PageNumber = 1, int PageSize = 20);
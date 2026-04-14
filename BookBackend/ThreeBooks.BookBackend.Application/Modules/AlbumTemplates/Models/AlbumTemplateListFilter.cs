namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;

public sealed record AlbumTemplateListFilter(string? Keyword, int PageNumber, int PageSize);
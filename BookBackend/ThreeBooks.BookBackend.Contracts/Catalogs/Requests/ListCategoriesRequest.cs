namespace ThreeBooks.BookBackend.Contracts.Catalogs.Requests;

public sealed record ListCategoriesRequest(int PageNumber = 1, int PageSize = 20);

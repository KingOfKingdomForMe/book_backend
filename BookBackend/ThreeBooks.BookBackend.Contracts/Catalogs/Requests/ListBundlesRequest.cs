namespace ThreeBooks.BookBackend.Contracts.Catalogs.Requests;

public sealed record ListBundlesRequest(int PageNumber = 1, int PageSize = 20);

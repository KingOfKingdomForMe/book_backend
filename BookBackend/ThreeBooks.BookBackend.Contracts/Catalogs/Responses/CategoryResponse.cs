namespace ThreeBooks.BookBackend.Contracts.Catalogs.Responses;

public sealed record CategoryResponse(
	int Id,
	string Name,
	string Slug,
	int SortOrder,
	int ProductCount,
	IReadOnlyCollection<ProductListItemResponse> Products);

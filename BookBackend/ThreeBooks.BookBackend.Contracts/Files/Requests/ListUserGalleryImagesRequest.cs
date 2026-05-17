namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

public sealed record ListUserGalleryImagesRequest(
    int PageNumber = 1,
    int PageSize = 20);
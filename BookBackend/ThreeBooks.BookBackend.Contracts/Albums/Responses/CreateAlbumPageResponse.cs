namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record CreateAlbumPageResponse(
    long ProjectId,
    long VersionId,
    long VersionPageId,
    int PageNo,
    string? PreviewPageUrl);
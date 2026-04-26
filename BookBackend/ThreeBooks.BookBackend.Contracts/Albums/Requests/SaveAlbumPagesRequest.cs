using System.Text.Json;

namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

public sealed record SaveAlbumPagesRequest(
    IReadOnlyCollection<SaveAlbumPageRequest> Pages);

public sealed record SaveAlbumPageRequest(
    int PageNo,
    string JsonSource,
    string? PageLabel = null,
    string? PageType = null,
    int? SortOrder = null,
    long? HtmlFileId = null,
    int? PageWidth = null,
    int? PageHeight = null,
    IReadOnlyCollection<SaveAlbumPageAssetRequest>? Images = null);

public sealed record SaveAlbumPageAssetRequest(
    int SortOrder,
    string Role,
    long FileId,
    int? Width,
    int? Height,
    string? AltText,
    string? Caption,
    JsonElement? CropData = null);
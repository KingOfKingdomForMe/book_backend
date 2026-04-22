using System.Text.Json;

namespace ThreeBooks.BookBackend.Contracts.Albums.Requests;

public sealed record CreateAlbumPageRequest(
    int PageNo,
    string PageLabel,
    string PageType,
    string JsonSource,
    long? HtmlFileId = null,
    IReadOnlyCollection<CreateAlbumPageAssetRequest>? Images = null);

public sealed record CreateAlbumPageAssetRequest(
    int SortOrder,
    string Role,
    long FileId,
    int? Width,
    int? Height,
    string? AltText,
    string? Caption,
    JsonElement? CropData = null);
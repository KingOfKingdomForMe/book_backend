using System.Text.Json;

namespace ThreeBooks.BookBackend.Contracts.Albums.Responses;

public sealed record AlbumPreviewPageResponse(
    int PageNo,
    string PageLabel,
    string PageType,
    string SchemaVersion,
    string JsonSource,
    string? HtmlProxyUrl,
    JsonElement? PageData,
    IReadOnlyCollection<AlbumPreviewPageImageResponse> Images);

public sealed record AlbumPreviewPageImageResponse(
    int SortOrder,
    string Role,
    string Url,
    int? Width,
    int? Height,
    string? AltText,
    string? Caption);
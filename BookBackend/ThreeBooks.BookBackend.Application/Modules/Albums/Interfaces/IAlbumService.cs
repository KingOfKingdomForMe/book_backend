using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Albums.Requests;
using ThreeBooks.BookBackend.Contracts.Albums.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;

public interface IAlbumService
{
    Task<CreateAlbumResponse> CreateAlbumAsync(
        CreateAlbumRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<SaveAlbumPagesResponse?> SavePagesAsync(
        string shareCode,
        SaveAlbumPagesRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<AlbumPreviewDetailResponse?> GetPreviewAsync(
        string shareCode,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<AlbumPreviewPageResponse?> GetPageAsync(
        string shareCode,
        int pageNumber,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<AlbumPreviewShareInfoResponse?> GetShareInfoAsync(
        string shareCode,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<bool> RecordViewAsync(
        string shareCode,
        int? pageNumber,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<bool> RecordShareAsync(
        string shareCode,
        string channel,
        RequestContext context,
        CancellationToken cancellationToken);
}
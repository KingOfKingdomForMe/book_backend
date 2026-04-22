using ThreeBooks.BookBackend.Application.Modules.Albums.Models;

namespace ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;

public interface IAlbumQueryStore
{
    Task<bool> ShareCodeExistsAsync(
        string shareCode,
        CancellationToken cancellationToken);

    Task<AlbumCreateResultModel> CreateAlbumAsync(
        AlbumCreateCommandModel command,
        CancellationToken cancellationToken);

    Task<AlbumPageWriteResultModel?> AddPageAsync(
        long projectId,
        AlbumPageWriteCommandModel command,
        CancellationToken cancellationToken);

    Task<AlbumPreviewQueryModel?> GetPreviewAsync(
        string shareCode,
        CancellationToken cancellationToken);

    Task<AlbumPreviewPageQueryModel?> GetPageAsync(
        string shareCode,
        int pageNumber,
        CancellationToken cancellationToken);

    Task<bool> RecordViewAsync(
        string shareCode,
        int? pageNumber,
        string? clientIp,
        string? clientUserAgent,
        CancellationToken cancellationToken);

    Task<bool> RecordShareAsync(
        string shareCode,
        string channel,
        string? clientIp,
        string? clientUserAgent,
        CancellationToken cancellationToken);
}
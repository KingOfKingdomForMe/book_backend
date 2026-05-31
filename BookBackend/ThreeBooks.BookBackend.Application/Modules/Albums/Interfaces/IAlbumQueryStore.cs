using ThreeBooks.BookBackend.Application.Modules.Albums.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;

public interface IAlbumQueryStore
{
    Task<PagedResult<AlbumListItemQueryModel>> GetListAsync(
        AlbumListFilter filter,
        CancellationToken cancellationToken);

    Task<bool> ShareCodeExistsAsync(
        string shareCode,
        CancellationToken cancellationToken);

    Task<AlbumCreateResultModel> CreateAlbumAsync(
        AlbumCreateCommandModel command,
        AlbumPagesWriteCommandModel? pagesCommand,
        CancellationToken cancellationToken);

    Task<AlbumPagesWriteResultModel?> SavePagesAsync(
        string shareCode,
        AlbumPagesWriteCommandModel command,
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
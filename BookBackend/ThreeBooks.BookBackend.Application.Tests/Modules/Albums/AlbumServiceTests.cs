using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Albums.Models;
using ThreeBooks.BookBackend.Application.Modules.Albums.Services;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Albums.Requests;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;
using Xunit;

namespace ThreeBooks.BookBackend.Application.Tests.Modules.Albums;

public sealed class AlbumServiceTests
{
    private static readonly RequestContext Context = new(null, null);

    [Fact]
    public async Task GetListAsync_ReturnsUserAlbums()
    {
        const long userId = 1024;
        var createdAtUtc = new DateTime(2026, 5, 20, 8, 30, 0, DateTimeKind.Utc);
        var updatedAtUtc = new DateTime(2026, 5, 30, 9, 45, 0, DateTimeKind.Utc);
        var queryStore = new FakeAlbumQueryStore
        {
            GetListHandler = filter => Task.FromResult(new PagedResult<AlbumListItemQueryModel>(
            [
                new AlbumListItemQueryModel(
                    11,
                    "story-album-001",
                    "我们的纪念",
                    "把时间折成书页",
                    "balbum",
                    null,
                    true,
                    24,
                    32,
                    120,
                    8,
                    createdAtUtc,
                    updatedAtUtc)
            ],
            filter.PageNumber,
            filter.PageSize,
            1))
        };

        var service = new AlbumService(queryStore, new FakeFileStorageService());

        var response = await service.GetListAsync(new ListAlbumsRequest(userId), Context, CancellationToken.None);

        var item = Assert.Single(response.Items);
        Assert.Equal(11, item.ProjectId);
        Assert.Equal("story-album-001", item.ShareCode);
        Assert.Equal("balbum", item.ProductCode);
        Assert.Equal("/albums/story-album-001", item.PreviewUrl);
        Assert.Equal(updatedAtUtc, item.UpdatedAtUtc);
    }

    private sealed class FakeAlbumQueryStore : IAlbumQueryStore
    {
        public Func<AlbumListFilter, Task<PagedResult<AlbumListItemQueryModel>>>? GetListHandler { get; init; }

        public Task<PagedResult<AlbumListItemQueryModel>> GetListAsync(
            AlbumListFilter filter,
            CancellationToken cancellationToken)
        {
            return GetListHandler is null
                ? Task.FromResult(new PagedResult<AlbumListItemQueryModel>([], filter.PageNumber, filter.PageSize, 0))
                : GetListHandler(filter);
        }

        public Task<bool> ShareCodeExistsAsync(string shareCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AlbumCreateResultModel> CreateAlbumAsync(AlbumCreateCommandModel command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AlbumPagesWriteResultModel?> SavePagesAsync(string shareCode, AlbumPagesWriteCommandModel command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AlbumPreviewQueryModel?> GetPreviewAsync(string shareCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AlbumPreviewPageQueryModel?> GetPageAsync(string shareCode, int pageNumber, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RecordViewAsync(string shareCode, int? pageNumber, string? clientIp, string? clientUserAgent, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RecordShareAsync(string shareCode, string channel, string? clientIp, string? clientUserAgent, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<UploadFileResponse> UploadAsync(
            UploadFileRequest request,
            Stream content,
            string originalFileName,
            string? contentType,
            long contentLength,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<FileAccessUrlResponse?> GetAccessUrlAsync(
            GetFileAccessUrlRequest request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StoredFileContent?> DownloadAsync(
            string? bucket,
            string objectKey,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(
            string? bucket,
            string objectKey,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
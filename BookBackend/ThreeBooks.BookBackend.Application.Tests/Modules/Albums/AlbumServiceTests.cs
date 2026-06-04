using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Albums.Models;
using ThreeBooks.BookBackend.Application.Modules.Albums.Services;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;
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
                        ["travel-memory", "story-starter"],
                    120,
                    8,
                    createdAtUtc,
                    updatedAtUtc)
            ],
            filter.PageNumber,
            filter.PageSize,
            1))
        };

    var service = new AlbumService(queryStore, new FakeFileStorageService(), new FakeDefaultAlbumQueryStore());

        var response = await service.GetListAsync(new ListAlbumsRequest(userId), Context, CancellationToken.None);

        var item = Assert.Single(response.Items);
        Assert.Equal(11, item.ProjectId);
        Assert.Equal("story-album-001", item.ShareCode);
        Assert.Equal("balbum", item.ProductCode);
        Assert.Equal("/albums/story-album-001", item.PreviewUrl);
        Assert.Equal(32, item.UploadedImageCount);
        Assert.Equal(["travel-memory", "story-starter"], item.ExtraProperties);
        Assert.Equal(updatedAtUtc, item.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetPreviewAsync_ReturnsUploadedImageCountAndExtraProperties()
    {
        var queryStore = new FakeAlbumQueryStore
        {
            GetPreviewHandler = shareCode => Task.FromResult<AlbumPreviewQueryModel?>(
                new AlbumPreviewQueryModel(
                    11,
                    shareCode,
                    "我们的纪念",
                    "把时间折成书页",
                    "balbum",
                    "balbum",
                    24,
                    32,
                    120,
                    8,
                    ["travel-memory", "story-starter"],
                    1001,
                    [
                        new AlbumPreviewPageSummaryModel(
                            1,
                            "封面",
                            "cover",
                            "{\"schemaVersion\":\"2.0\",\"blocks\":[]}",
                            null,
                            null,
                            null,
                            true)
                    ]))
        };

        var service = new AlbumService(queryStore, new FakeFileStorageService(), new FakeDefaultAlbumQueryStore());

        var response = await service.GetPreviewAsync("story-album-001", Context, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(32, response!.UploadedImageCount);
        Assert.Equal(["travel-memory", "story-starter"], response.ExtraProperties);
    }

    [Fact]
    public async Task CreateAlbumAsync_PassesInlinePagesToCreateFlow()
    {
        AlbumCreateCommandModel? capturedCommand = null;
        AlbumPagesWriteCommandModel? capturedPages = null;
        var queryStore = new FakeAlbumQueryStore
        {
            CreateAlbumHandler = (command, pagesCommand) =>
            {
                capturedCommand = command;
                capturedPages = pagesCommand;

                return Task.FromResult(new AlbumCreateResultModel(
                    9010,
                    901001,
                    command.ShareCode,
                    command.Title,
                    command.Subtitle,
                    command.BookType,
                    command.ProductCode,
                    command.IsPublic,
                    pagesCommand?.Pages.Count ?? 0,
                    0));
            }
        };

        var service = new AlbumService(queryStore, new FakeFileStorageService(), new FakeDefaultAlbumQueryStore());

        var response = await service.CreateAlbumAsync(
            new CreateAlbumRequest(
                9001,
                "family-magazine-001",
                "亲子时光杂志册",
                "亲子时光杂志册啊啊啊啊",
                true,
                [
                    new SaveAlbumPageRequest(
                        1,
                        "{\"schemaVersion\":\"2.0\",\"pageType\":\"cover\",\"blocks\":[]}",
                        PageLabel: "封面",
                        PageType: "cover",
                        SortOrder: 1,
                        PageWidth: 1200,
                        PageHeight: 1800)
                ]),
            Context,
            CancellationToken.None);

        Assert.NotNull(capturedCommand);
        Assert.Equal("family-magazine-001", capturedCommand!.ShareCode);
        Assert.Equal("balbum", capturedCommand.ProductCode);

        var page = Assert.Single(capturedPages!.Pages);
        Assert.Equal(1, page.PageNo);
        Assert.Equal("cover", page.PageType);
        Assert.Equal("封面", page.PageLabel);
        Assert.Equal("2.0", capturedPages.SnapshotSchemaVersion);
        Assert.Equal("/albums/family-magazine-001", response.PreviewUrl);
    }

    [Fact]
    public async Task CreateAlbumAsync_UsesProductDefaultAlbumTemplatesWhenPagesMissing()
    {
        AlbumCreateCommandModel? capturedCommand = null;
        AlbumPagesWriteCommandModel? capturedPages = null;
        var queryStore = new FakeAlbumQueryStore
        {
            CreateAlbumHandler = (command, pagesCommand) =>
            {
                capturedCommand = command;
                capturedPages = pagesCommand;

                return Task.FromResult(new AlbumCreateResultModel(
                    9010,
                    901001,
                    command.ShareCode,
                    command.Title,
                    command.Subtitle,
                    command.BookType,
                    command.ProductCode,
                    command.IsPublic,
                    pagesCommand?.Pages.Count ?? 0,
                    0));
            }
        };

        var defaultAlbumQueryStore = new FakeDefaultAlbumQueryStore
        {
            GetActiveByProductCodeHandler = productCode => Task.FromResult<DefaultAlbumDetailQueryModel?>(
                new DefaultAlbumDetailQueryModel(
                    10,
                    "default-xcalbum",
                    productCode,
                    "轻奢杂志册默认相册",
                    "按产品初始化的默认相册",
                    productCode,
                    "product-default",
                    "default-xcalbum",
                    ["product-default", "starter-layout"],
                    null,
                    null,
                    null,
                    true,
                    10,
                    [
                        new DefaultAlbumTemplateItemQueryModel(
                            100,
                            1000,
                            "default-product-cover",
                            "默认封面",
                            null,
                            "cover",
                            "product-default",
                            "default-xcalbum",
                            "2.0",
                            "{\"schemaVersion\":\"2.0\",\"pageType\":\"cover\",\"blocks\":[]}",
                            null,
                            10),
                        new DefaultAlbumTemplateItemQueryModel(
                            101,
                            1001,
                            "default-product-story",
                            "默认内页",
                            null,
                            "content",
                            "product-default",
                            "default-xcalbum",
                            "2.0",
                            "{\"schemaVersion\":\"2.0\",\"pageType\":\"content\",\"blocks\":[]}",
                            null,
                            20)
                    ],
                    new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc)))
        };

        var service = new AlbumService(queryStore, new FakeFileStorageService(), defaultAlbumQueryStore);

        await service.CreateAlbumAsync(
            new CreateAlbumRequest(
                9001,
                "product-initial-001",
                "轻奢杂志册初始项目",
                "按默认相册初始化",
                ProductCode: "xcalbum"),
            Context,
            CancellationToken.None);

        Assert.NotNull(capturedCommand);
        Assert.Equal("xcalbum", capturedCommand!.BookType);
        Assert.Equal("xcalbum", capturedCommand.ProductCode);
        Assert.Equal(["product-default", "starter-layout"], capturedCommand.ExtraProperties);

        Assert.NotNull(capturedPages);
        Assert.Equal(2, capturedPages!.Pages.Count);
        Assert.Equal("默认封面", capturedPages.Pages.First().PageLabel);
        Assert.Equal("cover", capturedPages.Pages.First().PageType);
        Assert.Equal("2.0", capturedPages.SnapshotSchemaVersion);
    }

    private sealed class FakeAlbumQueryStore : IAlbumQueryStore
    {
        public Func<AlbumListFilter, Task<PagedResult<AlbumListItemQueryModel>>>? GetListHandler { get; init; }

        public Func<AlbumCreateCommandModel, AlbumPagesWriteCommandModel?, Task<AlbumCreateResultModel>>? CreateAlbumHandler { get; init; }

        public Func<string, Task<AlbumPreviewQueryModel?>>? GetPreviewHandler { get; init; }

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

        public Task<AlbumCreateResultModel> CreateAlbumAsync(
            AlbumCreateCommandModel command,
            AlbumPagesWriteCommandModel? pagesCommand,
            CancellationToken cancellationToken)
        {
            return CreateAlbumHandler is null
                ? throw new NotSupportedException()
                : CreateAlbumHandler(command, pagesCommand);
        }

        public Task<AlbumPagesWriteResultModel?> SavePagesAsync(string shareCode, AlbumPagesWriteCommandModel command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AlbumPreviewQueryModel?> GetPreviewAsync(string shareCode, CancellationToken cancellationToken)
        {
            return GetPreviewHandler is null
                ? throw new NotSupportedException()
                : GetPreviewHandler(shareCode);
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

    private sealed class FakeDefaultAlbumQueryStore : IDefaultAlbumQueryStore
    {
        public Func<string, Task<DefaultAlbumDetailQueryModel?>>? GetActiveByProductCodeHandler { get; init; }

        public Task<PagedResult<DefaultAlbumListItemQueryModel>> GetListAsync(
            DefaultAlbumListFilter filter,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DefaultAlbumDetailQueryModel?> GetDetailAsync(string albumCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DefaultAlbumDetailQueryModel?> GetActiveByProductCodeAsync(string productCode, CancellationToken cancellationToken)
        {
            return GetActiveByProductCodeHandler is null
                ? Task.FromResult<DefaultAlbumDetailQueryModel?>(null)
                : GetActiveByProductCodeHandler(productCode);
        }

        public Task<DefaultAlbumCreateResultModel> CreateAsync(DefaultAlbumCreateCommandModel command, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DefaultAlbumDetailQueryModel?> UpdateAsync(
            string albumCode,
            DefaultAlbumUpdateCommandModel command,
            CancellationToken cancellationToken)
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
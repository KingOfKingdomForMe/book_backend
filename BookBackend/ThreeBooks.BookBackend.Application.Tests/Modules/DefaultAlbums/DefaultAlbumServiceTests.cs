using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Services;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;
using Xunit;

namespace ThreeBooks.BookBackend.Application.Tests.Modules.DefaultAlbums;

public sealed class DefaultAlbumServiceTests
{
    private static readonly RequestContext Context = new(null, null);

    [Fact]
    public async Task GetDetailAsync_ReturnsTemplates()
    {
        var queryStore = new FakeDefaultAlbumQueryStore
        {
            GetDetailHandler = albumCode => Task.FromResult<DefaultAlbumDetailQueryModel?>(
                new DefaultAlbumDetailQueryModel(
                    18,
                    albumCode,
                    "balbum",
                    "成长纪念册",
                    "适合新用户直接套用的默认相册",
                    "balbum",
                    "story",
                    "spring",
                    ["starter-layout", "growth-memory"],
                    91,
                    new DefaultAlbumStoredFileReference("preview", "default-albums/growth.jpg"),
                    9001,
                    true,
                    3,
                    [
                        new DefaultAlbumTemplateItemQueryModel(
                            101,
                            501,
                            "growth-cover",
                            "成长封面",
                            "封面模板",
                            "cover-template",
                            "story",
                            "spring",
                            "2.1",
                            "{\"blocks\":[]}",
                            new DefaultAlbumStoredFileReference("preview", "templates/growth-cover.jpg"),
                            1)
                    ],
                    new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 6, 2, 9, 30, 0, DateTimeKind.Utc)))
        };

        var service = new DefaultAlbumService(queryStore);

        var response = await service.GetDetailAsync("growth-default", Context, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("growth-default", response!.AlbumCode);
        Assert.Equal("balbum", response.ProductCode);
        Assert.Equal(1, response.TemplateCount);
        Assert.Equal(["starter-layout", "growth-memory"], response.ExtraProperties);
        Assert.Equal("/api/files/content/preview/default-albums/growth.jpg", response.PreviewUrl);

        var template = Assert.Single(response.Templates);
        Assert.Equal("growth-cover", template.TemplateCode);
        Assert.Equal("{\"blocks\":[]}", template.JsonSource);
        Assert.Equal("/api/files/content/preview/templates/growth-cover.jpg", template.PreviewUrl);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenTemplatesMissing()
    {
        var service = new DefaultAlbumService(new FakeDefaultAlbumQueryStore());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new CreateDefaultAlbumRequest(
                "growth-default",
                "balbum",
                "成长纪念册",
                null,
                "balbum",
                "story",
                "spring",
                null,
                null,
                Templates: []),
            Context,
            CancellationToken.None));

        Assert.Equal("Templates", exception.ParamName);
    }

    private sealed class FakeDefaultAlbumQueryStore : IDefaultAlbumQueryStore
    {
        public Func<DefaultAlbumListFilter, Task<PagedResult<DefaultAlbumListItemQueryModel>>>? GetListHandler { get; init; }

        public Func<string, Task<DefaultAlbumDetailQueryModel?>>? GetDetailHandler { get; init; }

        public Func<DefaultAlbumCreateCommandModel, Task<DefaultAlbumCreateResultModel>>? CreateHandler { get; init; }

        public Func<string, DefaultAlbumUpdateCommandModel, Task<DefaultAlbumDetailQueryModel?>>? UpdateHandler { get; init; }

        public Task<PagedResult<DefaultAlbumListItemQueryModel>> GetListAsync(
            DefaultAlbumListFilter filter,
            CancellationToken cancellationToken)
        {
            return GetListHandler is null
                ? Task.FromResult(new PagedResult<DefaultAlbumListItemQueryModel>([], filter.PageNumber, filter.PageSize, 0))
                : GetListHandler(filter);
        }

        public Task<DefaultAlbumDetailQueryModel?> GetDetailAsync(string albumCode, CancellationToken cancellationToken)
        {
            return GetDetailHandler is null
                ? Task.FromResult<DefaultAlbumDetailQueryModel?>(null)
                : GetDetailHandler(albumCode);
        }

        public Task<DefaultAlbumDetailQueryModel?> GetActiveByProductCodeAsync(string productCode, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DefaultAlbumCreateResultModel> CreateAsync(DefaultAlbumCreateCommandModel command, CancellationToken cancellationToken)
        {
            return CreateHandler is null
                ? throw new NotSupportedException()
                : CreateHandler(command);
        }

        public Task<DefaultAlbumDetailQueryModel?> UpdateAsync(
            string albumCode,
            DefaultAlbumUpdateCommandModel command,
            CancellationToken cancellationToken)
        {
            return UpdateHandler is null
                ? throw new NotSupportedException()
                : UpdateHandler(albumCode, command);
        }
    }
}
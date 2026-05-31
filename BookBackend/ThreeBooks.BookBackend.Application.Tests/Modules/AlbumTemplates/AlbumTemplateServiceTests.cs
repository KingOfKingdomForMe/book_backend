using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Services;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.Common;
using Xunit;

namespace ThreeBooks.BookBackend.Application.Tests.Modules.AlbumTemplates;

public sealed class AlbumTemplateServiceTests
{
    private static readonly RequestContext Context = new(null, null);

    [Fact]
    public async Task GetListAsync_ReturnsJsonSourceForEachItem()
    {
        const string jsonSource = "{\"schemaVersion\":\"2.0\",\"blocks\":[]}";
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            GetListHandler = filter => Task.FromResult(new PagedResult<AlbumTemplateListItemQueryModel>(
            [
                new AlbumTemplateListItemQueryModel(
                    1,
                    "story-template",
                    "Story Template",
                    "Template Description",
                    "story-book",
                    "content",
                    "story",
                    "theme-story",
                    "2.0",
                    jsonSource,
                    new AlbumTemplateStoredFileReference("bookbackend-dev", "album-templates/story-template.png"),
                    true,
                    true,
                    10,
                    DateTime.UtcNow)
            ],
            filter.PageNumber,
            filter.PageSize,
            1))
        };

        var service = new AlbumTemplateService(queryStore);

        var response = await service.GetListAsync(new ListAlbumTemplatesRequest(null), Context, CancellationToken.None);

        var item = Assert.Single(response.Items);
        Assert.Equal(jsonSource, item.JsonSource);
    }

    [Fact]
    public async Task GetListAsync_ReturnsEmptyForCoverTemplatePageType()
    {
        var service = new AlbumTemplateService(new FakeAlbumTemplateQueryStore());

        var response = await service.GetListAsync(
            new ListAlbumTemplatesRequest(null, PageType: "cover-template"),
            Context,
            CancellationToken.None);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNullForCoverTemplate()
    {
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            GetDetailHandler = templateCode => Task.FromResult<AlbumTemplateDetailQueryModel?>(new AlbumTemplateDetailQueryModel(
                9,
                templateCode,
                "Generated Cover",
                "cover",
                null,
                "cover-template",
                null,
                null,
                "1.0",
                "{\"fields\":[]}",
                null,
                null,
                null,
                true,
                true,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow))
        };

        var service = new AlbumTemplateService(queryStore);

        var response = await service.GetDetailAsync("generated-cover-story-default", Context, CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task DeleteAsync_NormalizesTemplateCodeAndReturnsTrue()
    {
        string? deletedTemplateCode = null;
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            DeleteHandler = templateCode =>
            {
                deletedTemplateCode = templateCode;
                return Task.FromResult(true);
            }
        };

        var service = new AlbumTemplateService(queryStore);

        var deleted = await service.DeleteAsync(" Story-Template ", Context, CancellationToken.None);

        Assert.True(deleted);
        Assert.Equal("story-template", deletedTemplateCode);
    }

    [Fact]
    public async Task DeleteAllAsync_ReturnsDeletedCount()
    {
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            DeleteAllHandler = () => Task.FromResult(5)
        };

        var service = new AlbumTemplateService(queryStore);

        var deletedCount = await service.DeleteAllAsync(Context, CancellationToken.None);

        Assert.Equal(5, deletedCount);
    }

    private sealed class FakeAlbumTemplateQueryStore : IAlbumTemplateQueryStore
    {
        public Func<AlbumTemplateListFilter, Task<PagedResult<AlbumTemplateListItemQueryModel>>>? GetListHandler { get; init; }

        public Func<string, Task<AlbumTemplateDetailQueryModel?>>? GetDetailHandler { get; init; }

        public Func<string, Task<bool>>? DeleteHandler { get; init; }

        public Func<Task<int>>? DeleteAllHandler { get; init; }

        public Task<PagedResult<AlbumTemplateListItemQueryModel>> GetListAsync(
            AlbumTemplateListFilter filter,
            CancellationToken cancellationToken)
        {
            return GetListHandler is null
                ? Task.FromResult(new PagedResult<AlbumTemplateListItemQueryModel>([], filter.PageNumber, filter.PageSize, 0))
                : GetListHandler(filter);
        }

        public Task<AlbumTemplateDetailQueryModel?> GetDetailAsync(
            string templateCode,
            CancellationToken cancellationToken)
        {
            return GetDetailHandler is null
                ? Task.FromResult<AlbumTemplateDetailQueryModel?>(null)
                : GetDetailHandler(templateCode);
        }

        public Task<AlbumTemplateCreateResultModel> CreateAsync(
            AlbumTemplateCreateCommandModel command,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<AlbumTemplateDetailQueryModel?> UpdateAsync(
            string templateCode,
            AlbumTemplateUpdateCommandModel command,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(
            string templateCode,
            CancellationToken cancellationToken)
        {
            return DeleteHandler is null
                ? Task.FromResult(false)
                : DeleteHandler(templateCode);
        }

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken)
        {
            return DeleteAllHandler is null
                ? Task.FromResult(0)
                : DeleteAllHandler();
        }
    }
}
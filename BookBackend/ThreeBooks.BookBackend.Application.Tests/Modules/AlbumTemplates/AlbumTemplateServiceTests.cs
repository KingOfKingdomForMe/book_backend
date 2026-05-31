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

    private sealed class FakeAlbumTemplateQueryStore : IAlbumTemplateQueryStore
    {
        public Func<AlbumTemplateListFilter, Task<PagedResult<AlbumTemplateListItemQueryModel>>>? GetListHandler { get; init; }

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
            throw new NotSupportedException();
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
    }
}
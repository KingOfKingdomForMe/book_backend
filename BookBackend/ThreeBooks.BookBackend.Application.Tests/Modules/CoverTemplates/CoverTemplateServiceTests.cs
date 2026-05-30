using System.Text.Json;
using SkiaSharp;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Services;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Requests;
using Xunit;

namespace ThreeBooks.BookBackend.Application.Tests.Modules.CoverTemplates;

public sealed class CoverTemplateServiceTests
{
    private static readonly RequestContext Context = new(null, null);

    [Fact]
    public async Task GetDetailAsync_IgnoresLegacyCoverPageTemplates()
    {
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            GetDetailHandler = _ => Task.FromResult<AlbumTemplateDetailQueryModel?>(CreateDetailModel("legacy-cover", "cover", BuildSchemaJson()))
        };

        var service = CreateService(queryStore);

        var response = await service.GetDetailAsync("legacy-cover", Context, CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task CreateAsync_UsesDedicatedPageTypeAndSchemaVersion()
    {
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            CreateHandler = command => Task.FromResult(new AlbumTemplateCreateResultModel(
                10,
                command.TemplateCode,
                true,
                new AlbumTemplateStoredFileReference("bookbackend-dev", "cover-templates/defaults/story-cover-bg.png")))
        };

        var service = CreateService(queryStore);

        var response = await service.CreateAsync(
            new CreateCoverTemplateRequest(
                "story-template",
                "Story Template",
                "desc",
                88,
                [
                    new CoverTemplateFieldDefinitionRequest(
                        "title",
                        "Title",
                        "Enter title",
                        80,
                        90,
                        640,
                        120,
                        "Segoe UI",
                        32,
                        "#FFFFFF",
                        true)
                ]),
            Context,
            CancellationToken.None);

        Assert.NotNull(queryStore.LastCreateCommand);
        Assert.Equal("cover-template", queryStore.LastCreateCommand!.PageType);
        Assert.Equal("1.0", queryStore.LastCreateCommand.SchemaVersion);
        Assert.Contains("fieldId", queryStore.LastCreateCommand.JsonSource, StringComparison.Ordinal);
        Assert.Equal(10, response.TemplateId);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenFieldIdsDuplicate()
    {
        var service = CreateService(new FakeAlbumTemplateQueryStore());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new CreateCoverTemplateRequest(
                "story-template",
                "Story Template",
                null,
                9,
                [
                    new CoverTemplateFieldDefinitionRequest("title", "Title", null, 0, 0, 100, 20, "Segoe UI", 16, "#FFFFFF"),
                    new CoverTemplateFieldDefinitionRequest("title", "Title 2", null, 0, 30, 100, 20, "Segoe UI", 16, "#FFFFFF")
                ]),
            Context,
            CancellationToken.None));

        Assert.Contains("Duplicate field ids", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_UploadsGeneratedImageAndReturnsGeneratedFileId()
    {
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            GetDetailHandler = _ => Task.FromResult<AlbumTemplateDetailQueryModel?>(CreateDetailModel(
                "generated-cover-story-default",
                "cover-template",
                BuildSchemaJson()))
        };

        var fileObjectStore = new FakeFileObjectStore
        {
            DefaultBucket = "bookbackend-dev",
            DownloadHandler = (_, _) => Task.FromResult<StoredFileContent?>(new StoredFileContent(
                new MemoryStream(CreatePngBytes()),
                "image/png",
                0,
                "story-cover-bg.png"))
        };

        var metadataStore = new FakeFileMetadataStore { NextFileId = 321 };
        var service = CreateService(queryStore, fileObjectStore, metadataStore);

        var response = await service.GenerateAsync(
            "generated-cover-story-default",
            new GenerateCoverImageRequest([], "bookbackend-dev", "generated-covers/test-run", "final-cover"),
            Context,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(321, response!.FileId);
        Assert.Equal("bookbackend-dev", response.Bucket);
        Assert.Equal("final-cover.png", metadataStore.LastOriginalFileName);
        Assert.NotNull(fileObjectStore.UploadedBytes);
        Assert.True(fileObjectStore.UploadedBytes!.Length > 8);
        Assert.Equal(0x89, fileObjectStore.UploadedBytes[0]);
        Assert.Equal((byte)'P', fileObjectStore.UploadedBytes[1]);
        Assert.Equal((byte)'N', fileObjectStore.UploadedBytes[2]);
        Assert.Equal((byte)'G', fileObjectStore.UploadedBytes[3]);
        Assert.Contains("generated-covers/test-run/", response.ObjectKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenBackgroundIsUnsupportedRasterFormat()
    {
        var queryStore = new FakeAlbumTemplateQueryStore
        {
            GetDetailHandler = _ => Task.FromResult<AlbumTemplateDetailQueryModel?>(CreateDetailModel(
                "generated-cover-story-default",
                "cover-template",
                BuildSchemaJson()))
        };

        var fileObjectStore = new FakeFileObjectStore
        {
            DefaultBucket = "bookbackend-dev",
            DownloadHandler = (_, _) => Task.FromResult<StoredFileContent?>(new StoredFileContent(
                new MemoryStream([1, 2, 3, 4, 5]),
                "application/octet-stream",
                5,
                "background.bin"))
        };

        var service = CreateService(queryStore, fileObjectStore, new FakeFileMetadataStore());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateAsync(
            "generated-cover-story-default",
            new GenerateCoverImageRequest([]),
            Context,
            CancellationToken.None));

        Assert.Contains("supported raster image format", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CoverTemplateService CreateService(
        FakeAlbumTemplateQueryStore queryStore,
        FakeFileObjectStore? fileObjectStore = null,
        FakeFileMetadataStore? metadataStore = null)
    {
        return new CoverTemplateService(
            queryStore,
            fileObjectStore ?? new FakeFileObjectStore(),
            metadataStore ?? new FakeFileMetadataStore());
    }

    private static AlbumTemplateDetailQueryModel CreateDetailModel(
        string templateCode,
        string pageType,
        string jsonSource)
    {
        return new AlbumTemplateDetailQueryModel(
            1,
            templateCode,
            "Template",
            "Template Description",
            null,
            pageType,
            "generated-cover",
            "cover-template-story",
            "1.0",
            jsonSource,
            100,
            new AlbumTemplateStoredFileReference("bookbackend-dev", "cover-templates/defaults/story-cover-bg.png"),
            null,
            true,
            true,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }

    private static string BuildSchemaJson()
    {
        return JsonSerializer.Serialize(new
        {
            schemaVersion = "1.0",
            fields = new[]
            {
                new
                {
                    fieldId = "title",
                    displayName = "Title",
                    placeholder = "Enter title",
                    x = 60,
                    y = 80,
                    width = 300,
                    height = 120,
                    fontFamily = "Segoe UI",
                    fontSize = 24,
                    fontColor = "#FFFFFF",
                    isRequired = false,
                    sortOrder = 10,
                    defaultValue = string.Empty,
                    horizontalAlignment = "center",
                    verticalAlignment = "center",
                    maxLength = 50
                }
            }
        });
    }

    private static byte[] CreatePngBytes()
    {
        using var bitmap = new SKBitmap(32, 32, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(15, 18, 24, 255));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = new MemoryStream();
        data.SaveTo(output);
        return output.ToArray();
    }

    private sealed class FakeAlbumTemplateQueryStore : IAlbumTemplateQueryStore
    {
        public Func<AlbumTemplateListFilter, Task<PagedResult<AlbumTemplateListItemQueryModel>>>? GetListHandler { get; init; }

        public Func<string, Task<AlbumTemplateDetailQueryModel?>>? GetDetailHandler { get; init; }

        public Func<AlbumTemplateCreateCommandModel, Task<AlbumTemplateCreateResultModel>>? CreateHandler { get; init; }

        public Func<string, AlbumTemplateUpdateCommandModel, Task<AlbumTemplateDetailQueryModel?>>? UpdateHandler { get; init; }

        public AlbumTemplateCreateCommandModel? LastCreateCommand { get; private set; }

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
            LastCreateCommand = command;

            return CreateHandler is null
                ? Task.FromResult(new AlbumTemplateCreateResultModel(command.SortOrder, command.TemplateCode, command.IsActive, null))
                : CreateHandler(command);
        }

        public Task<AlbumTemplateDetailQueryModel?> UpdateAsync(
            string templateCode,
            AlbumTemplateUpdateCommandModel command,
            CancellationToken cancellationToken)
        {
            return UpdateHandler is null
                ? Task.FromResult<AlbumTemplateDetailQueryModel?>(null)
                : UpdateHandler(templateCode, command);
        }
    }

    private sealed class FakeFileObjectStore : IFileObjectStore
    {
        public string? DefaultBucket { get; init; }

        public Func<string, string, Task<StoredFileContent?>>? DownloadHandler { get; init; }

        public byte[]? UploadedBytes { get; private set; }

        public Task<StoredFileObject> UploadAsync(FileUploadCommand command, CancellationToken cancellationToken)
        {
            using var copy = new MemoryStream();
            command.Content.CopyTo(copy);
            UploadedBytes = copy.ToArray();

            return Task.FromResult(new StoredFileObject(
                command.Bucket,
                command.ObjectKey,
                command.FileName,
                command.ContentType,
                command.ContentLength));
        }

        public Task<Uri?> GetReadUrlAsync(string bucket, string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken)
        {
            return Task.FromResult<Uri?>(new Uri($"https://example.invalid/{bucket}/{objectKey}"));
        }

        public Task<StoredFileContent?> DownloadAsync(string bucket, string objectKey, CancellationToken cancellationToken)
        {
            return DownloadHandler is null
                ? Task.FromResult<StoredFileContent?>(null)
                : DownloadHandler(bucket, objectKey);
        }

        public Task<bool> DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeFileMetadataStore : IFileMetadataStore
    {
        public long NextFileId { get; init; } = 1;

        public string? LastOriginalFileName { get; private set; }

        public Task<StoredFileMetadata?> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoredFileMetadata?>(null);
        }

        public Task<long> SaveUploadAsync(
            StoredFileObject file,
            string originalFileName,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            LastOriginalFileName = originalFileName;
            return Task.FromResult(NextFileId);
        }

        public Task RecordAccessAsync(
            string bucket,
            string objectKey,
            DateTimeOffset expiresAtUtc,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkDeletedAsync(
            string bucket,
            string objectKey,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
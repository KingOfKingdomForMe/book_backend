using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Albums.Models;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Contracts.Albums.Requests;
using ThreeBooks.BookBackend.Contracts.Albums.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.Albums.Services;

public sealed class AlbumService(
    IAlbumQueryStore queryStore,
    IFileStorageService fileStorageService,
    IDefaultAlbumQueryStore defaultAlbumQueryStore) : IAlbumService
{
    private const int DefaultPageNumber = 1;

    private const int DefaultPageSize = 20;

    private const int MaxPageSize = 100;

    private const string DefaultAlbumBookType = "balbum";

    private const int DefaultAlbumStatus = 1;

    private const int DefaultAlbumVersionNo = 1;

    private const string DefaultJsonSchemaVersion = "2.0";

    private const int GeneratedShareCodeLength = 14;

    private const int MaxShareCodeGenerationAttempts = 16;

    private const string GeneratedShareCodeAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly Regex ShareCodePattern = new("^[a-z0-9_-]{4,32}$", RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "wechat",
        "link",
        "qrcode",
        "weibo"
    };

    public async Task<PagedResult<AlbumListItemResponse>> GetListAsync(
        ListAlbumsRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new AlbumListFilter(
            NormalizePositiveId(request.UserId, nameof(request.UserId)),
            NormalizeListPageNumber(request.PageNumber),
            NormalizeListPageSize(request.PageSize));

        var result = await queryStore.GetListAsync(filter, cancellationToken);

        return new PagedResult<AlbumListItemResponse>(
            result.Items.Select(MapListItem).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<CreateAlbumResponse> CreateAlbumAsync(
        CreateAlbumRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = NormalizePositiveId(request.UserId, nameof(request.UserId));
        var title = NormalizeRequiredText(request.Title, nameof(request.Title), 256);
        var subtitle = NormalizeOptionalText(request.Subtitle, 256, nameof(request.Subtitle));
        var providedShareCode = NormalizeOptionalShareCode(request.ShareCode);
        var normalizedProductCode = string.IsNullOrWhiteSpace(request.ProductCode)
            ? DefaultAlbumBookType
            : NormalizeCode(request.ProductCode, nameof(request.ProductCode), 32);
        var pagesCommand = BuildOptionalPagesWriteCommand(request.Pages);
        DefaultAlbumDetailQueryModel? defaultAlbum = null;
        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            defaultAlbum = await defaultAlbumQueryStore.GetActiveByProductCodeAsync(normalizedProductCode, cancellationToken);

            if (pagesCommand is null)
            {
                pagesCommand = BuildDefaultAlbumPagesWriteCommand(
                    defaultAlbum,
                    nameof(request.ProductCode));
            }
        }

        var extraProperties = defaultAlbum?.ExtraProperties ?? Array.Empty<string>();

        AlbumCreateCommandModel BuildCommand(string shareCode)
        {
            return new AlbumCreateCommandModel(
                userId,
                shareCode,
                title,
                subtitle,
                normalizedProductCode,
                normalizedProductCode,
                DefaultAlbumStatus,
                request.IsPublic,
                DefaultAlbumVersionNo,
                DefaultJsonSchemaVersion,
                null,
                extraProperties);
        }

        var result = providedShareCode is null
            ? await CreateAlbumWithGeneratedShareCodeAsync(BuildCommand, pagesCommand, cancellationToken)
            : await queryStore.CreateAlbumAsync(BuildCommand(providedShareCode), pagesCommand, cancellationToken);

        return new CreateAlbumResponse(
            result.ProjectId,
            result.VersionId,
            result.ShareCode,
            result.IsPublic ? BuildShareUrl(result.ShareCode) : null);
    }

    private async Task<AlbumCreateResultModel> CreateAlbumWithGeneratedShareCodeAsync(
        Func<string, AlbumCreateCommandModel> buildCommand,
        AlbumPagesWriteCommandModel? pagesCommand,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxShareCodeGenerationAttempts; attempt++)
        {
            var generatedShareCode = GenerateShareCode();
            if (await queryStore.ShareCodeExistsAsync(generatedShareCode, cancellationToken))
            {
                continue;
            }

            try
            {
                return await queryStore.CreateAlbumAsync(buildCommand(generatedShareCode), pagesCommand, cancellationToken);
            }
            catch (ArgumentException exception) when (IsShareCodeConflict(exception))
            {
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique share code.");
    }

    public async Task<SaveAlbumPagesResponse?> SavePagesAsync(
        string shareCode,
        SaveAlbumPagesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedShareCode = NormalizeShareCode(shareCode);
        var result = await queryStore.SavePagesAsync(normalizedShareCode, BuildPagesWriteCommand(request.Pages), cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new SaveAlbumPagesResponse(
            result.ProjectId,
            result.VersionId,
            result.ShareCode,
            result.PageCount,
            result.ImageCount,
            result.IsPublic ? BuildShareUrl(result.ShareCode) : null);
    }

    public async Task<AlbumPreviewDetailResponse?> GetPreviewAsync(
        string shareCode,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedShareCode = NormalizeShareCode(shareCode);
        var preview = await queryStore.GetPreviewAsync(normalizedShareCode, cancellationToken);

        if (preview is null)
        {
            return null;
        }

        var pages = await Task.WhenAll(preview.Pages.Select(async page =>
            new AlbumPreviewPageSummaryResponse(
                page.PageNo,
                page.PageLabel,
                page.PageType,
                BuildOptionalProxyUrl(page.ThumbnailFile),
                await ResolveJsonSourceAsync(page.JsonSource, page.JsonFile, context, cancellationToken),
                page.HasImages)));

        return new AlbumPreviewDetailResponse(
            preview.ShareCode,
            preview.Title,
            preview.Subtitle,
            preview.PageCount,
            preview.ImageCount,
            preview.ViewCount,
            preview.ShareCount,
            ResolveProductCode(preview.ProductCode, preview.BookType),
            preview.ExtraProperties,
            BuildShareUrl(preview.ShareCode),
            pages);
    }

    public async Task<AlbumPreviewPageResponse?> GetPageAsync(
        string shareCode,
        int pageNumber,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedShareCode = NormalizeShareCode(shareCode);
        var normalizedPageNumber = NormalizePageNumber(pageNumber);
        var page = await queryStore.GetPageAsync(normalizedShareCode, normalizedPageNumber, cancellationToken);

        if (page is null)
        {
            return null;
        }

        var jsonSource = await ResolveJsonSourceAsync(page.JsonSource, page.JsonFile, context, cancellationToken);
        var pageData = ParseJsonData(jsonSource);

        var images = page.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => new AlbumPreviewPageImageResponse(
                image.SortOrder,
                image.Role,
                BuildProxyUrl(image.File),
                image.Width,
                image.Height,
                image.AltText,
                image.Caption))
            .ToArray();

        return new AlbumPreviewPageResponse(
            page.PageNo,
            page.PageLabel,
            page.PageType,
            page.SchemaVersion,
            jsonSource,
            BuildOptionalProxyUrl(page.HtmlFile),
            pageData,
            images);
    }

    public async Task<AlbumPreviewShareInfoResponse?> GetShareInfoAsync(
        string shareCode,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedShareCode = NormalizeShareCode(shareCode);
        var preview = await queryStore.GetPreviewAsync(normalizedShareCode, cancellationToken);

        if (preview is null)
        {
            return null;
        }

        var coverThumbnail = preview.Pages
            .FirstOrDefault(page => string.Equals(page.PageType, "cover", StringComparison.OrdinalIgnoreCase))
            ?.ThumbnailFile;

        if (coverThumbnail is null)
        {
            coverThumbnail = preview.Pages.FirstOrDefault(page => page.ThumbnailFile is not null)?.ThumbnailFile;
        }

        return new AlbumPreviewShareInfoResponse(
            preview.ShareCode,
            BuildShareUrl(preview.ShareCode),
            preview.Title,
            preview.Subtitle,
            BuildOptionalProxyUrl(coverThumbnail),
            preview.PageCount,
            preview.ViewCount,
            preview.ShareCount,
            ResolveProductCode(preview.ProductCode, preview.BookType));
    }

    public Task<bool> RecordViewAsync(
        string shareCode,
        int? pageNumber,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedShareCode = NormalizeShareCode(shareCode);
        int? normalizedPageNumber = pageNumber.HasValue ? NormalizePageNumber(pageNumber.Value) : null;

        return queryStore.RecordViewAsync(
            normalizedShareCode,
            normalizedPageNumber,
            context.IpAddress,
            context.UserAgent,
            cancellationToken);
    }

    public Task<bool> RecordShareAsync(
        string shareCode,
        string channel,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedShareCode = NormalizeShareCode(shareCode);
        var normalizedChannel = NormalizeChannel(channel);

        return queryStore.RecordShareAsync(
            normalizedShareCode,
            normalizedChannel,
            context.IpAddress,
            context.UserAgent,
            cancellationToken);
    }

    private static AlbumListItemResponse MapListItem(AlbumListItemQueryModel item)
    {
        return new AlbumListItemResponse(
            item.ProjectId,
            item.ShareCode,
            item.Title,
            item.Subtitle,
            item.BookType,
            ResolveProductCode(item.ProductCode, item.BookType),
            item.IsPublic,
            item.PageCount,
            item.ImageCount,
            item.ImageCount,
            item.ExtraProperties,
            item.ViewCount,
            item.ShareCount,
            item.IsPublic && !string.IsNullOrWhiteSpace(item.ShareCode) ? BuildShareUrl(item.ShareCode) : null,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    private static string NormalizeShareCode(string shareCode)
    {
        var normalized = shareCode?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Share code is required.", nameof(shareCode));
        }

        if (!ShareCodePattern.IsMatch(normalized))
        {
            throw new ArgumentException("Share code must be 4-32 chars and contain only lowercase letters, numbers, '_' or '-'.", nameof(shareCode));
        }

        return normalized;
    }

    private static string? NormalizeOptionalShareCode(string? shareCode)
    {
        var normalized = shareCode?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : NormalizeShareCode(normalized);
    }

    private static int NormalizePageNumber(int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
        }

        return pageNumber;
    }

    private static string NormalizeChannel(string channel)
    {
        var normalized = channel?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Channel is required.", nameof(channel));
        }

        if (!AllowedChannels.Contains(normalized))
        {
            throw new ArgumentException("Unsupported share channel.", nameof(channel));
        }

        return normalized;
    }

    private static AlbumPageWriteCommandModel BuildPageWriteCommand(
        SaveAlbumPageRequest request,
        ISet<int> pageNumbers)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageNo = NormalizePositiveNumber(request.PageNo, nameof(request.PageNo));
        if (!pageNumbers.Add(pageNo))
        {
            throw new ArgumentException($"Duplicate page number is not allowed: {pageNo}.", nameof(request.PageNo));
        }

        var images = (request.Images ?? Array.Empty<SaveAlbumPageAssetRequest>())
            .Select(image => new AlbumPageAssetWriteModel(
                NormalizeNonNegative(image.SortOrder, nameof(image.SortOrder)),
                NormalizeCode(image.Role, nameof(image.Role), 32),
                NormalizePositiveId(image.FileId, nameof(image.FileId)),
                NormalizeNullableDimension(image.Width, nameof(image.Width)),
                NormalizeNullableDimension(image.Height, nameof(image.Height)),
                NormalizeOptionalText(image.AltText, 256, nameof(image.AltText)),
                NormalizeOptionalText(image.Caption, 256, nameof(image.Caption)),
                NormalizeOptionalJson(image.CropData)))
            .ToArray();

        var jsonSource = NormalizeRequiredJsonSource(request.JsonSource, nameof(request.JsonSource));
        var thumbnailFileId = images
            .OrderBy(image => image.SortOrder)
            .Select(image => (long?)image.FileId)
            .FirstOrDefault();
        var schemaVersion = ExtractSchemaVersion(jsonSource);

        return new AlbumPageWriteCommandModel(
            pageNo,
            ResolvePageLabel(request.PageLabel, pageNo),
            ResolvePageType(request.PageType, jsonSource),
            request.SortOrder.HasValue ? NormalizeNonNegative(request.SortOrder.Value, nameof(request.SortOrder)) : pageNo,
            jsonSource,
            NormalizeNullableId(request.HtmlFileId, nameof(request.HtmlFileId)),
            thumbnailFileId,
            NormalizeNullableDimension(request.PageWidth, nameof(request.PageWidth)),
            NormalizeNullableDimension(request.PageHeight, nameof(request.PageHeight)),
            schemaVersion,
            images);
    }

    private static AlbumPagesWriteCommandModel BuildPagesWriteCommand(IReadOnlyCollection<SaveAlbumPageRequest>? requestPages)
    {
        ArgumentNullException.ThrowIfNull(requestPages);

        var pageNumbers = new HashSet<int>();
        var pages = requestPages
            .Select(page => BuildPageWriteCommand(page, pageNumbers))
            .OrderBy(page => page.SortOrder)
            .ThenBy(page => page.PageNo)
            .ToArray();

        return new AlbumPagesWriteCommandModel(pages, ResolveSnapshotSchemaVersion(pages));
    }

    private static AlbumPagesWriteCommandModel? BuildOptionalPagesWriteCommand(IReadOnlyCollection<SaveAlbumPageRequest>? requestPages)
    {
        return requestPages is null ? null : BuildPagesWriteCommand(requestPages);
    }

    private AlbumPagesWriteCommandModel BuildDefaultAlbumPagesWriteCommand(
        DefaultAlbumDetailQueryModel? defaultAlbum,
        string parameterName)
    {
        if (defaultAlbum is null)
        {
            throw new ArgumentException("ProductCode does not have an active default album.", parameterName);
        }

        if (defaultAlbum.Templates.Count == 0)
        {
            throw new ArgumentException("ProductCode default album does not contain any templates.", parameterName);
        }

        var pages = defaultAlbum.Templates
            .OrderBy(template => template.SortOrder)
            .ThenBy(template => template.ItemId)
            .Select((template, index) => new AlbumPageWriteCommandModel(
                index + 1,
                ResolveDefaultAlbumPageLabel(template.Name, index + 1),
                NormalizeCode(template.PageType, nameof(template.PageType), 32, "content"),
                NormalizeNonNegative(template.SortOrder, nameof(template.SortOrder)),
                NormalizeRequiredJsonSource(template.JsonSource, nameof(template.JsonSource)),
                null,
                null,
                null,
                null,
                NormalizeVersion(template.SchemaVersion),
                Array.Empty<AlbumPageAssetWriteModel>()))
            .ToArray();

        return new AlbumPagesWriteCommandModel(pages, ResolveSnapshotSchemaVersion(pages));
    }

    private static string ResolveDefaultAlbumPageLabel(string? templateName, int pageNo)
    {
        var normalized = templateName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"第{pageNo}页";
        }

        return normalized.Length <= 64 ? normalized : normalized[..64];
    }

    private static string ResolvePageLabel(string? pageLabel, int pageNo)
    {
        return string.IsNullOrWhiteSpace(pageLabel)
            ? $"第{pageNo}页"
            : NormalizeRequiredText(pageLabel, nameof(pageLabel), 64);
    }

    private static string ResolvePageType(string? pageType, string jsonSource)
    {
        return NormalizeCode(pageType, nameof(pageType), 32, TryExtractTopLevelString(jsonSource, "pageType") ?? "content");
    }

    private static string ResolveSnapshotSchemaVersion(IReadOnlyCollection<AlbumPageWriteCommandModel> pages)
    {
        return pages.Count == 0
            ? DefaultJsonSchemaVersion
            : NormalizeVersion(pages.First().SchemaVersion);
    }

    private static string? TryExtractTopLevelString(string jsonSource, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonSource);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static bool IsShareCodeConflict(ArgumentException exception)
    {
        return string.Equals(exception.ParamName, "ShareCode", StringComparison.Ordinal)
            && exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static long NormalizePositiveId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than 0.");
        }

        return value;
    }

    private static long? NormalizeNullableId(long? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return NormalizePositiveId(value.Value, parameterName);
    }

    private static int NormalizePositiveNumber(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than 0.");
        }

        return value;
    }

    private static int NormalizeListPageNumber(int value)
    {
        return value < 1 ? DefaultPageNumber : value;
    }

    private static int NormalizeListPageSize(int value)
    {
        if (value < 1)
        {
            return DefaultPageSize;
        }

        return value > MaxPageSize ? MaxPageSize : value;
    }

    private static int NormalizeNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than or equal to 0.");
        }

        return value;
    }

    private static int? NormalizeNullableDimension(int? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return NormalizePositiveNumber(value.Value, parameterName);
    }

    private static int NormalizeStatus(int status)
    {
        if (status is < 0 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Status must be between 0 and 4.");
        }

        return status;
    }

    private static string NormalizeRequiredText(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string ExtractSchemaVersion(string jsonSource)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonSource);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement)
                && schemaVersionElement.ValueKind == JsonValueKind.String)
            {
                return NormalizeVersion(schemaVersionElement.GetString());
            }
        }
        catch (JsonException)
        {
        }

        return DefaultJsonSchemaVersion;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeCode(string? value, string parameterName, int maxLength, string? fallback = null)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback?.Trim()
            : value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized.ToLowerInvariant();
    }

    private static string NormalizeVersion(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "1.0" : value.Trim();
        if (normalized.Length > 16)
        {
            throw new ArgumentException("Schema version length cannot exceed 16 characters.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeRequiredJsonSource(string? value, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        try
        {
            using var document = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }

        return normalized;
    }

    private static string? NormalizeOptionalJson(JsonElement? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var element = value.Value;
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return element.GetRawText();
    }

    private async Task<string> ResolveJsonSourceAsync(
        string? jsonSource,
        AlbumStoredFileReference? legacyJsonFile,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedJsonSource = NormalizeExistingJsonSource(jsonSource);
        if (!string.IsNullOrWhiteSpace(normalizedJsonSource))
        {
            return normalizedJsonSource;
        }

        if (legacyJsonFile is null)
        {
            return string.Empty;
        }

        return await LoadJsonSourceAsync(legacyJsonFile, context, cancellationToken) ?? string.Empty;
    }

    private async Task<string?> LoadJsonSourceAsync(
        AlbumStoredFileReference file,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var content = await fileStorageService.DownloadAsync(file.Bucket, file.ObjectKey, context, cancellationToken);
        if (content is null)
        {
            return null;
        }

        await using var stream = content.Content;
        using var lease = content.Lease;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static JsonElement? ParseJsonData(string? jsonSource)
    {
        if (string.IsNullOrWhiteSpace(jsonSource))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonSource);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? NormalizeExistingJsonSource(string? jsonSource)
    {
        var normalized = jsonSource?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string ResolveProductCode(string? productCode, string bookType)
    {
        var normalizedProductCode = productCode?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedProductCode))
        {
            return normalizedProductCode;
        }

        return bookType.Trim().ToLowerInvariant();
    }

    private static string GenerateShareCode()
    {
        Span<char> buffer = stackalloc char[GeneratedShareCodeLength];

        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] = GeneratedShareCodeAlphabet[RandomNumberGenerator.GetInt32(GeneratedShareCodeAlphabet.Length)];
        }

        return new string(buffer);
    }

    private static string BuildShareUrl(string shareCode)
    {
        return $"/albums/{Uri.EscapeDataString(shareCode)}";
    }

    private static string BuildProxyUrl(AlbumStoredFileReference file)
    {
        return BuildProxyUrl(file.Bucket, file.ObjectKey);
    }

    private static string? BuildOptionalProxyUrl(AlbumStoredFileReference? file)
    {
        return file is null ? null : BuildProxyUrl(file.Bucket, file.ObjectKey);
    }

    private static string BuildProxyUrl(string bucket, string objectKey)
    {
        return $"/api/files/content/{Uri.EscapeDataString(bucket)}/{EncodeObjectKeyForPath(objectKey)}";
    }

    private static string EncodeObjectKeyForPath(string objectKey)
    {
        return string.Join(
            '/',
            objectKey
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }
}
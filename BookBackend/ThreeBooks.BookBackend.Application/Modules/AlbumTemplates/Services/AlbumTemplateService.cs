using System.Text.Json;
using System.Text.RegularExpressions;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Services;

public sealed class AlbumTemplateService(IAlbumTemplateQueryStore queryStore) : IAlbumTemplateService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const string DefaultSchemaVersion = "2.0";
    private const string CoverTemplatePageType = "cover-template";

    private static readonly Regex CodePattern = new("^[a-z0-9_-]{2,64}$", RegexOptions.Compiled);

    public async Task<PagedResult<AlbumTemplateListItemResponse>> GetListAsync(
        ListAlbumTemplatesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageType = NormalizeOptionalCode(request.PageType, nameof(request.PageType), 32);
        if (IsCoverTemplatePageType(pageType))
        {
            return new PagedResult<AlbumTemplateListItemResponse>(
                [],
                NormalizePageNumber(request.PageNumber),
                NormalizePageSize(request.PageSize),
                0);
        }

        var filter = new AlbumTemplateListFilter(
            NormalizeKeyword(request.Keyword),
            NormalizeOptionalCode(request.BookType, nameof(request.BookType), 32),
            pageType,
            NormalizeOptionalCode(request.Category, nameof(request.Category), 32),
            request.IsActive,
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetListAsync(filter, cancellationToken);

        return new PagedResult<AlbumTemplateListItemResponse>(
            result.Items.Select(MapListItem).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<AlbumTemplateDetailResponse?> GetDetailAsync(
        string templateCode,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateCode = NormalizeRequiredCode(templateCode, nameof(templateCode), 64);
        var detail = await queryStore.GetDetailAsync(normalizedTemplateCode, cancellationToken);
        return detail is null || IsCoverTemplatePageType(detail.PageType) ? null : MapDetail(detail);
    }

    public async Task<CreateAlbumTemplateResponse> CreateAsync(
        CreateAlbumTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var jsonSource = NormalizeRequiredJsonSource(request.JsonSource, nameof(request.JsonSource));
        var command = new AlbumTemplateCreateCommandModel(
            NormalizeRequiredCode(request.TemplateCode, nameof(request.TemplateCode), 64),
            NormalizeRequiredText(request.Name, nameof(request.Name), 128),
            NormalizeOptionalText(request.Description, 512, nameof(request.Description)),
            NormalizeOptionalCode(request.BookType, nameof(request.BookType), 32),
            NormalizeAlbumTemplatePageType(request.PageType, nameof(request.PageType)),
            NormalizeOptionalCode(request.Category, nameof(request.Category), 32),
            NormalizeOptionalCode(request.ThemeCode, nameof(request.ThemeCode), 64),
            NormalizeSchemaVersion(request.SchemaVersion, jsonSource),
            jsonSource,
            NormalizeNullableId(request.PreviewFileId, nameof(request.PreviewFileId)),
            NormalizeNullableId(request.CreatedByUserId, nameof(request.CreatedByUserId)),
            request.IsBuiltIn,
            request.IsActive,
            NormalizeSortOrder(request.SortOrder));

        var result = await queryStore.CreateAsync(command, cancellationToken);

        return new CreateAlbumTemplateResponse(
            result.TemplateId,
            result.TemplateCode,
            result.IsActive,
            BuildOptionalProxyUrl(result.PreviewFile));
    }

    public async Task<AlbumTemplateDetailResponse?> UpdateAsync(
        string templateCode,
        UpdateAlbumTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedTemplateCode = NormalizeRequiredCode(templateCode, nameof(templateCode), 64);
        var existing = await queryStore.GetDetailAsync(normalizedTemplateCode, cancellationToken);
        if (existing is null || IsCoverTemplatePageType(existing.PageType))
        {
            return null;
        }

        var jsonSource = NormalizeRequiredJsonSource(request.JsonSource, nameof(request.JsonSource));
        var command = new AlbumTemplateUpdateCommandModel(
            NormalizeRequiredText(request.Name, nameof(request.Name), 128),
            NormalizeOptionalText(request.Description, 512, nameof(request.Description)),
            NormalizeOptionalCode(request.BookType, nameof(request.BookType), 32),
            NormalizeAlbumTemplatePageType(request.PageType, nameof(request.PageType)),
            NormalizeOptionalCode(request.Category, nameof(request.Category), 32),
            NormalizeOptionalCode(request.ThemeCode, nameof(request.ThemeCode), 64),
            NormalizeSchemaVersion(request.SchemaVersion, jsonSource),
            jsonSource,
            NormalizeNullableId(request.PreviewFileId, nameof(request.PreviewFileId)),
            NormalizeNullableId(request.CreatedByUserId, nameof(request.CreatedByUserId)),
            request.IsBuiltIn,
            request.IsActive,
            NormalizeSortOrder(request.SortOrder));

        var result = await queryStore.UpdateAsync(normalizedTemplateCode, command, cancellationToken);
        return result is null ? null : MapDetail(result);
    }

    public async Task<bool> DeleteAsync(
        string templateCode,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateCode = NormalizeRequiredCode(templateCode, nameof(templateCode), 64);
        return await queryStore.DeleteAsync(normalizedTemplateCode, cancellationToken);
    }

    public async Task<int> DeleteAllAsync(
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return await queryStore.DeleteAllAsync(cancellationToken);
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        var normalized = keyword?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
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

    private static string NormalizeRequiredCode(string? value, string parameterName, int maxLength)
    {
        var normalized = NormalizeOptionalCode(value, parameterName, maxLength);
        if (normalized is null)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeAlbumTemplatePageType(string? value, string parameterName)
    {
        var normalized = NormalizeRequiredCode(value, parameterName, 32);
        if (IsCoverTemplatePageType(normalized))
        {
            throw new ArgumentException("Cover templates are managed by the cover-templates APIs.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalCode(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.", parameterName);
        }

        if (!CodePattern.IsMatch(normalized))
        {
            throw new ArgumentException("Value format is invalid.", parameterName);
        }

        return normalized;
    }

    private static long? NormalizeNullableId(long? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than 0.");
        }

        return value.Value;
    }

    private static int NormalizeSortOrder(int sortOrder)
    {
        return sortOrder < 0 ? 0 : sortOrder;
    }

    private static string NormalizeRequiredJsonSource(string? jsonSource, string parameterName)
    {
        var normalized = jsonSource?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"Value is not valid JSON: {exception.Message}", parameterName, exception);
        }

        return normalized;
    }

    private static string NormalizeSchemaVersion(string? requestedSchemaVersion, string jsonSource)
    {
        var normalizedRequested = NormalizeOptionalText(requestedSchemaVersion, 16, nameof(requestedSchemaVersion));
        if (!string.IsNullOrWhiteSpace(normalizedRequested))
        {
            return normalizedRequested;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonSource);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement)
                && schemaVersionElement.ValueKind == JsonValueKind.String)
            {
                var extracted = schemaVersionElement.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    return extracted;
                }
            }
        }
        catch (JsonException)
        {
        }

        return DefaultSchemaVersion;
    }

    private static int NormalizePageNumber(int pageNumber)
    {
        return pageNumber < 1 ? DefaultPageNumber : pageNumber;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1)
        {
            return DefaultPageSize;
        }

        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }

    private static bool IsCoverTemplatePageType(string? pageType)
    {
        return string.Equals(pageType, CoverTemplatePageType, StringComparison.OrdinalIgnoreCase);
    }

    private static AlbumTemplateListItemResponse MapListItem(AlbumTemplateListItemQueryModel item)
    {
        return new AlbumTemplateListItemResponse(
            item.TemplateId,
            item.TemplateCode,
            item.Name,
            item.Description,
            item.BookType,
            item.PageType,
            item.Category,
            item.ThemeCode,
            item.SchemaVersion,
            item.JsonSource,
            BuildOptionalProxyUrl(item.PreviewFile),
            item.IsBuiltIn,
            item.IsActive,
            item.SortOrder,
            item.UpdatedAtUtc);
    }

    private static AlbumTemplateDetailResponse MapDetail(AlbumTemplateDetailQueryModel item)
    {
        return new AlbumTemplateDetailResponse(
            item.TemplateId,
            item.TemplateCode,
            item.Name,
            item.Description,
            item.BookType,
            item.PageType,
            item.Category,
            item.ThemeCode,
            item.SchemaVersion,
            item.JsonSource,
            BuildOptionalProxyUrl(item.PreviewFile),
            item.PreviewFileId,
            item.CreatedByUserId,
            item.IsBuiltIn,
            item.IsActive,
            item.SortOrder,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    private static string? BuildOptionalProxyUrl(AlbumTemplateStoredFileReference? file)
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
using System.Text.RegularExpressions;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Requests;
using ThreeBooks.BookBackend.Contracts.DefaultAlbums.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Services;

public sealed class DefaultAlbumService(IDefaultAlbumQueryStore queryStore) : IDefaultAlbumService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly Regex CodePattern = new("^[a-z0-9_-]{2,64}$", RegexOptions.Compiled);

    public async Task<PagedResult<DefaultAlbumListItemResponse>> GetListAsync(
        ListDefaultAlbumsRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new DefaultAlbumListFilter(
            NormalizeKeyword(request.Keyword),
            NormalizeOptionalCode(request.ProductCode, nameof(request.ProductCode), 32),
            NormalizeOptionalCode(request.BookType, nameof(request.BookType), 32),
            NormalizeOptionalCode(request.Category, nameof(request.Category), 32),
            request.IsActive,
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetListAsync(filter, cancellationToken);

        return new PagedResult<DefaultAlbumListItemResponse>(
            result.Items.Select(MapListItem).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<DefaultAlbumDetailResponse?> GetDetailAsync(
        string albumCode,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedAlbumCode = NormalizeRequiredCode(albumCode, nameof(albumCode), 64);
        var detail = await queryStore.GetDetailAsync(normalizedAlbumCode, cancellationToken);
        return detail is null ? null : MapDetail(detail);
    }

    public async Task<CreateDefaultAlbumResponse> CreateAsync(
        CreateDefaultAlbumRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedProductCode = NormalizeRequiredCode(request.ProductCode, nameof(request.ProductCode), 32);

        var command = new DefaultAlbumCreateCommandModel(
            NormalizeRequiredCode(request.AlbumCode, nameof(request.AlbumCode), 64),
            normalizedProductCode,
            NormalizeRequiredText(request.Name, nameof(request.Name), 128),
            NormalizeOptionalText(request.Description, 512, nameof(request.Description)),
            NormalizeOptionalCode(request.BookType, nameof(request.BookType), 32) ?? normalizedProductCode,
            NormalizeOptionalCode(request.Category, nameof(request.Category), 32),
            NormalizeOptionalCode(request.ThemeCode, nameof(request.ThemeCode), 64),
            NormalizeExtraProperties(request.ExtraProperties, nameof(request.ExtraProperties)),
            NormalizeNullableId(request.PreviewFileId, nameof(request.PreviewFileId)),
            NormalizeNullableId(request.CreatedByUserId, nameof(request.CreatedByUserId)),
            request.IsActive,
            NormalizeSortOrder(request.SortOrder),
            NormalizeTemplateAssignments(request.Templates, nameof(request.Templates)));

        var result = await queryStore.CreateAsync(command, cancellationToken);

        return new CreateDefaultAlbumResponse(
            result.AlbumId,
            result.AlbumCode,
            result.IsActive,
            BuildOptionalProxyUrl(result.PreviewFile));
    }

    public async Task<DefaultAlbumDetailResponse?> UpdateAsync(
        string albumCode,
        UpdateDefaultAlbumRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedAlbumCode = NormalizeRequiredCode(albumCode, nameof(albumCode), 64);
        var normalizedProductCode = NormalizeRequiredCode(request.ProductCode, nameof(request.ProductCode), 32);
        var command = new DefaultAlbumUpdateCommandModel(
            normalizedProductCode,
            NormalizeRequiredText(request.Name, nameof(request.Name), 128),
            NormalizeOptionalText(request.Description, 512, nameof(request.Description)),
            NormalizeOptionalCode(request.BookType, nameof(request.BookType), 32) ?? normalizedProductCode,
            NormalizeOptionalCode(request.Category, nameof(request.Category), 32),
            NormalizeOptionalCode(request.ThemeCode, nameof(request.ThemeCode), 64),
            NormalizeExtraProperties(request.ExtraProperties, nameof(request.ExtraProperties)),
            NormalizeNullableId(request.PreviewFileId, nameof(request.PreviewFileId)),
            NormalizeNullableId(request.CreatedByUserId, nameof(request.CreatedByUserId)),
            request.IsActive,
            NormalizeSortOrder(request.SortOrder),
            NormalizeTemplateAssignments(request.Templates, nameof(request.Templates)));

        var result = await queryStore.UpdateAsync(normalizedAlbumCode, command, cancellationToken);
        return result is null ? null : MapDetail(result);
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

    private static IReadOnlyCollection<string> NormalizeExtraProperties(
        IReadOnlyCollection<string>? extraProperties,
        string parameterName)
    {
        if (extraProperties is null || extraProperties.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>(extraProperties.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var extraProperty in extraProperties)
        {
            var value = extraProperty?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Length > 128)
            {
                throw new ArgumentException("Value length cannot exceed 128 characters.", parameterName);
            }

            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }

    private static IReadOnlyCollection<DefaultAlbumTemplateAssignmentModel> NormalizeTemplateAssignments(
        IReadOnlyCollection<DefaultAlbumTemplateAssignmentRequest>? templates,
        string parameterName)
    {
        if (templates is null || templates.Count == 0)
        {
            throw new ArgumentException("At least one template is required.", parameterName);
        }

        var normalized = new List<DefaultAlbumTemplateAssignmentModel>(templates.Count);
        var seenTemplateCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var template in templates)
        {
            var templateCode = NormalizeRequiredCode(template.TemplateCode, nameof(template.TemplateCode), 64);
            if (!seenTemplateCodes.Add(templateCode))
            {
                throw new ArgumentException("TemplateCode cannot be duplicated.", parameterName);
            }

            normalized.Add(new DefaultAlbumTemplateAssignmentModel(
                templateCode,
                NormalizeSortOrder(template.SortOrder)));
        }

        return normalized;
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

    private static DefaultAlbumListItemResponse MapListItem(DefaultAlbumListItemQueryModel item)
    {
        return new DefaultAlbumListItemResponse(
            item.AlbumId,
            item.AlbumCode,
            item.ProductCode,
            item.Name,
            item.Description,
            item.BookType,
            item.Category,
            item.ThemeCode,
            item.ExtraProperties,
            BuildOptionalProxyUrl(item.PreviewFile),
            item.TemplateCount,
            item.IsActive,
            item.SortOrder,
            item.UpdatedAtUtc);
    }

    private static DefaultAlbumDetailResponse MapDetail(DefaultAlbumDetailQueryModel item)
    {
        var templates = item.Templates.Select(MapTemplateItem).ToArray();

        return new DefaultAlbumDetailResponse(
            item.AlbumId,
            item.AlbumCode,
            item.ProductCode,
            item.Name,
            item.Description,
            item.BookType,
            item.Category,
            item.ThemeCode,
            item.ExtraProperties,
            BuildOptionalProxyUrl(item.PreviewFile),
            item.PreviewFileId,
            item.CreatedByUserId,
            templates.Length,
            item.IsActive,
            item.SortOrder,
            templates,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    private static DefaultAlbumTemplateItemResponse MapTemplateItem(DefaultAlbumTemplateItemQueryModel item)
    {
        return new DefaultAlbumTemplateItemResponse(
            item.ItemId,
            item.TemplateId,
            item.TemplateCode,
            item.Name,
            item.Description,
            item.PageType,
            item.Category,
            item.ThemeCode,
            item.SchemaVersion,
            item.JsonSource,
            BuildOptionalProxyUrl(item.PreviewFile),
            item.SortOrder);
    }

    private static string? BuildOptionalProxyUrl(DefaultAlbumStoredFileReference? file)
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
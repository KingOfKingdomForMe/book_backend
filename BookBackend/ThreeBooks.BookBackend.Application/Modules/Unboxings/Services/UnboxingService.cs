using System.Globalization;
using System.Text.RegularExpressions;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Models;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Unboxings.Requests;
using ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Unboxings.Services;

public sealed class UnboxingService(IUnboxingQueryStore queryStore) : IUnboxingService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int ExcerptMaxLength = 140;
    private const int MaxShortTextLength = 256;
    private const int MaxMediumTextLength = 512;
    private const int MaxPostNoLength = 32;
    private const int MaxTagCount = 12;
    private const int MinGeneratedPostNo = 40001;

    private static readonly Regex CodePattern = new("^[a-z0-9_-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex PostNoPattern = new("^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex MediaTypePattern = new("^(image|video)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<PagedResult<AdminUnboxingListItemResponse>> GetAdminListAsync(
        AdminListUnboxingsRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new AdminUnboxingListFilter(
            NormalizeOptionalText(request.Keyword, 128),
            NormalizeOptionalCode(request.LevelCode, nameof(request.LevelCode)),
            request.IsFeatured,
            NormalizeAdminStatus(request.Status),
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetAdminListAsync(filter, cancellationToken);

        return new PagedResult<AdminUnboxingListItemResponse>(
            result.Items.Select(MapAdminListItem).ToArray(),
            filter.PageNumber,
            filter.PageSize,
            result.TotalCount);
    }

    public async Task<AdminUnboxingDetailResponse?> GetAdminDetailAsync(
        string postNo,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedPostNo = NormalizePostNo(postNo);
        var detail = await queryStore.GetAdminDetailAsync(normalizedPostNo, cancellationToken);
        return detail is null ? null : MapAdminDetail(detail);
    }

    public async Task<bool> DeleteAsync(
        string postNo,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedPostNo = NormalizePostNo(postNo);
        return await queryStore.DeleteAsync(normalizedPostNo, cancellationToken);
    }

    public async Task<SaveUnboxingResponse> CreateAsync(
        CreateUnboxingRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = NormalizePositiveId(request.UserId, nameof(request.UserId));
        if (!await queryStore.UserExistsAsync(userId, cancellationToken))
        {
            throw new ArgumentException("User does not exist.", nameof(request.UserId));
        }

        var title = NormalizeRequiredText(request.Title, nameof(request.Title), MaxShortTextLength);
        var contentText = NormalizeRequiredText(request.ContentText, nameof(request.ContentText));
        var level = await ResolveLevelAsync(request.LevelCode, cancellationToken);
        var postNo = await ResolveCreatePostNoAsync(request.PostNo, cancellationToken);

        var command = new UnboxingCreateCommandModel(
            userId,
            postNo,
            NormalizeOptionalText(request.AuthorName, MaxShortTextLength),
            NormalizeOptionalText(request.AuthorAvatarUrl, MaxMediumTextLength),
            NormalizeOptionalPositiveId(request.OrderItemId),
            NormalizeOptionalPositiveId(request.ProjectVersionId),
            NormalizeOptionalText(request.ProductLabel, 128),
            NormalizeBookTitle(request.BookTitle, title),
            level?.LevelCode,
            title,
            contentText,
            NormalizeStatus(request.Status),
            request.IsFeatured,
            NormalizePublishedAtUtc(request.PublishedAtUtc, request.Status),
            NormalizeMedia(request.Media),
            NormalizeTags(request.Tags));

        var result = await queryStore.CreateAsync(command, cancellationToken);
        return MapWriteResponse(result, command.LevelCode);
    }

    public async Task<SaveUnboxingResponse?> UpdateAsync(
        string postNo,
        UpdateUnboxingRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPostNo = NormalizePostNo(postNo);
        var title = NormalizeRequiredText(request.Title, nameof(request.Title), MaxShortTextLength);
        var contentText = NormalizeRequiredText(request.ContentText, nameof(request.ContentText));
        var level = await ResolveLevelAsync(request.LevelCode, cancellationToken);

        var command = new UnboxingUpdateCommandModel(
            NormalizeOptionalText(request.AuthorName, MaxShortTextLength),
            NormalizeOptionalText(request.AuthorAvatarUrl, MaxMediumTextLength),
            NormalizeOptionalPositiveId(request.OrderItemId),
            NormalizeOptionalPositiveId(request.ProjectVersionId),
            NormalizeOptionalText(request.ProductLabel, 128),
            NormalizeBookTitle(request.BookTitle, title),
            level?.LevelCode,
            title,
            contentText,
            NormalizeStatus(request.Status),
            request.IsFeatured,
            NormalizePublishedAtUtc(request.PublishedAtUtc, request.Status),
            NormalizeMedia(request.Media),
            NormalizeTags(request.Tags));

        var result = await queryStore.UpdateAsync(normalizedPostNo, command, cancellationToken);
        return result is null ? null : MapWriteResponse(result, command.LevelCode);
    }

    public async Task<PagedResult<UnboxingListItemResponse>> GetListAsync(
        ListUnboxingsRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new UnboxingListFilter(
            NormalizeOptionalCode(request.LevelCode, nameof(request.LevelCode)),
            request.IsFeatured,
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetListAsync(filter, cancellationToken);

        return new PagedResult<UnboxingListItemResponse>(
            result.Items.Select(MapListItem).ToArray(),
            filter.PageNumber,
            filter.PageSize,
            result.TotalCount);
    }

    public async Task<UnboxingDetailResponse?> GetDetailAsync(
        string postNo,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedPostNo = NormalizePostNo(postNo);
        var detail = await queryStore.GetDetailAsync(normalizedPostNo, cancellationToken);
        return detail is null ? null : MapDetail(detail);
    }

    public async Task<IReadOnlyCollection<UnboxingLevelResponse>> GetLevelsAsync(
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var levels = await queryStore.GetLevelsAsync(cancellationToken);
        return levels.Select(MapLevel).ToArray();
    }

    private static UnboxingListItemResponse MapListItem(UnboxingListItemQueryModel item)
    {
        return new UnboxingListItemResponse(
            item.PostId,
            item.PostNo,
            ResolveAuthorName(item.AuthorName),
            item.AuthorAvatarUrl,
            item.Title,
            item.BookTitle,
            BuildExcerpt(item.ContentText),
            item.ProductLabel,
            item.Level is null ? null : MapLevel(item.Level),
            item.CoverImageUrl,
            item.CoverThumbnailUrl,
            item.IsFeatured,
            item.PublishedAtUtc,
            item.Tags);
    }

    private static UnboxingDetailResponse MapDetail(UnboxingDetailQueryModel item)
    {
        return new UnboxingDetailResponse(
            item.PostId,
            item.PostNo,
            ResolveAuthorName(item.AuthorName),
            item.AuthorAvatarUrl,
            item.Title,
            item.BookTitle,
            item.ContentText,
            item.ProductLabel,
            item.Level is null ? null : MapLevel(item.Level),
            item.IsFeatured,
            item.PublishedAtUtc,
            item.Tags,
            item.Media.Select(media => new UnboxingMediaResponse(
                media.MediaType,
                media.StorageUrl,
                media.ThumbnailUrl,
                media.SortOrder,
                media.Width,
                media.Height)).ToArray());
    }

    private static AdminUnboxingListItemResponse MapAdminListItem(AdminUnboxingListItemQueryModel item)
    {
        return new AdminUnboxingListItemResponse(
            item.PostId,
            item.PostNo,
            item.UserId,
            ResolveAuthorName(item.AuthorName),
            item.AuthorAvatarUrl,
            item.Title,
            item.BookTitle,
            item.ProductLabel,
            item.Status,
            item.Level is null ? null : MapLevel(item.Level),
            item.IsFeatured,
            item.CoverImageUrl,
            item.CoverThumbnailUrl,
            item.PublishedAtUtc,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.Tags);
    }

    private static AdminUnboxingDetailResponse MapAdminDetail(AdminUnboxingDetailQueryModel item)
    {
        return new AdminUnboxingDetailResponse(
            item.PostId,
            item.PostNo,
            item.UserId,
            ResolveAuthorName(item.AuthorName),
            item.AuthorAvatarUrl,
            item.Title,
            item.BookTitle,
            item.ContentText,
            item.ProductLabel,
            item.Status,
            item.Level is null ? null : MapLevel(item.Level),
            item.IsFeatured,
            item.PublishedAtUtc,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.Tags,
            item.Media.Select(media => new UnboxingMediaResponse(
                media.MediaType,
                media.StorageUrl,
                media.ThumbnailUrl,
                media.SortOrder,
                media.Width,
                media.Height)).ToArray());
    }

    private static UnboxingLevelResponse MapLevel(UnboxingLevelModel level)
    {
        return new UnboxingLevelResponse(level.LevelCode, level.LevelName, level.IconUrl);
    }

    private static SaveUnboxingResponse MapWriteResponse(UnboxingWriteResultModel result, string? levelCode)
    {
        return new SaveUnboxingResponse(
            result.PostId,
            result.PostNo,
            result.UserId,
            result.Status,
            result.IsFeatured,
            levelCode,
            result.MediaCount,
            result.TagCount,
            result.PublishedAtUtc);
    }

    private static string ResolveAuthorName(string? authorName)
    {
        var normalized = authorName?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "匿名用户" : normalized;
    }

    private async Task<UnboxingLevelModel?> ResolveLevelAsync(string? levelCode, CancellationToken cancellationToken)
    {
        var normalizedLevelCode = NormalizeOptionalCode(levelCode, nameof(levelCode));
        if (normalizedLevelCode is null)
        {
            return null;
        }

        var level = await queryStore.GetLevelByCodeAsync(normalizedLevelCode, cancellationToken);
        if (level is null)
        {
            throw new ArgumentException("Level code does not exist.", nameof(levelCode));
        }

        return level;
    }

    private async Task<string> ResolveCreatePostNoAsync(string? postNo, CancellationToken cancellationToken)
    {
        var normalized = postNo?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            if (!PostNoPattern.IsMatch(normalized))
            {
                throw new ArgumentException("Post number format is invalid.", nameof(postNo));
            }

            if (await queryStore.PostNoExistsAsync(normalized, cancellationToken))
            {
                throw new ArgumentException("Post number already exists.", nameof(postNo));
            }

            return normalized;
        }

        var currentMax = await queryStore.GetCurrentMaxNumericPostNoAsync(cancellationToken);
        for (var attempt = 1; attempt <= 20; attempt++)
        {
            var candidate = Math.Max(currentMax + attempt, MinGeneratedPostNo).ToString(CultureInfo.InvariantCulture);
            if (!await queryStore.PostNoExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique post number.");
    }

    private static string? BuildExcerpt(string? contentText)
    {
        if (string.IsNullOrWhiteSpace(contentText))
        {
            return null;
        }

        var normalized = string.Join(' ', contentText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length <= ExcerptMaxLength)
        {
            return normalized;
        }

        return string.Concat(normalized.AsSpan(0, ExcerptMaxLength), "...");
    }

    private static string NormalizePostNo(string postNo)
    {
        var normalized = postNo?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Post number is required.", nameof(postNo));
        }

        if (!PostNoPattern.IsMatch(normalized))
        {
            throw new ArgumentException("Post number format is invalid.", nameof(postNo));
        }

        return normalized;
    }

    private static long NormalizePositiveId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Value must be greater than zero.", parameterName);
        }

        return value;
    }

    private static long? NormalizeOptionalPositiveId(long? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return null;
        }

        return value.Value;
    }

    private static int NormalizeStatus(int status)
    {
        return status is >= 0 and <= 3
            ? status
            : throw new ArgumentException("Status must be between 0 and 3.", nameof(status));
    }

    private static int? NormalizeAdminStatus(int? status)
    {
        if (!status.HasValue)
        {
            return null;
        }

        return NormalizeStatus(status.Value);
    }

    private static DateTime? NormalizePublishedAtUtc(DateTime? publishedAtUtc, int status)
    {
        if (status == 1)
        {
            return publishedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        }

        return publishedAtUtc?.ToUniversalTime();
    }

    private static string NormalizeRequiredText(string value, string parameterName, int? maxLength = null)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (maxLength.HasValue && normalized.Length > maxLength.Value)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength.Value}.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength}.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeBookTitle(string? bookTitle, string title)
    {
        return NormalizeOptionalText(bookTitle, MaxShortTextLength) ?? title;
    }

    private static IReadOnlyCollection<UnboxingWriteMediaCommandModel> NormalizeMedia(
        IReadOnlyCollection<WriteUnboxingMediaRequest>? media)
    {
        if (media is null || media.Count == 0)
        {
            throw new ArgumentException("At least one media item is required.", nameof(media));
        }

        var normalized = media
            .Select((item, index) => new
            {
                RequestedSort = item.SortOrder.GetValueOrDefault(index + 1),
                Index = index,
                MediaType = NormalizeMediaType(item.MediaType),
                StorageUrl = NormalizeRequiredText(item.StorageUrl, nameof(item.StorageUrl), 1024),
                ThumbnailUrl = NormalizeOptionalText(item.ThumbnailUrl, 1024),
                Width = NormalizeOptionalDimension(item.Width, nameof(item.Width)),
                Height = NormalizeOptionalDimension(item.Height, nameof(item.Height))
            })
            .OrderBy(item => item.RequestedSort)
            .ThenBy(item => item.Index)
            .Select((item, index) => new UnboxingWriteMediaCommandModel(
                item.MediaType,
                item.StorageUrl,
                item.ThumbnailUrl,
                index + 1,
                item.Width,
                item.Height))
            .ToArray();

        return normalized;
    }

    private static IReadOnlyCollection<string> NormalizeTags(IReadOnlyCollection<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = tags
            .Select(tag => NormalizeRequiredText(tag, nameof(tags), 64))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTagCount)
            .ToArray();

        return normalized;
    }

    private static string NormalizeMediaType(string mediaType)
    {
        var normalized = mediaType?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !MediaTypePattern.IsMatch(normalized))
        {
            throw new ArgumentException("Media type must be image or video.", nameof(mediaType));
        }

        return normalized;
    }

    private static int? NormalizeOptionalDimension(int? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        if (value.Value <= 0)
        {
            throw new ArgumentException("Value must be greater than zero.", parameterName);
        }

        return value.Value;
    }

    private static string? NormalizeOptionalCode(string? value, string parameterName)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (!CodePattern.IsMatch(normalized))
        {
            throw new ArgumentException("Value format is invalid.", parameterName);
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
}
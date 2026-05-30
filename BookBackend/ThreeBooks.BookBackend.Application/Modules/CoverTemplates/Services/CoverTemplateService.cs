using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Models;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Requests;
using ThreeBooks.BookBackend.Contracts.CoverTemplates.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.CoverTemplates.Services;

public sealed class CoverTemplateService(
    IAlbumTemplateQueryStore queryStore,
    IFileObjectStore objectStore,
    IFileMetadataStore metadataStore) : ICoverTemplateService
{
    private const string CoverPageType = "cover-template";
    private const string CoverSchemaVersion = "1.0";
    private const string DefaultGeneratedDirectory = "generated-covers";
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly Regex CodePattern = new("^[a-z0-9_-]{2,64}$", RegexOptions.Compiled);
    private static readonly Regex FieldCodePattern = new("^[a-z0-9_-]{1,64}$", RegexOptions.Compiled);
    private static readonly Regex ColorPattern = new("^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] FallbackFontNames =
    [
        "Microsoft YaHei",
        "SimSun",
        "Noto Sans CJK SC",
        "WenQuanYi Micro Hei",
        "Segoe UI",
        "Arial"
    ];

    public async Task<PagedResult<CoverTemplateListItemResponse>> GetListAsync(
        ListCoverTemplatesRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new AlbumTemplateListFilter(
            NormalizeKeyword(request.Keyword),
            null,
            CoverPageType,
            null,
            request.IsActive,
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetListAsync(filter, cancellationToken);

        return new PagedResult<CoverTemplateListItemResponse>(
            result.Items.Select(MapListItem).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<CoverTemplateDetailResponse?> GetDetailAsync(
        string templateCode,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var detail = await GetCoverTemplateDetailCoreAsync(templateCode, cancellationToken);
        return detail is null ? null : MapDetail(detail);
    }

    public async Task<CreateCoverTemplateResponse> CreateAsync(
        CreateCoverTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fields = NormalizeFieldDefinitions(request.Fields, nameof(request.Fields));
        var command = new AlbumTemplateCreateCommandModel(
            NormalizeRequiredCode(request.TemplateCode, nameof(request.TemplateCode), 64),
            NormalizeRequiredText(request.Name, nameof(request.Name), 128),
            NormalizeOptionalText(request.Description, 512, nameof(request.Description)),
            null,
            CoverPageType,
            null,
            null,
            CoverSchemaVersion,
            SerializeSchema(fields),
            NormalizeRequiredId(request.BackgroundFileId, nameof(request.BackgroundFileId)),
            NormalizeNullableId(request.CreatedByUserId, nameof(request.CreatedByUserId)),
            request.IsBuiltIn,
            request.IsActive,
            NormalizeSortOrder(request.SortOrder));

        var created = await queryStore.CreateAsync(command, cancellationToken);

        return new CreateCoverTemplateResponse(
            created.TemplateId,
            created.TemplateCode,
            created.IsActive,
            BuildOptionalProxyUrl(created.PreviewFile));
    }

    public async Task<CoverTemplateDetailResponse?> UpdateAsync(
        string templateCode,
        UpdateCoverTemplateRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fields = NormalizeFieldDefinitions(request.Fields, nameof(request.Fields));
        var normalizedTemplateCode = NormalizeRequiredCode(templateCode, nameof(templateCode), 64);
        var command = new AlbumTemplateUpdateCommandModel(
            NormalizeRequiredText(request.Name, nameof(request.Name), 128),
            NormalizeOptionalText(request.Description, 512, nameof(request.Description)),
            null,
            CoverPageType,
            null,
            null,
            CoverSchemaVersion,
            SerializeSchema(fields),
            NormalizeRequiredId(request.BackgroundFileId, nameof(request.BackgroundFileId)),
            NormalizeNullableId(request.CreatedByUserId, nameof(request.CreatedByUserId)),
            request.IsBuiltIn,
            request.IsActive,
            NormalizeSortOrder(request.SortOrder));

        var updated = await queryStore.UpdateAsync(normalizedTemplateCode, command, cancellationToken);
        if (updated is null || !IsCoverTemplate(updated.PageType))
        {
            return null;
        }

        return MapDetail(updated);
    }

    public async Task<GeneratedCoverImageResponse?> GenerateAsync(
        string templateCode,
        GenerateCoverImageRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var detail = await GetCoverTemplateDetailCoreAsync(templateCode, cancellationToken);
        if (detail is null || !detail.IsActive)
        {
            return null;
        }

        if (detail.PreviewFileId is null || detail.PreviewFile is null)
        {
            throw new ArgumentException("Cover template background image is required.", nameof(templateCode));
        }

        var fields = ParseSchema(detail.JsonSource, nameof(templateCode));
        var requestedValues = NormalizeFieldValues(request.FieldValues, nameof(request.FieldValues));
        var resolvedFields = ResolveFieldValues(fields, requestedValues);

        using var backgroundLease = await DownloadBackgroundAsync(detail.PreviewFile, cancellationToken);
        using var output = await RenderCoverAsync(backgroundLease.Content, resolvedFields, cancellationToken);

        var fileName = BuildOutputFileName(request.FileName, detail.TemplateCode);
        var bucket = NormalizeBucket(request.Bucket, objectStore.DefaultBucket);
        var objectKey = BuildObjectKey(request.Directory, fileName);
        var contentLength = output.Length;
        output.Position = 0;

        var stored = await objectStore.UploadAsync(
            new FileUploadCommand(
                bucket,
                objectKey,
                fileName,
                "image/png",
                contentLength,
                output),
            cancellationToken);

        long fileId;
        try
        {
            fileId = await metadataStore.SaveUploadAsync(stored, fileName, context, cancellationToken);
        }
        catch
        {
            await objectStore.DeleteAsync(stored.Bucket, stored.ObjectKey, cancellationToken);
            throw;
        }

        return new GeneratedCoverImageResponse(
            fileId,
            stored.Bucket,
            stored.ObjectKey,
            BuildProxyUrl(stored.Bucket, stored.ObjectKey));
    }

    private async Task<AlbumTemplateDetailQueryModel?> GetCoverTemplateDetailCoreAsync(
        string templateCode,
        CancellationToken cancellationToken)
    {
        var normalizedTemplateCode = NormalizeRequiredCode(templateCode, nameof(templateCode), 64);
        var detail = await queryStore.GetDetailAsync(normalizedTemplateCode, cancellationToken);
        return detail is null || !IsCoverTemplate(detail.PageType)
            ? null
            : detail;
    }

    private static bool IsCoverTemplate(string? pageType)
    {
        return string.Equals(pageType?.Trim(), CoverPageType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<DownloadedBackgroundLease> DownloadBackgroundAsync(
        AlbumTemplateStoredFileReference backgroundFile,
        CancellationToken cancellationToken)
    {
        var content = await objectStore.DownloadAsync(backgroundFile.Bucket, backgroundFile.ObjectKey, cancellationToken);
        if (content is null)
        {
            throw new ArgumentException("Cover template background image does not exist or is inactive.");
        }

        if (content.Content is null)
        {
            content.Lease?.Dispose();
            throw new ArgumentException("Cover template background image content is unavailable.");
        }

        return new DownloadedBackgroundLease(content.Content, content.Lease);
    }

    private static async Task<MemoryStream> RenderCoverAsync(
        Stream backgroundContent,
        IReadOnlyCollection<CoverTemplateResolvedFieldValueModel> fields,
        CancellationToken cancellationToken)
    {
        try
        {
            using var sourceBuffer = new MemoryStream();
            await backgroundContent.CopyToAsync(sourceBuffer, cancellationToken);
            sourceBuffer.Position = 0;

            using var bitmap = SKBitmap.Decode(sourceBuffer);
            if (bitmap is null)
            {
                throw new ArgumentException(
                    "Cover template background file must be a supported raster image format such as PNG, JPEG, GIF, BMP, or WebP.",
                    nameof(backgroundContent));
            }

            using var canvas = new SKCanvas(bitmap);

            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field.Value))
                {
                    continue;
                }

                using var typeface = CreateTypeface(field.Definition.FontFamily!);
                using var font = CreateFont(typeface, field.Definition.FontSize);
                using var paint = CreateTextPaint(ParseColor(field.Definition.FontColor!));
                DrawTextInField(canvas, font, paint, field.Definition, field.Value);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            var output = new MemoryStream();
            encoded.SaveTo(output);
            output.Position = 0;
            return output;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ArgumentException(
                "Cover template background file must be a supported raster image format such as PNG, JPEG, GIF, BMP, or WebP.",
                nameof(backgroundContent),
                exception);
        }
    }

    private static SKFont CreateFont(SKTypeface typeface, float fontSize)
    {
        return new SKFont(typeface, fontSize)
        {
            Edging = SKFontEdging.Antialias,
            Subpixel = true
        };
    }

    private static SKPaint CreateTextPaint(SKColor color)
    {
        return new SKPaint
        {
            Color = color,
            IsAntialias = true,
            IsStroke = false
        };
    }

    private static void DrawTextInField(
        SKCanvas canvas,
        SKFont font,
        SKPaint paint,
        CoverTemplateFieldDefinitionModel field,
        string text)
    {
        var lines = WrapText(text, font, paint, field.Width);
        if (lines.Count == 0)
        {
            return;
        }

        var metrics = font.Metrics;
        var lineHeight = MathF.Max(
            field.FontSize,
            (metrics.Descent - metrics.Ascent) + metrics.Leading);
        var maxLines = Math.Max(1, (int)MathF.Floor(field.Height / lineHeight));

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToArray();
        }

        var contentHeight = lines.Count * lineHeight;
        var top = field.Y + ResolveVerticalOffset(field.Height, contentHeight, field.VerticalAlignment);
        var baseline = top - metrics.Ascent;

        foreach (var line in lines)
        {
            var lineWidth = font.MeasureText(line, paint);
            var drawX = ResolveLineX(field.X, field.Width, lineWidth, field.HorizontalAlignment);
            canvas.DrawText(line, drawX, baseline, SKTextAlign.Left, font, paint);
            baseline += lineHeight;
        }
    }

    private static IReadOnlyList<string> WrapText(string text, SKFont font, SKPaint paint, float maxWidth)
    {
        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (normalizedText.Length == 0)
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>();
        foreach (var paragraph in normalizedText.Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            AppendWrappedParagraph(lines, paragraph, font, paint, maxWidth);
        }

        return lines;
    }

    private static void AppendWrappedParagraph(
        ICollection<string> lines,
        string paragraph,
        SKFont font,
        SKPaint paint,
        float maxWidth)
    {
        var start = 0;
        while (start < paragraph.Length)
        {
            var bestLength = 0;
            var bestWhitespaceLength = 0;

            for (var length = 1; start + length <= paragraph.Length; length++)
            {
                var candidate = paragraph.Substring(start, length);
                var measuredWidth = font.MeasureText(candidate, paint);
                if (measuredWidth > maxWidth && bestLength > 0)
                {
                    break;
                }

                if (measuredWidth > maxWidth)
                {
                    bestLength = 1;
                    break;
                }

                bestLength = length;
                if (char.IsWhiteSpace(paragraph[start + length - 1]))
                {
                    bestWhitespaceLength = length;
                }
            }

            if (bestLength <= 0)
            {
                bestLength = 1;
            }

            var segmentLength = bestWhitespaceLength > 0 && bestWhitespaceLength < bestLength
                ? bestWhitespaceLength
                : bestLength;
            var segment = paragraph.Substring(start, segmentLength).TrimEnd();
            if (segment.Length == 0)
            {
                segment = paragraph.Substring(start, Math.Max(1, bestLength));
                segmentLength = segment.Length;
            }

            lines.Add(segment);
            start += segmentLength;

            while (start < paragraph.Length && char.IsWhiteSpace(paragraph[start]))
            {
                start++;
            }
        }
    }

    private static float ResolveLineX(float x, float width, float lineWidth, string? alignment)
    {
        return NormalizeAlignment(alignment, nameof(alignment), "left", "center", "right") switch
        {
            "center" => x + MathF.Max(0, (width - lineWidth) / 2F),
            "right" => x + MathF.Max(0, width - lineWidth),
            _ => x
        };
    }

    private static float ResolveVerticalOffset(float height, float contentHeight, string? alignment)
    {
        var remaining = MathF.Max(0, height - contentHeight);
        return NormalizeAlignment(alignment, nameof(alignment), "top", "center", "bottom") switch
        {
            "center" => remaining / 2F,
            "bottom" => remaining,
            _ => 0F
        };
    }

    private static string NormalizeAlignment(
        string? value,
        string parameterName,
        params string[] allowedValues)
    {
        var normalized = NormalizeRequiredCode(value, parameterName, 16);
        if (!allowedValues.Contains(normalized, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Value must be one of: {string.Join(", ", allowedValues)}.",
                parameterName);
        }

        return normalized;
    }

    private static SKColor ParseColor(string colorHex)
    {
        var hex = colorHex.Trim().TrimStart('#');
        return hex.Length switch
        {
            6 => new SKColor(
                ParseHexByte(hex, 0),
                ParseHexByte(hex, 2),
                ParseHexByte(hex, 4),
                255),
            8 => new SKColor(
                ParseHexByte(hex, 0),
                ParseHexByte(hex, 2),
                ParseHexByte(hex, 4),
                ParseHexByte(hex, 6)),
            _ => throw new ArgumentException("Value must be a hex color in #RRGGBB or #RRGGBBAA format.", nameof(colorHex))
        };
    }

    private static byte ParseHexByte(string hex, int startIndex)
    {
        return byte.Parse(hex.AsSpan(startIndex, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static SKTypeface CreateTypeface(string fontFamily)
    {
        var primary = SKTypeface.FromFamilyName(fontFamily.Trim());
        if (primary is not null)
        {
            return primary;
        }

        foreach (var fallback in FallbackFontNames)
        {
            var fallbackTypeface = SKTypeface.FromFamilyName(fallback);
            if (fallbackTypeface is not null)
            {
                return fallbackTypeface;
            }
        }

        return SKTypeface.Default;
    }

    private static IReadOnlyCollection<CoverTemplateResolvedFieldValueModel> ResolveFieldValues(
        IReadOnlyCollection<CoverTemplateFieldDefinitionModel> fields,
        IReadOnlyDictionary<string, string?> requestedValues)
    {
        var knownIds = fields.Select(item => item.FieldId!).ToHashSet(StringComparer.Ordinal);
        var unknownIds = requestedValues.Keys
            .Where(fieldId => !knownIds.Contains(fieldId))
            .OrderBy(fieldId => fieldId, StringComparer.Ordinal)
            .ToArray();

        if (unknownIds.Length > 0)
        {
            throw new ArgumentException(
                $"The following field ids are not defined by the template: {string.Join(", ", unknownIds)}.",
                nameof(requestedValues));
        }

        return fields
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.FieldId, StringComparer.Ordinal)
            .Select(field =>
            {
                requestedValues.TryGetValue(field.FieldId!, out var value);
                var resolvedValue = value ?? field.DefaultValue ?? string.Empty;

                if (field.MaxLength.HasValue && resolvedValue.Length > field.MaxLength.Value)
                {
                    throw new ArgumentException(
                        $"Field '{field.FieldId}' length cannot exceed {field.MaxLength.Value} characters.",
                        nameof(requestedValues));
                }

                if (field.IsRequired && string.IsNullOrWhiteSpace(resolvedValue))
                {
                    throw new ArgumentException(
                        $"Field '{field.FieldId}' is required.",
                        nameof(requestedValues));
                }

                return new CoverTemplateResolvedFieldValueModel(field, resolvedValue);
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string?> NormalizeFieldValues(
        IReadOnlyCollection<CoverTemplateFieldValueRequest> fieldValues,
        string parameterName)
    {
        if (fieldValues is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var dictionary = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var item in fieldValues)
        {
            if (item is null)
            {
                throw new ArgumentException("Field values cannot contain null items.", parameterName);
            }

            var fieldId = NormalizeFieldId(item.FieldId, nameof(item.FieldId));
            if (!dictionary.TryAdd(fieldId, NormalizeOptionalMultilineText(item.Value, 2000)))
            {
                throw new ArgumentException($"Duplicate field id '{fieldId}' is not allowed.", parameterName);
            }
        }

        return dictionary;
    }

    private static string SerializeSchema(IReadOnlyCollection<CoverTemplateFieldDefinitionModel> fields)
    {
        return JsonSerializer.Serialize(
            new CoverTemplateSchemaDocument(CoverSchemaVersion, fields),
            SerializerOptions);
    }

    private static IReadOnlyCollection<CoverTemplateFieldDefinitionModel> ParseSchema(
        string jsonSource,
        string parameterName)
    {
        try
        {
            var document = JsonSerializer.Deserialize<CoverTemplateSchemaDocument>(jsonSource, SerializerOptions)
                ?? throw new ArgumentException("Cover template schema cannot be empty.", parameterName);

            return NormalizeFieldDefinitions(document.Fields ?? Array.Empty<CoverTemplateFieldDefinitionModel>(), parameterName);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"Cover template schema is invalid JSON: {exception.Message}", parameterName, exception);
        }
    }

    private static IReadOnlyCollection<CoverTemplateFieldDefinitionModel> NormalizeFieldDefinitions(
        IReadOnlyCollection<CoverTemplateFieldDefinitionRequest> fields,
        string parameterName)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalizedFields = fields
            .Select((item, index) => NormalizeFieldDefinition(item, index, parameterName))
            .ToArray();

        EnsureDistinctFieldIds(normalizedFields, parameterName);

        return normalizedFields
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.FieldId, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyCollection<CoverTemplateFieldDefinitionModel> NormalizeFieldDefinitions(
        IReadOnlyCollection<CoverTemplateFieldDefinitionModel> fields,
        string parameterName)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var normalizedFields = fields
            .Select((item, index) => NormalizeFieldDefinition(item, index, parameterName))
            .ToArray();

        EnsureDistinctFieldIds(normalizedFields, parameterName);

        return normalizedFields
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.FieldId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void EnsureDistinctFieldIds(
        IReadOnlyCollection<CoverTemplateFieldDefinitionModel> fields,
        string parameterName)
    {
        var duplicateFieldIds = fields
            .GroupBy(item => item.FieldId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        if (duplicateFieldIds.Length > 0)
        {
            throw new ArgumentException(
                $"Duplicate field ids are not allowed: {string.Join(", ", duplicateFieldIds)}.",
                parameterName);
        }
    }

    private static CoverTemplateFieldDefinitionModel NormalizeFieldDefinition(
        CoverTemplateFieldDefinitionRequest item,
        int index,
        string parameterName)
    {
        if (item is null)
        {
            throw new ArgumentException("Fields cannot contain null items.", parameterName);
        }

        return CreateNormalizedField(
            item.FieldId,
            item.DisplayName,
            item.Placeholder,
            item.X,
            item.Y,
            item.Width,
            item.Height,
            item.FontFamily,
            item.FontSize,
            item.FontColor,
            item.IsRequired,
            item.SortOrder,
            item.DefaultValue,
            item.HorizontalAlignment,
            item.VerticalAlignment,
            item.MaxLength,
            index,
            parameterName);
    }

    private static CoverTemplateFieldDefinitionModel NormalizeFieldDefinition(
        CoverTemplateFieldDefinitionModel item,
        int index,
        string parameterName)
    {
        if (item is null)
        {
            throw new ArgumentException("Fields cannot contain null items.", parameterName);
        }

        return CreateNormalizedField(
            item.FieldId,
            item.DisplayName,
            item.Placeholder,
            item.X,
            item.Y,
            item.Width,
            item.Height,
            item.FontFamily,
            item.FontSize,
            item.FontColor,
            item.IsRequired,
            item.SortOrder,
            item.DefaultValue,
            item.HorizontalAlignment,
            item.VerticalAlignment,
            item.MaxLength,
            index,
            parameterName);
    }

    private static CoverTemplateFieldDefinitionModel CreateNormalizedField(
        string? fieldId,
        string? displayName,
        string? placeholder,
        float x,
        float y,
        float width,
        float height,
        string? fontFamily,
        float fontSize,
        string? fontColor,
        bool isRequired,
        int sortOrder,
        string? defaultValue,
        string? horizontalAlignment,
        string? verticalAlignment,
        int? maxLength,
        int index,
        string parameterName)
    {
        var normalizedField = new CoverTemplateFieldDefinitionModel(
            NormalizeFieldId(fieldId, $"{parameterName}[{index}].FieldId"),
            NormalizeRequiredText(displayName, $"{parameterName}[{index}].DisplayName", 64),
            NormalizeOptionalText(placeholder, 128, $"{parameterName}[{index}].Placeholder"),
            NormalizeCoordinate(x, $"{parameterName}[{index}].X"),
            NormalizeCoordinate(y, $"{parameterName}[{index}].Y"),
            NormalizeDimension(width, $"{parameterName}[{index}].Width"),
            NormalizeDimension(height, $"{parameterName}[{index}].Height"),
            NormalizeRequiredText(fontFamily, $"{parameterName}[{index}].FontFamily", 128),
            NormalizeFontSize(fontSize, $"{parameterName}[{index}].FontSize"),
            NormalizeColor(fontColor, $"{parameterName}[{index}].FontColor"),
            isRequired,
            NormalizeSortOrder(sortOrder),
            NormalizeOptionalMultilineText(defaultValue, 2000),
            NormalizeAlignment(horizontalAlignment, $"{parameterName}[{index}].HorizontalAlignment", "left", "center", "right"),
            NormalizeAlignment(verticalAlignment, $"{parameterName}[{index}].VerticalAlignment", "top", "center", "bottom"),
            NormalizeNullableLength(maxLength, $"{parameterName}[{index}].MaxLength"));

        return normalizedField;
    }

    private static int? NormalizeNullableLength(int? length, string parameterName)
    {
        if (!length.HasValue)
        {
            return null;
        }

        if (length.Value < 1 || length.Value > 2000)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be between 1 and 2000.");
        }

        return length.Value;
    }

    private static float NormalizeCoordinate(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be a non-negative number.");
        }

        return value;
    }

    private static float NormalizeDimension(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than 0.");
        }

        return value;
    }

    private static float NormalizeFontSize(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0 || value > 512)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be between 0 and 512.");
        }

        return value;
    }

    private static string NormalizeFieldId(string? value, string parameterName)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (!FieldCodePattern.IsMatch(normalized))
        {
            throw new ArgumentException("Value format is invalid.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeColor(string? value, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (!ColorPattern.IsMatch(normalized))
        {
            throw new ArgumentException("Value must be a hex color in #RRGGBB or #RRGGBBAA format.", parameterName);
        }

        return normalized.ToUpperInvariant();
    }

    private static string BuildOutputFileName(string? requestedFileName, string templateCode)
    {
        var candidate = string.IsNullOrWhiteSpace(requestedFileName)
            ? $"{templateCode}-cover.png"
            : Path.GetFileName(requestedFileName.Trim());

        var baseName = Path.GetFileNameWithoutExtension(candidate);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = $"{templateCode}-cover";
        }

        return $"{baseName}.png";
    }

    private static string BuildObjectKey(string? directory, string fileName)
    {
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        var safeFileName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "cover";
        }

        var generatedFileName = $"{safeFileName}-{Guid.NewGuid():N}.png";
        var prefix = string.IsNullOrWhiteSpace(directory)
            ? DefaultGeneratedDirectory
            : NormalizeObjectKey(directory);

        return $"{prefix}/{datePath}/{generatedFileName}";
    }

    private static string NormalizeBucket(string? bucket, string? defaultBucket)
    {
        var normalized = string.IsNullOrWhiteSpace(bucket)
            ? defaultBucket?.Trim().ToLowerInvariant()
            : bucket.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Bucket is required. Provide bucket explicitly or configure ObjectStorage:DefaultBucket.", nameof(bucket));
        }

        return normalized;
    }

    private static string NormalizeObjectKey(string objectKey)
    {
        var normalized = objectKey.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("ObjectKey is required.", nameof(objectKey));
        }

        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("ObjectKey cannot contain '.' or '..' segments.", nameof(objectKey));
        }

        return string.Join('/', segments);
    }

    private static CoverTemplateListItemResponse MapListItem(AlbumTemplateListItemQueryModel item)
    {
        return new CoverTemplateListItemResponse(
            item.TemplateId,
            item.TemplateCode,
            item.Name,
            item.Description,
            BuildOptionalProxyUrl(item.PreviewFile),
            item.IsBuiltIn,
            item.IsActive,
            item.SortOrder,
            item.UpdatedAtUtc);
    }

    private static CoverTemplateDetailResponse MapDetail(AlbumTemplateDetailQueryModel item)
    {
        var fields = ParseSchema(item.JsonSource, nameof(item.JsonSource));

        return new CoverTemplateDetailResponse(
            item.TemplateId,
            item.TemplateCode,
            item.Name,
            item.Description,
            ResolveRequiredBackgroundFileId(item.PreviewFileId),
            BuildOptionalProxyUrl(item.PreviewFile),
            fields.Select(MapField).ToArray(),
            item.CreatedByUserId,
            item.IsBuiltIn,
            item.IsActive,
            item.SortOrder,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    private static long ResolveRequiredBackgroundFileId(long? previewFileId)
    {
        if (!previewFileId.HasValue || previewFileId.Value <= 0)
        {
            throw new ArgumentException("Cover template background image is required.", nameof(previewFileId));
        }

        return previewFileId.Value;
    }

    private static CoverTemplateFieldResponse MapField(CoverTemplateFieldDefinitionModel item)
    {
        return new CoverTemplateFieldResponse(
            item.FieldId!,
            item.DisplayName!,
            item.Placeholder,
            item.X,
            item.Y,
            item.Width,
            item.Height,
            item.FontFamily!,
            item.FontSize,
            item.FontColor!,
            item.IsRequired,
            item.SortOrder,
            item.DefaultValue,
            item.HorizontalAlignment!,
            item.VerticalAlignment!,
            item.MaxLength);
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

    private static string? NormalizeOptionalMultilineText(string? value, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.");
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

    private static long NormalizeRequiredId(long value, string parameterName)
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

    private sealed class DownloadedBackgroundLease(Stream content, IDisposable? lease) : IDisposable
    {
        public Stream Content { get; } = content;

        public void Dispose()
        {
            Content.Dispose();
            lease?.Dispose();
        }
    }
}
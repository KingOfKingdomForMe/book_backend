using System.Text.Json;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Albums.Models;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Albums;

public sealed class AlbumQueryStore(string connectionString) : IAlbumQueryStore
{
    private const string FindProductIdSql = """
        SELECT id
        FROM catalog_product_spu
        WHERE spu_code = @ProductCode
          AND is_active = 1
        LIMIT 1;
        """;

    private const string FindProjectByShareCodeSql = """
        SELECT id
        FROM book_project
        WHERE share_code = @ShareCode
        LIMIT 1;
        """;

    private const string InsertProjectSql = """
        INSERT INTO book_project (
            user_id,
            book_type,
            title,
            subtitle,
            status,
            spu_id,
            page_count,
            image_count,
            is_gift,
            gift_target_name,
            share_code,
            is_public,
            shared_version_id,
            shared_at,
            view_count,
            share_count)
        VALUES (
            @UserId,
            @BookType,
            @Title,
            @Subtitle,
            @Status,
            @SpuId,
            0,
            0,
            0,
            NULL,
            @ShareCode,
            @IsPublic,
            NULL,
            @SharedAt,
            0,
            0);
        """;

    private const string InsertVersionSql = """
        INSERT INTO book_project_version (
            project_id,
            version_no,
            snapshot_data,
            page_count,
            image_count,
            render_version,
            frozen_at)
        VALUES (
            @ProjectId,
            @VersionNo,
            @SnapshotData,
            0,
            0,
            @RenderVersion,
            UTC_TIMESTAMP());
        """;

    private const string UpdateProjectSharedVersionSql = """
        UPDATE book_project
        SET shared_version_id = @VersionId,
            shared_at = @SharedAt,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @ProjectId;
        """;

    private const string FindProjectSql = """
        SELECT
            p.id AS ProjectId,
            p.share_code AS ShareCode,
            p.title AS Title,
            p.subtitle AS Subtitle,
            p.book_type AS BookType,
            spu.spu_code AS ProductCode,
            p.page_count AS PageCount,
            p.view_count AS ViewCount,
            p.share_count AS ShareCount,
            p.shared_version_id AS SharedVersionId
        FROM book_project p
        LEFT JOIN catalog_product_spu spu ON spu.id = p.spu_id AND spu.is_active = 1
        WHERE p.share_code = @ShareCode
          AND p.is_public = 1
          AND p.shared_version_id IS NOT NULL
        LIMIT 1;
        """;

    private const string FindPagesSql = """
        SELECT
            vp.page_no AS PageNo,
            vp.page_label AS PageLabel,
            vp.page_type AS PageType,
            vp.json_source AS JsonSource,
            vp.json_file_id AS JsonFileId,
            vp.json_bucket AS JsonBucket,
            vp.json_object_key AS JsonObjectKey,
            vp.html_file_id AS HtmlFileId,
            vp.html_bucket AS HtmlBucket,
            vp.html_object_key AS HtmlObjectKey,
            vp.thumbnail_file_id AS ThumbnailFileId,
            vp.thumbnail_bucket AS ThumbnailBucket,
            vp.thumbnail_object_key AS ThumbnailObjectKey,
            CASE WHEN image_stats.asset_count > 0 THEN TRUE ELSE FALSE END AS HasImages
        FROM book_project_version_page vp
        LEFT JOIN (
            SELECT
                version_page_id,
                COUNT(*) AS asset_count
            FROM book_project_version_page_asset
            GROUP BY version_page_id
        ) image_stats ON image_stats.version_page_id = vp.id
        WHERE vp.project_version_id = @ProjectVersionId
        ORDER BY vp.sort_order, vp.page_no;
        """;

    private const string FindPageSql = """
        SELECT
            vp.id AS VersionPageId,
            vp.page_no AS PageNo,
            vp.page_label AS PageLabel,
            vp.page_type AS PageType,
            vp.schema_version AS SchemaVersion,
            vp.json_source AS JsonSource,
            vp.json_file_id AS JsonFileId,
            vp.json_bucket AS JsonBucket,
            vp.json_object_key AS JsonObjectKey,
            vp.html_file_id AS HtmlFileId,
            vp.html_bucket AS HtmlBucket,
            vp.html_object_key AS HtmlObjectKey
        FROM book_project p
        INNER JOIN book_project_version_page vp ON vp.project_version_id = p.shared_version_id
        WHERE p.share_code = @ShareCode
          AND p.is_public = 1
          AND vp.page_no = @PageNo
        LIMIT 1;
        """;

    private const string FindPageAssetsSql = """
        SELECT
            a.sort_order AS SortOrder,
            a.role AS Role,
            a.file_id AS FileId,
            a.bucket_name AS Bucket,
            a.object_key AS ObjectKey,
            a.width AS Width,
            a.height AS Height,
            a.alt_text AS AltText,
            a.caption AS Caption
        FROM book_project_version_page_asset a
        WHERE a.version_page_id = @VersionPageId
        ORDER BY a.sort_order, a.id;
        """;

    private const string FindProjectForMutationSql = """
        SELECT id AS ProjectId
        FROM book_project
        WHERE share_code = @ShareCode
          AND is_public = 1
        LIMIT 1;
        """;

    private const string FindProjectForWriteSql = """
        SELECT
            p.id AS ProjectId,
            p.share_code AS ShareCode,
            p.shared_version_id AS SharedVersionId,
            p.is_public AS IsPublic
        FROM book_project p
        WHERE p.id = @ProjectId
        LIMIT 1;
        """;

    private const string FindExistingVersionPageSql = """
        SELECT id
        FROM book_project_version_page
        WHERE project_version_id = @ProjectVersionId
          AND page_no = @PageNo
        LIMIT 1;
        """;

    private const string FindActiveFilesSql = """
        SELECT
            id AS FileId,
            bucket_name AS Bucket,
            object_key AS ObjectKey
        FROM storage_file_object
        WHERE id IN @FileIds
          AND storage_status = 1;
        """;

    private const string InsertVersionPageSql = """
        INSERT INTO book_project_version_page (
            project_version_id,
            page_no,
            page_label,
            page_type,
            sort_order,
            json_source,
            json_file_id,
            json_bucket,
            json_object_key,
            html_file_id,
            html_bucket,
            html_object_key,
            thumbnail_file_id,
            thumbnail_bucket,
            thumbnail_object_key,
            page_width,
            page_height,
            schema_version)
        VALUES (
            @ProjectVersionId,
            @PageNo,
            @PageLabel,
            @PageType,
            @SortOrder,
            @JsonSource,
            @JsonFileId,
            @JsonBucket,
            @JsonObjectKey,
            @HtmlFileId,
            @HtmlBucket,
            @HtmlObjectKey,
            @ThumbnailFileId,
            @ThumbnailBucket,
            @ThumbnailObjectKey,
            @PageWidth,
            @PageHeight,
            @SchemaVersion);
        """;

    private const string InsertVersionPageAssetSql = """
        INSERT INTO book_project_version_page_asset (
            version_page_id,
            sort_order,
            role,
            file_id,
            bucket_name,
            object_key,
            width,
            height,
            alt_text,
            caption,
            crop_json,
            created_at)
        VALUES (
            @VersionPageId,
            @SortOrder,
            @Role,
            @FileId,
            @BucketName,
            @ObjectKey,
            @Width,
            @Height,
            @AltText,
            @Caption,
            @CropJson,
            UTC_TIMESTAMP());
        """;

    private const string FindVersionStatsSql = """
        SELECT
            (SELECT COUNT(*)
             FROM book_project_version_page
             WHERE project_version_id = @ProjectVersionId) AS PageCount,
            (SELECT COUNT(*)
             FROM book_project_version_page_asset asset
             INNER JOIN book_project_version_page page ON page.id = asset.version_page_id
             WHERE page.project_version_id = @ProjectVersionId) AS ImageCount;
        """;

    private const string FindSnapshotPagesSql = """
        SELECT
            page_no AS PageNo,
            page_label AS PageLabel,
            page_type AS PageType
        FROM book_project_version_page
        WHERE project_version_id = @ProjectVersionId
        ORDER BY sort_order, page_no;
        """;

    private const string UpdateProjectVersionSnapshotSql = """
        UPDATE book_project_version
        SET snapshot_data = @SnapshotData,
            page_count = @PageCount,
            image_count = @ImageCount
        WHERE id = @ProjectVersionId;
        """;

    private const string UpdateProjectCountsSql = """
        UPDATE book_project
        SET page_count = @PageCount,
            image_count = @ImageCount,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @ProjectId;
        """;

    private const string InsertViewLogSql = """
        INSERT INTO book_project_view_log (
            project_id,
            share_code,
            page_no,
            client_ip,
            client_user_agent,
            created_at)
        VALUES (
            @ProjectId,
            @ShareCode,
            @PageNo,
            @ClientIp,
            @ClientUserAgent,
            UTC_TIMESTAMP());
        """;

    private const string InsertShareLogSql = """
        INSERT INTO book_project_share_log (
            project_id,
            share_code,
            channel,
            client_ip,
            client_user_agent,
            created_at)
        VALUES (
            @ProjectId,
            @ShareCode,
            @Channel,
            @ClientIp,
            @ClientUserAgent,
            UTC_TIMESTAMP());
        """;

    private const string IncrementViewCountSql = """
        UPDATE book_project
        SET view_count = view_count + 1,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @ProjectId;
        """;

    private const string IncrementShareCountSql = """
        UPDATE book_project
        SET share_count = share_count + 1,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @ProjectId;
        """;

    private const string GetLastInsertIdSql = """
        SELECT LAST_INSERT_ID();
        """;

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Album database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<bool> ShareCodeExistsAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var existingProjectId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProjectByShareCodeSql,
                new { ShareCode = shareCode },
                cancellationToken: cancellationToken));

        return existingProjectId.HasValue;
    }

    public async Task<AlbumCreateResultModel> CreateAlbumAsync(
        AlbumCreateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var existingProjectId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProjectByShareCodeSql,
                new { ShareCode = command.ShareCode },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (existingProjectId.HasValue)
        {
            throw new ArgumentException("Share code already exists.", nameof(command.ShareCode));
        }

        var spuId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProductIdSql,
                new { ProductCode = command.ProductCode },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!spuId.HasValue)
        {
            throw new ArgumentException("Product code does not exist or is inactive.", nameof(command.ProductCode));
        }

        var sharedAt = command.IsPublic ? DateTime.UtcNow : (DateTime?)null;

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertProjectSql,
                new
                {
                    command.UserId,
                    command.BookType,
                    command.Title,
                    command.Subtitle,
                    command.Status,
                    SpuId = spuId.Value,
                    command.ShareCode,
                    IsPublic = command.IsPublic,
                    SharedAt = sharedAt
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var projectId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        var snapshotData = BuildSnapshotData(command.SnapshotSchemaVersion, command.ShareCode, Array.Empty<SnapshotPageRow>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertVersionSql,
                new
                {
                    ProjectId = projectId,
                    command.VersionNo,
                    SnapshotData = snapshotData,
                    RenderVersion = NormalizeNullable(command.RenderVersion)
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var versionId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectSharedVersionSql,
                new
                {
                    ProjectId = projectId,
                    VersionId = versionId,
                    SharedAt = sharedAt
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new AlbumCreateResultModel(
            projectId,
            versionId,
            command.ShareCode,
            command.Title,
            command.Subtitle,
            command.BookType,
            command.ProductCode,
            command.IsPublic,
            0,
            0);
    }

    public async Task<AlbumPageWriteResultModel?> AddPageAsync(
        long projectId,
        AlbumPageWriteCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var projectRow = await connection.QuerySingleOrDefaultAsync<ProjectWriteRow>(
            new CommandDefinition(
                FindProjectForWriteSql,
                new { ProjectId = projectId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (projectRow is null || !projectRow.SharedVersionId.HasValue)
        {
            return null;
        }

        var existingPageId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindExistingVersionPageSql,
                new
                {
                    ProjectVersionId = projectRow.SharedVersionId.Value,
                    command.PageNo
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (existingPageId.HasValue)
        {
            throw new ArgumentException("Page number already exists in the album.", nameof(command.PageNo));
        }

        var fileIds = new HashSet<long>();
        if (command.HtmlFileId.HasValue)
        {
            fileIds.Add(command.HtmlFileId.Value);
        }

        if (command.ThumbnailFileId.HasValue)
        {
            fileIds.Add(command.ThumbnailFileId.Value);
        }

        foreach (var image in command.Images)
        {
            fileIds.Add(image.FileId);
        }

        var resolvedFiles = fileIds.Count == 0
            ? new Dictionary<long, FileReferenceRow>()
            : (await connection.QueryAsync<FileReferenceRow>(
                new CommandDefinition(
                    FindActiveFilesSql,
                    new { FileIds = fileIds.ToArray() },
                    transaction: transaction,
                    cancellationToken: cancellationToken)))
                .ToDictionary(item => item.FileId);

        var missingFileIds = fileIds
            .Where(fileId => !resolvedFiles.ContainsKey(fileId))
            .OrderBy(fileId => fileId)
            .ToArray();

        if (missingFileIds.Length > 0)
        {
            throw new ArgumentException(
                $"The following file ids do not exist or are inactive: {string.Join(", ", missingFileIds)}.",
                nameof(command));
        }

        var htmlFile = command.HtmlFileId.HasValue ? resolvedFiles[command.HtmlFileId.Value] : null;
        var thumbnailFile = command.ThumbnailFileId.HasValue ? resolvedFiles[command.ThumbnailFileId.Value] : null;

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertVersionPageSql,
                new
                {
                    ProjectVersionId = projectRow.SharedVersionId.Value,
                    command.PageNo,
                    command.PageLabel,
                    command.PageType,
                    command.SortOrder,
                    JsonSource = command.JsonSource,
                    JsonFileId = (long?)null,
                    JsonBucket = (string?)null,
                    JsonObjectKey = (string?)null,
                    HtmlFileId = htmlFile?.FileId,
                    HtmlBucket = htmlFile?.Bucket,
                    HtmlObjectKey = htmlFile?.ObjectKey,
                    ThumbnailFileId = thumbnailFile?.FileId,
                    ThumbnailBucket = thumbnailFile?.Bucket,
                    ThumbnailObjectKey = thumbnailFile?.ObjectKey,
                    command.PageWidth,
                    command.PageHeight,
                    command.SchemaVersion
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var versionPageId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        foreach (var image in command.Images.OrderBy(item => item.SortOrder))
        {
            var imageFile = resolvedFiles[image.FileId];

            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertVersionPageAssetSql,
                    new
                    {
                        VersionPageId = versionPageId,
                        image.SortOrder,
                        image.Role,
                        FileId = imageFile.FileId,
                        BucketName = imageFile.Bucket,
                        ObjectKey = imageFile.ObjectKey,
                        image.Width,
                        image.Height,
                        AltText = NormalizeNullable(image.AltText),
                        Caption = NormalizeNullable(image.Caption),
                        CropJson = NormalizeNullable(image.CropJson)
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }

        var stats = await connection.QuerySingleAsync<VersionStatsRow>(
            new CommandDefinition(
                FindVersionStatsSql,
                new { ProjectVersionId = projectRow.SharedVersionId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var pageCount = checked((int)stats.PageCount);
        var imageCount = checked((int)stats.ImageCount);

        var snapshotPages = (await connection.QueryAsync<SnapshotPageRow>(
            new CommandDefinition(
                FindSnapshotPagesSql,
                new { ProjectVersionId = projectRow.SharedVersionId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken)))
            .ToArray();

        var snapshotData = BuildSnapshotData(command.SchemaVersion, projectRow.ShareCode, snapshotPages);

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectVersionSnapshotSql,
                new
                {
                    ProjectVersionId = projectRow.SharedVersionId.Value,
                    PageCount = pageCount,
                    ImageCount = imageCount,
                    SnapshotData = snapshotData
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectCountsSql,
                new
                {
                    ProjectId = projectRow.ProjectId,
                    PageCount = pageCount,
                    ImageCount = imageCount
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new AlbumPageWriteResultModel(
            projectRow.ProjectId,
            projectRow.SharedVersionId.Value,
            versionPageId,
            projectRow.ShareCode,
            projectRow.IsPublic,
            command.PageNo,
            pageCount,
            imageCount,
            command.SchemaVersion);
    }

    public async Task<AlbumPreviewQueryModel?> GetPreviewAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var projectRow = await connection.QuerySingleOrDefaultAsync<ProjectRow>(
            new CommandDefinition(FindProjectSql, new { ShareCode = shareCode }, cancellationToken: cancellationToken));

        if (projectRow is null)
        {
            return null;
        }

        var pageRows = (await connection.QueryAsync<PageSummaryRow>(
            new CommandDefinition(
                FindPagesSql,
                new { ProjectVersionId = projectRow.SharedVersionId },
                cancellationToken: cancellationToken)))
            .ToArray();

        var pages = pageRows
            .Select(MapPageSummary)
            .ToArray();

        return new AlbumPreviewQueryModel(
            projectRow.ProjectId,
            projectRow.ShareCode,
            projectRow.Title,
            projectRow.Subtitle,
            projectRow.BookType,
            projectRow.ProductCode,
            projectRow.PageCount,
            projectRow.ViewCount,
            projectRow.ShareCount,
            projectRow.SharedVersionId,
            pages);
    }

    public async Task<AlbumPreviewPageQueryModel?> GetPageAsync(
        string shareCode,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var pageRow = await connection.QuerySingleOrDefaultAsync<PageRow>(
            new CommandDefinition(
                FindPageSql,
                new { ShareCode = shareCode, PageNo = pageNumber },
                cancellationToken: cancellationToken));

        if (pageRow is null)
        {
            return null;
        }

        var assetRows = (await connection.QueryAsync<PageAssetRow>(
            new CommandDefinition(
                FindPageAssetsSql,
                new { VersionPageId = pageRow.VersionPageId },
                cancellationToken: cancellationToken)))
            .ToArray();

        var images = assetRows
            .Select(MapPageAsset)
            .ToArray();

        return new AlbumPreviewPageQueryModel(
            pageRow.PageNo,
            pageRow.PageLabel,
            pageRow.PageType,
            pageRow.SchemaVersion,
            NormalizeNullable(pageRow.JsonSource),
            BuildOptionalReference(pageRow.JsonFileId, pageRow.JsonBucket, pageRow.JsonObjectKey),
            BuildOptionalReference(pageRow.HtmlFileId, pageRow.HtmlBucket, pageRow.HtmlObjectKey),
            images);
    }

    public async Task<bool> RecordViewAsync(
        string shareCode,
        int? pageNumber,
        string? clientIp,
        string? clientUserAgent,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var projectId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProjectForMutationSql,
                new { ShareCode = shareCode },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!projectId.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertViewLogSql,
                new
                {
                    ProjectId = projectId.Value,
                    ShareCode = shareCode,
                    PageNo = pageNumber,
                    ClientIp = NormalizeNullable(clientIp),
                    ClientUserAgent = NormalizeNullable(clientUserAgent)
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                IncrementViewCountSql,
                new { ProjectId = projectId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RecordShareAsync(
        string shareCode,
        string channel,
        string? clientIp,
        string? clientUserAgent,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var projectId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProjectForMutationSql,
                new { ShareCode = shareCode },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!projectId.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertShareLogSql,
                new
                {
                    ProjectId = projectId.Value,
                    ShareCode = shareCode,
                    Channel = channel,
                    ClientIp = NormalizeNullable(clientIp),
                    ClientUserAgent = NormalizeNullable(clientUserAgent)
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                IncrementShareCountSql,
                new { ProjectId = projectId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static AlbumPreviewPageSummaryModel MapPageSummary(PageSummaryRow row)
    {
        return new AlbumPreviewPageSummaryModel(
            row.PageNo,
            row.PageLabel,
            row.PageType,
            NormalizeNullable(row.JsonSource),
            BuildOptionalReference(row.JsonFileId, row.JsonBucket, row.JsonObjectKey),
            BuildOptionalReference(row.HtmlFileId, row.HtmlBucket, row.HtmlObjectKey),
            BuildOptionalReference(row.ThumbnailFileId, row.ThumbnailBucket, row.ThumbnailObjectKey),
            row.HasImages != 0);
    }

    private static AlbumPreviewPageAssetModel MapPageAsset(PageAssetRow row)
    {
        return new AlbumPreviewPageAssetModel(
            row.SortOrder,
            row.Role,
            new AlbumStoredFileReference(row.FileId, row.Bucket, row.ObjectKey),
            row.Width,
            row.Height,
            row.AltText,
            row.Caption);
    }

    private static AlbumStoredFileReference? BuildOptionalReference(long? fileId, string? bucket, string? objectKey)
    {
        if (!fileId.HasValue || string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        return new AlbumStoredFileReference(fileId.Value, bucket, objectKey);
    }

    private static string BuildSnapshotData(string schemaVersion, string shareCode, IReadOnlyCollection<SnapshotPageRow> pages)
    {
        return JsonSerializer.Serialize(new
        {
            schemaVersion,
            shareCode,
            pageCount = pages.Count,
            pages = pages.Select(page => new
            {
                pageNo = page.PageNo,
                pageLabel = page.PageLabel,
                pageType = page.PageType
            })
        });
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record ProjectRow(
        long ProjectId,
        string ShareCode,
        string Title,
        string? Subtitle,
        string BookType,
        string? ProductCode,
        int PageCount,
        long ViewCount,
        long ShareCount,
        long SharedVersionId);

    private sealed record ProjectWriteRow(
        long ProjectId,
        string ShareCode,
        long? SharedVersionId,
        bool IsPublic);

    private sealed class PageSummaryRow
    {
        public int PageNo { get; init; }

        public string PageLabel { get; init; } = string.Empty;

        public string PageType { get; init; } = string.Empty;

        public string? JsonSource { get; init; }

        public long? JsonFileId { get; init; }

        public string? JsonBucket { get; init; }

        public string? JsonObjectKey { get; init; }

        public long? HtmlFileId { get; init; }

        public string? HtmlBucket { get; init; }

        public string? HtmlObjectKey { get; init; }

        public long? ThumbnailFileId { get; init; }

        public string? ThumbnailBucket { get; init; }

        public string? ThumbnailObjectKey { get; init; }

        public long HasImages { get; init; }
    }

    private sealed class PageRow
    {
        public long VersionPageId { get; init; }

        public int PageNo { get; init; }

        public string PageLabel { get; init; } = string.Empty;

        public string PageType { get; init; } = string.Empty;

        public string SchemaVersion { get; init; } = string.Empty;

        public string? JsonSource { get; init; }

        public long? JsonFileId { get; init; }

        public string? JsonBucket { get; init; }

        public string? JsonObjectKey { get; init; }

        public long? HtmlFileId { get; init; }

        public string? HtmlBucket { get; init; }

        public string? HtmlObjectKey { get; init; }
    }

    private sealed record PageAssetRow(
        int SortOrder,
        string Role,
        long FileId,
        string Bucket,
        string ObjectKey,
        int? Width,
        int? Height,
        string? AltText,
        string? Caption);

    private sealed record FileReferenceRow(
        long FileId,
        string Bucket,
        string ObjectKey);

    private sealed class VersionStatsRow
    {
        public long PageCount { get; init; }

        public long ImageCount { get; init; }
    }

    private sealed record SnapshotPageRow(
        int PageNo,
        string PageLabel,
        string PageType);
}
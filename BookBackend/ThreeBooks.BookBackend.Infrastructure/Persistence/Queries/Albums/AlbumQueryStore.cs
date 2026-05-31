using System.Data;
using System.Text.Json;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Albums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Albums.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Albums;

public sealed class AlbumQueryStore(string connectionString) : IAlbumQueryStore
{
    private const string CountProjectsByUserSql = "usp_Album_CountProjectsByUser";
    private const string ListProjectsByUserSql = "usp_Album_ListProjectsByUser";
    private const string FindProductIdSql = "usp_Album_FindProductId";
    private const string FindProjectByShareCodeSql = "usp_Album_FindProjectByShareCode";
    private const string InsertProjectSql = "usp_Album_InsertProject";
    private const string InsertVersionSql = "usp_Album_InsertVersion";
    private const string UpdateProjectSharedVersionSql = "usp_Album_UpdateProjectSharedVersion";
    private const string FindProjectSql = "usp_Album_FindProjectPreview";
    private const string FindPagesSql = "usp_Album_FindPagesByVersion";
    private const string FindPageSql = "usp_Album_FindPageByShareCodeAndPageNo";
    private const string FindPageAssetsSql = "usp_Album_FindPageAssetsByVersionPage";
    private const string FindProjectForMutationSql = "usp_Album_FindProjectForMutation";
    private const string FindProjectForWriteSql = "usp_Album_FindProjectForWrite";
    private const string FindActiveFilesSql = "usp_Album_FindActiveFilesByIds";
    private const string InsertVersionPageSql = "usp_Album_InsertVersionPage";
    private const string InsertVersionPageAssetSql = "usp_Album_InsertVersionPageAsset";
    private const string DeleteVersionPageAssetsSql = "usp_Album_DeleteVersionPageAssets";
    private const string DeleteVersionPagesSql = "usp_Album_DeleteVersionPages";
    private const string FindVersionStatsSql = "usp_Album_FindVersionStats";
    private const string FindSnapshotPagesSql = "usp_Album_FindSnapshotPages";
    private const string UpdateProjectVersionSnapshotSql = "usp_Album_UpdateProjectVersionSnapshot";
    private const string UpdateProjectCountsSql = "usp_Album_UpdateProjectCounts";
    private const string InsertViewLogSql = "usp_Album_InsertViewLog";
    private const string InsertShareLogSql = "usp_Album_InsertShareLog";
    private const string IncrementViewCountSql = "usp_Album_IncrementViewCount";
    private const string IncrementShareCountSql = "usp_Album_IncrementShareCount";
    private const string GetLastInsertIdSql = "usp_Common_GetLastInsertId";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Album database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<PagedResult<AlbumListItemQueryModel>> GetListAsync(
        AlbumListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountProjectsByUserSql,
                new { p_user_id = filter.UserId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<AlbumListRow>(
            new CommandDefinition(
                ListProjectsByUserSql,
                new
                {
                    p_user_id = filter.UserId,
                    p_page_size = filter.PageSize,
                    p_offset = (filter.PageNumber - 1) * filter.PageSize
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return new PagedResult<AlbumListItemQueryModel>(
            rows.Select(MapAlbumListItem).ToArray(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    public async Task<bool> ShareCodeExistsAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var existingProjectId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProjectByShareCodeSql,
                new { p_share_code = shareCode },
                commandType: CommandType.StoredProcedure,
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
                new { p_share_code = command.ShareCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (existingProjectId.HasValue)
        {
            throw new ArgumentException("Share code already exists.", nameof(command.ShareCode));
        }

        var spuId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProductIdSql,
                new { p_product_code = command.ProductCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
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
                    p_user_id = command.UserId,
                    p_book_type = command.BookType,
                    p_title = command.Title,
                    p_subtitle = NormalizeNullable(command.Subtitle),
                    p_status = command.Status,
                    p_spu_id = spuId.Value,
                    p_share_code = command.ShareCode,
                    p_is_public = command.IsPublic,
                    p_shared_at = sharedAt
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var projectId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                GetLastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var snapshotData = BuildSnapshotData(command.SnapshotSchemaVersion, command.ShareCode, Array.Empty<SnapshotPageRow>());

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertVersionSql,
                new
                {
                    p_project_id = projectId,
                    p_version_no = command.VersionNo,
                    p_snapshot_data = snapshotData,
                    p_render_version = NormalizeNullable(command.RenderVersion)
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var versionId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                GetLastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectSharedVersionSql,
                new
                {
                    p_project_id = projectId,
                    p_version_id = versionId,
                    p_shared_at = sharedAt
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
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

    public async Task<AlbumPagesWriteResultModel?> SavePagesAsync(
        string shareCode,
        AlbumPagesWriteCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var projectRow = await connection.QuerySingleOrDefaultAsync<ProjectWriteRow>(
            new CommandDefinition(
                FindProjectForWriteSql,
                new { p_share_code = shareCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (projectRow is null || !projectRow.SharedVersionId.HasValue)
        {
            return null;
        }

        var fileIds = new HashSet<long>();
        foreach (var page in command.Pages)
        {
            if (page.HtmlFileId.HasValue)
            {
                fileIds.Add(page.HtmlFileId.Value);
            }

            if (page.ThumbnailFileId.HasValue)
            {
                fileIds.Add(page.ThumbnailFileId.Value);
            }

            foreach (var image in page.Images)
            {
                fileIds.Add(image.FileId);
            }
        }

        var resolvedFiles = fileIds.Count == 0
            ? new Dictionary<long, FileReferenceRow>()
            : (await connection.QueryAsync<FileReferenceRow>(
                new CommandDefinition(
                    FindActiveFilesSql,
                    new { p_file_ids = JoinCsv(fileIds) },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
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

        await connection.ExecuteAsync(
            new CommandDefinition(
                DeleteVersionPageAssetsSql,
                new { p_project_version_id = projectRow.SharedVersionId.Value },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                DeleteVersionPagesSql,
                new { p_project_version_id = projectRow.SharedVersionId.Value },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        foreach (var page in command.Pages.OrderBy(item => item.SortOrder).ThenBy(item => item.PageNo))
        {
            var htmlFile = page.HtmlFileId.HasValue ? resolvedFiles[page.HtmlFileId.Value] : null;
            var thumbnailFile = page.ThumbnailFileId.HasValue ? resolvedFiles[page.ThumbnailFileId.Value] : null;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertVersionPageSql,
                    new
                    {
                        p_project_version_id = projectRow.SharedVersionId.Value,
                        p_page_no = page.PageNo,
                        p_page_label = page.PageLabel,
                        p_page_type = page.PageType,
                        p_sort_order = page.SortOrder,
                        p_json_source = page.JsonSource,
                        p_json_file_id = (long?)null,
                        p_json_bucket = (string?)null,
                        p_json_object_key = (string?)null,
                        p_html_file_id = htmlFile?.FileId,
                        p_html_bucket = htmlFile?.Bucket,
                        p_html_object_key = htmlFile?.ObjectKey,
                        p_thumbnail_file_id = thumbnailFile?.FileId,
                        p_thumbnail_bucket = thumbnailFile?.Bucket,
                        p_thumbnail_object_key = thumbnailFile?.ObjectKey,
                        p_page_width = page.PageWidth,
                        p_page_height = page.PageHeight,
                        p_schema_version = page.SchemaVersion
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            var versionPageId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    GetLastInsertIdSql,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            foreach (var image in page.Images.OrderBy(item => item.SortOrder))
            {
                var imageFile = resolvedFiles[image.FileId];

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        InsertVersionPageAssetSql,
                        new
                        {
                            p_version_page_id = versionPageId,
                            p_sort_order = image.SortOrder,
                            p_role = image.Role,
                            p_file_id = imageFile.FileId,
                            p_bucket_name = imageFile.Bucket,
                            p_object_key = imageFile.ObjectKey,
                            p_width = image.Width,
                            p_height = image.Height,
                            p_alt_text = NormalizeNullable(image.AltText),
                            p_caption = NormalizeNullable(image.Caption),
                            p_crop_json = NormalizeNullable(image.CropJson)
                        },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken));
            }
        }

        var stats = await connection.QuerySingleAsync<VersionStatsRow>(
            new CommandDefinition(
                FindVersionStatsSql,
                new { p_project_version_id = projectRow.SharedVersionId.Value },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var pageCount = checked((int)stats.PageCount);
        var imageCount = checked((int)stats.ImageCount);

        var snapshotPages = (await connection.QueryAsync<SnapshotPageRow>(
            new CommandDefinition(
                FindSnapshotPagesSql,
                new { p_project_version_id = projectRow.SharedVersionId.Value },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        var snapshotData = BuildSnapshotData(command.SnapshotSchemaVersion, projectRow.ShareCode, snapshotPages);

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectVersionSnapshotSql,
                new
                {
                    p_project_version_id = projectRow.SharedVersionId.Value,
                    p_page_count = pageCount,
                    p_image_count = imageCount,
                    p_snapshot_data = snapshotData
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectCountsSql,
                new
                {
                    p_project_id = projectRow.ProjectId,
                    p_page_count = pageCount,
                    p_image_count = imageCount
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new AlbumPagesWriteResultModel(
            projectRow.ProjectId,
            projectRow.SharedVersionId.Value,
            projectRow.ShareCode,
            projectRow.IsPublic,
            pageCount,
            imageCount,
            command.SnapshotSchemaVersion);
    }

    public async Task<AlbumPreviewQueryModel?> GetPreviewAsync(
        string shareCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var projectRow = await connection.QuerySingleOrDefaultAsync<ProjectRow>(
            new CommandDefinition(
                FindProjectSql,
                new { p_share_code = shareCode },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (projectRow is null)
        {
            return null;
        }

        var pageRows = (await connection.QueryAsync<PageSummaryRow>(
            new CommandDefinition(
                FindPagesSql,
                new { p_project_version_id = projectRow.SharedVersionId },
                commandType: CommandType.StoredProcedure,
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
                new { p_share_code = shareCode, p_page_no = pageNumber },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (pageRow is null)
        {
            return null;
        }

        var assetRows = (await connection.QueryAsync<PageAssetRow>(
            new CommandDefinition(
                FindPageAssetsSql,
                new { p_version_page_id = pageRow.VersionPageId },
                commandType: CommandType.StoredProcedure,
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
                new { p_share_code = shareCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
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
                    p_project_id = projectId.Value,
                    p_share_code = shareCode,
                    p_page_no = pageNumber,
                    p_client_ip = NormalizeNullable(clientIp),
                    p_client_user_agent = NormalizeNullable(clientUserAgent)
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                IncrementViewCountSql,
                new { p_project_id = projectId.Value },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
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
                new { p_share_code = shareCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
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
                    p_project_id = projectId.Value,
                    p_share_code = shareCode,
                    p_channel = channel,
                    p_client_ip = NormalizeNullable(clientIp),
                    p_client_user_agent = NormalizeNullable(clientUserAgent)
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                IncrementShareCountSql,
                new { p_project_id = projectId.Value },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
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

    private static string JoinCsv(IEnumerable<long> values)
    {
        return string.Join(',', values);
    }

    private static AlbumListItemQueryModel MapAlbumListItem(AlbumListRow row)
    {
        return new AlbumListItemQueryModel(
            row.ProjectId,
            NormalizeNullable(row.ShareCode),
            row.Title,
            row.Subtitle,
            row.BookType,
            NormalizeNullable(row.ProductCode),
            row.IsPublic,
            row.PageCount,
            row.ImageCount,
            row.ViewCount,
            row.ShareCount,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
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

    private sealed class AlbumListRow
    {
        public long ProjectId { get; init; }

        public string? ShareCode { get; init; }

        public string Title { get; init; } = string.Empty;

        public string? Subtitle { get; init; }

        public string BookType { get; init; } = string.Empty;

        public string? ProductCode { get; init; }

        public bool IsPublic { get; init; }

        public int PageCount { get; init; }

        public int ImageCount { get; init; }

        public long ViewCount { get; init; }

        public long ShareCount { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime UpdatedAtUtc { get; init; }
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
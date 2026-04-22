using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.AlbumTemplates;

public sealed class AlbumTemplateQueryStore(string connectionString) : IAlbumTemplateQueryStore
{
    private const string CountSql = """
        SELECT COUNT(*)
        FROM album_content_template t
        WHERE (@Keyword IS NULL
            OR t.template_code LIKE CONCAT('%', @Keyword, '%')
            OR t.name LIKE CONCAT('%', @Keyword, '%')
            OR t.description LIKE CONCAT('%', @Keyword, '%'))
          AND (@BookType IS NULL OR t.book_type = @BookType)
          AND (@PageType IS NULL OR t.page_type = @PageType)
          AND (@Category IS NULL OR t.category = @Category)
          AND (@IsActive IS NULL OR t.is_active = @IsActive);
        """;

    private const string ListSql = """
        SELECT
            t.id AS TemplateId,
            t.template_code AS TemplateCode,
            t.name AS Name,
            t.description AS Description,
            t.book_type AS BookType,
            t.page_type AS PageType,
            t.category AS Category,
            t.theme_code AS ThemeCode,
            t.schema_version AS SchemaVersion,
            t.is_built_in AS IsBuiltIn,
            t.is_active AS IsActive,
            t.sort_order AS SortOrder,
            t.updated_at AS UpdatedAtUtc,
            preview.bucket_name AS PreviewBucket,
            preview.object_key AS PreviewObjectKey
        FROM album_content_template t
        LEFT JOIN storage_file_object preview ON preview.id = t.preview_file_id AND preview.storage_status = 1
        WHERE (@Keyword IS NULL
            OR t.template_code LIKE CONCAT('%', @Keyword, '%')
            OR t.name LIKE CONCAT('%', @Keyword, '%')
            OR t.description LIKE CONCAT('%', @Keyword, '%'))
          AND (@BookType IS NULL OR t.book_type = @BookType)
          AND (@PageType IS NULL OR t.page_type = @PageType)
          AND (@Category IS NULL OR t.category = @Category)
          AND (@IsActive IS NULL OR t.is_active = @IsActive)
        ORDER BY t.sort_order ASC, t.updated_at DESC, t.id DESC
        LIMIT @PageSize OFFSET @Offset;
        """;

    private const string DetailSql = """
        SELECT
            t.id AS TemplateId,
            t.template_code AS TemplateCode,
            t.name AS Name,
            t.description AS Description,
            t.book_type AS BookType,
            t.page_type AS PageType,
            t.category AS Category,
            t.theme_code AS ThemeCode,
            t.schema_version AS SchemaVersion,
            t.json_source AS JsonSource,
            t.preview_file_id AS PreviewFileId,
            t.created_by_user_id AS CreatedByUserId,
            t.is_built_in AS IsBuiltIn,
            t.is_active AS IsActive,
            t.sort_order AS SortOrder,
            t.created_at AS CreatedAtUtc,
            t.updated_at AS UpdatedAtUtc,
            preview.bucket_name AS PreviewBucket,
            preview.object_key AS PreviewObjectKey
        FROM album_content_template t
        LEFT JOIN storage_file_object preview ON preview.id = t.preview_file_id AND preview.storage_status = 1
        WHERE t.template_code = @TemplateCode
        LIMIT 1;
        """;

    private const string FindPreviewFileSql = """
        SELECT id
        FROM storage_file_object
        WHERE id = @PreviewFileId
                    AND storage_status = 1
        LIMIT 1;
        """;

    private const string FindCreatorSql = """
        SELECT id
        FROM identity_user
        WHERE id = @CreatedByUserId
        LIMIT 1;
        """;

    private const string InsertSql = """
        INSERT INTO album_content_template (
            template_code,
            name,
            description,
            book_type,
            page_type,
            category,
            theme_code,
            schema_version,
            json_source,
            preview_file_id,
            created_by_user_id,
            is_built_in,
            is_active,
            sort_order)
        VALUES (
            @TemplateCode,
            @Name,
            @Description,
            @BookType,
            @PageType,
            @Category,
            @ThemeCode,
            @SchemaVersion,
            @JsonSource,
            @PreviewFileId,
            @CreatedByUserId,
            @IsBuiltIn,
            @IsActive,
            @SortOrder);
        """;

    private const string LastInsertIdSql = "SELECT LAST_INSERT_ID();";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Album template database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<PagedResult<AlbumTemplateListItemQueryModel>> GetListAsync(
        AlbumTemplateListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            filter.Keyword,
            filter.BookType,
            filter.PageType,
            filter.Category,
            filter.IsActive,
            filter.PageSize,
            Offset = (filter.PageNumber - 1) * filter.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CountSql, parameters, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<AlbumTemplateListRow>(
            new CommandDefinition(ListSql, parameters, cancellationToken: cancellationToken));

        var response = new PagedResult<AlbumTemplateListItemQueryModel>(
            rows.Select(MapListItem).ToArray(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);

        return response;
    }

    public async Task<AlbumTemplateDetailQueryModel?> GetDetailAsync(
        string templateCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AlbumTemplateDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { TemplateCode = templateCode },
                cancellationToken: cancellationToken));

        return row is null ? null : MapDetail(row);
    }

    public async Task<AlbumTemplateCreateResultModel> CreateAsync(
        AlbumTemplateCreateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (command.PreviewFileId.HasValue)
        {
            var previewFileExists = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    FindPreviewFileSql,
                    new { command.PreviewFileId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (!previewFileExists.HasValue)
            {
                throw new ArgumentException("Preview file does not exist.", nameof(command.PreviewFileId));
            }
        }

        if (command.CreatedByUserId.HasValue)
        {
            var userExists = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    FindCreatorSql,
                    new { command.CreatedByUserId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (!userExists.HasValue)
            {
                throw new ArgumentException("CreatedByUser does not exist.", nameof(command.CreatedByUserId));
            }
        }

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertSql,
                    command,
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new ArgumentException("TemplateCode already exists.", nameof(command.TemplateCode), exception);
        }

        var templateId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(LastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        var created = await connection.QuerySingleAsync<AlbumTemplateDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { TemplateCode = command.TemplateCode },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new AlbumTemplateCreateResultModel(
            templateId,
            created.TemplateCode,
            created.IsActive,
            BuildPreviewFile(created.PreviewBucket, created.PreviewObjectKey));
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static AlbumTemplateListItemQueryModel MapListItem(AlbumTemplateListRow row)
    {
        return new AlbumTemplateListItemQueryModel(
            row.TemplateId,
            row.TemplateCode,
            row.Name,
            row.Description,
            row.BookType,
            row.PageType,
            row.Category,
            row.ThemeCode,
            row.SchemaVersion,
            BuildPreviewFile(row.PreviewBucket, row.PreviewObjectKey),
            row.IsBuiltIn,
            row.IsActive,
            row.SortOrder,
            row.UpdatedAtUtc);
    }

    private static AlbumTemplateDetailQueryModel MapDetail(AlbumTemplateDetailRow row)
    {
        return new AlbumTemplateDetailQueryModel(
            row.TemplateId,
            row.TemplateCode,
            row.Name,
            row.Description,
            row.BookType,
            row.PageType,
            row.Category,
            row.ThemeCode,
            row.SchemaVersion,
            row.JsonSource,
            row.PreviewFileId,
            BuildPreviewFile(row.PreviewBucket, row.PreviewObjectKey),
            row.CreatedByUserId,
            row.IsBuiltIn,
            row.IsActive,
            row.SortOrder,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

    private static AlbumTemplateStoredFileReference? BuildPreviewFile(string? bucket, string? objectKey)
    {
        return string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(objectKey)
            ? null
            : new AlbumTemplateStoredFileReference(bucket, objectKey);
    }

    private class AlbumTemplateListRow
    {
        public long TemplateId { get; init; }

        public string TemplateCode { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string? BookType { get; init; }

        public string PageType { get; init; } = string.Empty;

        public string? Category { get; init; }

        public string? ThemeCode { get; init; }

        public string SchemaVersion { get; init; } = string.Empty;

        public bool IsBuiltIn { get; init; }

        public bool IsActive { get; init; }

        public int SortOrder { get; init; }

        public DateTime UpdatedAtUtc { get; init; }

        public string? PreviewBucket { get; init; }

        public string? PreviewObjectKey { get; init; }
    }

    private sealed class AlbumTemplateDetailRow : AlbumTemplateListRow
    {
        public string JsonSource { get; init; } = string.Empty;

        public long? PreviewFileId { get; init; }

        public long? CreatedByUserId { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }

}
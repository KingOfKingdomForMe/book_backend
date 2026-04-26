using System.Data;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.AlbumTemplates.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.AlbumTemplates;

public sealed class AlbumTemplateQueryStore(string connectionString) : IAlbumTemplateQueryStore
{
    private const string CountSql = "usp_AlbumTemplates_Count";
    private const string ListSql = "usp_AlbumTemplates_List";
    private const string DetailSql = "usp_AlbumTemplates_GetDetail";
    private const string FindPreviewFileSql = "usp_AlbumTemplates_FindPreviewFile";
    private const string FindCreatorSql = "usp_AlbumTemplates_FindCreator";
    private const string InsertSql = "usp_AlbumTemplates_Create";
    private const string UpdateSql = "usp_AlbumTemplates_Update";
    private const string LastInsertIdSql = "usp_Common_GetLastInsertId";

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
            p_keyword = NormalizeNullable(filter.Keyword),
            p_book_type = NormalizeNullable(filter.BookType),
            p_page_type = NormalizeNullable(filter.PageType),
            p_category = NormalizeNullable(filter.Category),
            p_is_active = filter.IsActive,
            p_page_size = filter.PageSize,
            p_offset = (filter.PageNumber - 1) * filter.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<AlbumTemplateListRow>(
            new CommandDefinition(
                ListSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

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
                new { p_template_code = templateCode },
                commandType: CommandType.StoredProcedure,
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
                    new { p_preview_file_id = command.PreviewFileId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
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
                    new { p_created_by_user_id = command.CreatedByUserId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
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
                    new
                    {
                        p_template_code = command.TemplateCode,
                        p_name = command.Name,
                        p_description = NormalizeNullable(command.Description),
                        p_book_type = NormalizeNullable(command.BookType),
                        p_page_type = command.PageType,
                        p_category = NormalizeNullable(command.Category),
                        p_theme_code = NormalizeNullable(command.ThemeCode),
                        p_schema_version = command.SchemaVersion,
                        p_json_source = command.JsonSource,
                        p_preview_file_id = command.PreviewFileId,
                        p_created_by_user_id = command.CreatedByUserId,
                        p_is_built_in = command.IsBuiltIn,
                        p_is_active = command.IsActive,
                        p_sort_order = command.SortOrder
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new ArgumentException("TemplateCode already exists.", nameof(command.TemplateCode), exception);
        }

        var templateId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                LastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var created = await connection.QuerySingleAsync<AlbumTemplateDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { p_template_code = command.TemplateCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new AlbumTemplateCreateResultModel(
            templateId,
            created.TemplateCode,
            created.IsActive,
            BuildPreviewFile(created.PreviewBucket, created.PreviewObjectKey));
    }

    public async Task<AlbumTemplateDetailQueryModel?> UpdateAsync(
        string templateCode,
        AlbumTemplateUpdateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<AlbumTemplateDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { p_template_code = templateCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (existing is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (command.PreviewFileId.HasValue)
        {
            var previewFileExists = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    FindPreviewFileSql,
                    new { p_preview_file_id = command.PreviewFileId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
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
                    new { p_created_by_user_id = command.CreatedByUserId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (!userExists.HasValue)
            {
                throw new ArgumentException("CreatedByUser does not exist.", nameof(command.CreatedByUserId));
            }
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateSql,
                new
                {
                    p_template_code = templateCode,
                    p_name = command.Name,
                    p_description = NormalizeNullable(command.Description),
                    p_book_type = NormalizeNullable(command.BookType),
                    p_page_type = command.PageType,
                    p_category = NormalizeNullable(command.Category),
                    p_theme_code = NormalizeNullable(command.ThemeCode),
                    p_schema_version = command.SchemaVersion,
                    p_json_source = command.JsonSource,
                    p_preview_file_id = command.PreviewFileId,
                    p_created_by_user_id = command.CreatedByUserId,
                    p_is_built_in = command.IsBuiltIn,
                    p_is_active = command.IsActive,
                    p_sort_order = command.SortOrder
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var updated = await connection.QuerySingleAsync<AlbumTemplateDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { p_template_code = templateCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return MapDetail(updated);
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

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
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
using System.Data;
using System.Text.Json;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.DefaultAlbums.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.DefaultAlbums;

public sealed class DefaultAlbumQueryStore(string connectionString) : IDefaultAlbumQueryStore
{
    private const string CountSql = "usp_DefaultAlbums_Count";
    private const string ListSql = "usp_DefaultAlbums_List";
    private const string DetailSql = "usp_DefaultAlbums_GetDetail";
    private const string DetailByProductCodeSql = "usp_DefaultAlbums_GetActiveByProductCode";
    private const string ListTemplatesSql = "usp_DefaultAlbums_GetTemplates";
    private const string FindPreviewFileSql = "usp_DefaultAlbums_FindPreviewFile";
    private const string FindCreatorSql = "usp_DefaultAlbums_FindCreator";
    private const string FindProductIdSql = "usp_DefaultAlbums_FindProductId";
    private const string FindTemplatesByCodesSql = "usp_DefaultAlbums_FindTemplateIdsByCodes";
    private const string InsertSql = "usp_DefaultAlbums_Create";
    private const string UpdateSql = "usp_DefaultAlbums_Update";
    private const string AddTemplateSql = "usp_DefaultAlbums_AddTemplate";
    private const string DeleteTemplatesSql = "usp_DefaultAlbums_DeleteTemplates";
    private const string LastInsertIdSql = "usp_Common_GetLastInsertId";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Default album database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<PagedResult<DefaultAlbumListItemQueryModel>> GetListAsync(
        DefaultAlbumListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            p_keyword = NormalizeNullable(filter.Keyword),
            p_product_code = NormalizeNullable(filter.ProductCode),
            p_book_type = NormalizeNullable(filter.BookType),
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

        var rows = await connection.QueryAsync<DefaultAlbumListRow>(
            new CommandDefinition(
                ListSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return new PagedResult<DefaultAlbumListItemQueryModel>(
            rows.Select(MapListItem).ToArray(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    public async Task<DefaultAlbumDetailQueryModel?> GetDetailAsync(
        string albumCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await GetDetailRowAsync(connection, null, albumCode, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var templates = await GetTemplatesAsync(connection, null, row.AlbumId, cancellationToken);
        return MapDetail(row, templates);
    }

    public async Task<DefaultAlbumDetailQueryModel?> GetActiveByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<DefaultAlbumDetailRow>(
            new CommandDefinition(
                DetailByProductCodeSql,
                new { p_product_code = productCode },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var templates = await GetTemplatesAsync(connection, null, row.AlbumId, cancellationToken);
        return MapDetail(row, templates);
    }

    public async Task<DefaultAlbumCreateResultModel> CreateAsync(
        DefaultAlbumCreateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var productId = await ResolveProductIdAsync(connection, transaction, command.ProductCode, cancellationToken);

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
            var creatorExists = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    FindCreatorSql,
                    new { p_created_by_user_id = command.CreatedByUserId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (!creatorExists.HasValue)
            {
                throw new ArgumentException("CreatedByUser does not exist.", nameof(command.CreatedByUserId));
            }
        }

        var templateIds = await ResolveTemplateIdsAsync(connection, transaction, command.Templates, cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertSql,
                    new
                    {
                        p_album_code = command.AlbumCode,
                        p_product_spu_id = productId,
                        p_name = command.Name,
                        p_description = NormalizeNullable(command.Description),
                        p_book_type = NormalizeNullable(command.BookType),
                        p_category = NormalizeNullable(command.Category),
                        p_theme_code = NormalizeNullable(command.ThemeCode),
                        p_extra_properties_json = SerializeStringCollection(command.ExtraProperties),
                        p_preview_file_id = command.PreviewFileId,
                        p_created_by_user_id = command.CreatedByUserId,
                        p_is_active = command.IsActive,
                        p_sort_order = command.SortOrder
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            if (IsProductCodeDuplicate(exception))
            {
                throw new ArgumentException("ProductCode already has a default album.", nameof(command.ProductCode), exception);
            }

            throw new ArgumentException("AlbumCode already exists.", nameof(command.AlbumCode), exception);
        }

        var albumId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                LastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        foreach (var template in command.Templates)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    AddTemplateSql,
                    new
                    {
                        p_default_album_id = albumId,
                        p_template_id = templateIds[template.TemplateCode],
                        p_sort_order = template.SortOrder
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }

        var created = await GetDetailRowAsync(connection, transaction, command.AlbumCode, cancellationToken)
            ?? throw new InvalidOperationException("Created default album could not be loaded.");

        await transaction.CommitAsync(cancellationToken);

        return new DefaultAlbumCreateResultModel(
            albumId,
            created.AlbumCode,
            created.IsActive,
            BuildPreviewFile(created.PreviewBucket, created.PreviewObjectKey));
    }

    public async Task<DefaultAlbumDetailQueryModel?> UpdateAsync(
        string albumCode,
        DefaultAlbumUpdateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var existing = await GetDetailRowAsync(connection, transaction, albumCode, cancellationToken);
        if (existing is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var productId = await ResolveProductIdAsync(connection, transaction, command.ProductCode, cancellationToken);

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
            var creatorExists = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(
                    FindCreatorSql,
                    new { p_created_by_user_id = command.CreatedByUserId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (!creatorExists.HasValue)
            {
                throw new ArgumentException("CreatedByUser does not exist.", nameof(command.CreatedByUserId));
            }
        }

        var templateIds = await ResolveTemplateIdsAsync(connection, transaction, command.Templates, cancellationToken);

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    UpdateSql,
                    new
                    {
                        p_album_code = albumCode,
                        p_product_spu_id = productId,
                        p_name = command.Name,
                        p_description = NormalizeNullable(command.Description),
                        p_book_type = NormalizeNullable(command.BookType),
                        p_category = NormalizeNullable(command.Category),
                        p_theme_code = NormalizeNullable(command.ThemeCode),
                        p_extra_properties_json = SerializeStringCollection(command.ExtraProperties),
                        p_preview_file_id = command.PreviewFileId,
                        p_created_by_user_id = command.CreatedByUserId,
                        p_is_active = command.IsActive,
                        p_sort_order = command.SortOrder
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }
        catch (MySqlException exception) when (exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new ArgumentException("ProductCode already has a default album.", nameof(command.ProductCode), exception);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                DeleteTemplatesSql,
                new { p_default_album_id = existing.AlbumId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        foreach (var template in command.Templates)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    AddTemplateSql,
                    new
                    {
                        p_default_album_id = existing.AlbumId,
                        p_template_id = templateIds[template.TemplateCode],
                        p_sort_order = template.SortOrder
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }

        var updated = await GetDetailRowAsync(connection, transaction, albumCode, cancellationToken)
            ?? throw new InvalidOperationException("Updated default album could not be loaded.");

        var templates = await GetTemplatesAsync(connection, transaction, updated.AlbumId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapDetail(updated, templates);
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<DefaultAlbumDetailRow?> GetDetailRowAsync(
        MySqlConnection connection,
        IDbTransaction? transaction,
        string albumCode,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleOrDefaultAsync<DefaultAlbumDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { p_album_code = albumCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    private async Task<IReadOnlyCollection<DefaultAlbumTemplateItemQueryModel>> GetTemplatesAsync(
        MySqlConnection connection,
        IDbTransaction? transaction,
        long albumId,
        CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<DefaultAlbumTemplateRow>(
            new CommandDefinition(
                ListTemplatesSql,
                new { p_default_album_id = albumId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return rows.Select(MapTemplateItem).ToArray();
    }

    private static DefaultAlbumListItemQueryModel MapListItem(DefaultAlbumListRow row)
    {
        return new DefaultAlbumListItemQueryModel(
            row.AlbumId,
            row.AlbumCode,
            row.ProductCode ?? string.Empty,
            row.Name,
            row.Description,
            row.BookType,
            row.Category,
            row.ThemeCode,
            ParseStringCollection(row.ExtraPropertiesJson),
            BuildPreviewFile(row.PreviewBucket, row.PreviewObjectKey),
            row.TemplateCount,
            row.IsActive,
            row.SortOrder,
            row.UpdatedAtUtc);
    }

    private static DefaultAlbumDetailQueryModel MapDetail(
        DefaultAlbumDetailRow row,
        IReadOnlyCollection<DefaultAlbumTemplateItemQueryModel> templates)
    {
        return new DefaultAlbumDetailQueryModel(
            row.AlbumId,
            row.AlbumCode,
            row.ProductCode ?? string.Empty,
            row.Name,
            row.Description,
            row.BookType,
            row.Category,
            row.ThemeCode,
            ParseStringCollection(row.ExtraPropertiesJson),
            row.PreviewFileId,
            BuildPreviewFile(row.PreviewBucket, row.PreviewObjectKey),
            row.CreatedByUserId,
            row.IsActive,
            row.SortOrder,
            templates,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

    private static DefaultAlbumTemplateItemQueryModel MapTemplateItem(DefaultAlbumTemplateRow row)
    {
        return new DefaultAlbumTemplateItemQueryModel(
            row.ItemId,
            row.TemplateId,
            row.TemplateCode,
            row.Name,
            row.Description,
            row.PageType,
            row.Category,
            row.ThemeCode,
            row.SchemaVersion,
            row.JsonSource,
            BuildPreviewFile(row.PreviewBucket, row.PreviewObjectKey),
            row.SortOrder);
    }

    private async Task<IReadOnlyDictionary<string, long>> ResolveTemplateIdsAsync(
        MySqlConnection connection,
        IDbTransaction? transaction,
        IReadOnlyCollection<DefaultAlbumTemplateAssignmentModel> templates,
        CancellationToken cancellationToken)
    {
        var templateCodes = templates.Select(template => template.TemplateCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var rows = await connection.QueryAsync<TemplateReferenceRow>(
            new CommandDefinition(
                FindTemplatesByCodesSql,
                new { p_template_codes = string.Join(',', templateCodes) },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var lookup = rows.ToDictionary(
            row => row.TemplateCode,
            row => row.TemplateId,
            StringComparer.OrdinalIgnoreCase);

        var missing = templateCodes
            .Where(templateCode => !lookup.ContainsKey(templateCode))
            .OrderBy(templateCode => templateCode, StringComparer.Ordinal)
            .ToArray();

        if (missing.Length > 0)
        {
            throw new ArgumentException($"Templates do not exist: {string.Join(", ", missing)}.", nameof(templates));
        }

        return lookup;
    }

    private async Task<long> ResolveProductIdAsync(
        MySqlConnection connection,
        IDbTransaction? transaction,
        string productCode,
        CancellationToken cancellationToken)
    {
        var productId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindProductIdSql,
                new { p_product_code = productCode },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (!productId.HasValue)
        {
            throw new ArgumentException("ProductCode does not exist or is inactive.", nameof(productCode));
        }

        return productId.Value;
    }

    private static bool IsProductCodeDuplicate(MySqlException exception)
    {
        return exception.Message.Contains("uk_default_album_product_spu", StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultAlbumStoredFileReference? BuildPreviewFile(string? bucket, string? objectKey)
    {
        return string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(objectKey)
            ? null
            : new DefaultAlbumStoredFileReference(bucket, objectKey);
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? SerializeStringCollection(IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        var normalized = values
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.Length == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    private static IReadOnlyCollection<string> ParseStringCollection(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return (JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>())
                .Select(value => value?.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private class DefaultAlbumListRow
    {
        public long AlbumId { get; init; }

        public string AlbumCode { get; init; } = string.Empty;

        public string? ProductCode { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string? BookType { get; init; }

        public string? Category { get; init; }

        public string? ThemeCode { get; init; }

        public string? ExtraPropertiesJson { get; init; }

        public bool IsActive { get; init; }

        public int SortOrder { get; init; }

        public int TemplateCount { get; init; }

        public DateTime UpdatedAtUtc { get; init; }

        public string? PreviewBucket { get; init; }

        public string? PreviewObjectKey { get; init; }
    }

    private sealed class DefaultAlbumDetailRow : DefaultAlbumListRow
    {
        public long? PreviewFileId { get; init; }

        public long? CreatedByUserId { get; init; }

        public DateTime CreatedAtUtc { get; init; }
    }

    private sealed class DefaultAlbumTemplateRow
    {
        public long ItemId { get; init; }

        public long TemplateId { get; init; }

        public string TemplateCode { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public string PageType { get; init; } = string.Empty;

        public string? Category { get; init; }

        public string? ThemeCode { get; init; }

        public string SchemaVersion { get; init; } = string.Empty;

        public string JsonSource { get; init; } = string.Empty;

        public int SortOrder { get; init; }

        public string? PreviewBucket { get; init; }

        public string? PreviewObjectKey { get; init; }
    }

    private sealed class TemplateReferenceRow
    {
        public long TemplateId { get; init; }

        public string TemplateCode { get; init; } = string.Empty;
    }
}
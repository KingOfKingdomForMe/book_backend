using System.Data;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Models;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Unboxings;

public sealed class UnboxingQueryStore(string connectionString) : IUnboxingQueryStore
{
    private const string UserExistsSql = "usp_Unboxing_UserExists";
    private const string PostNoExistsSql = "usp_Unboxing_PostNoExists";
    private const string CurrentMaxNumericPostNoSql = "usp_Unboxing_GetCurrentMaxNumericPostNo";
    private const string LevelByCodeSql = "usp_Unboxing_GetLevelByCode";
    private const string ExistingPostSql = "usp_Unboxing_FindExistingPost";
    private const string InsertPostSql = "usp_Unboxing_InsertPost";
    private const string UpdatePostSql = "usp_Unboxing_UpdatePost";
    private const string DeleteMediaSql = "usp_Unboxing_DeleteMedia";
    private const string DeleteTagsSql = "usp_Unboxing_DeleteTags";
    private const string InsertMediaSql = "usp_Unboxing_InsertMedia";
    private const string InsertTagSql = "usp_Unboxing_InsertTag";
    private const string CountSql = "usp_Unboxing_Count";
    private const string ListSql = "usp_Unboxing_List";
    private const string DetailSql = "usp_Unboxing_GetDetail";
    private const string AdminCountSql = "usp_Unboxing_AdminCount";
    private const string AdminListSql = "usp_Unboxing_AdminList";
    private const string AdminDetailSql = "usp_Unboxing_AdminGetDetail";
    private const string DeletePostSql = "usp_Unboxing_DeletePost";
    private const string MediaSql = "usp_Unboxing_GetMedia";
    private const string TagsByPostIdsSql = "usp_Unboxing_GetTagsByPostIds";
    private const string TagsByPostIdSql = "usp_Unboxing_GetTagsByPostId";
    private const string LevelsSql = "usp_Unboxing_GetLevels";
    private const string GetLastInsertIdProcedure = "usp_Common_GetLastInsertId";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Unboxing database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<AdminUnboxingListQueryResultModel> GetAdminListAsync(
        AdminUnboxingListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            p_keyword = filter.Keyword,
            p_level_code = filter.LevelCode,
            p_is_featured = filter.IsFeatured,
            p_status = filter.Status,
            p_page_size = filter.PageSize,
            p_offset = (filter.PageNumber - 1) * filter.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                AdminCountSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<AdminUnboxingListRow>(
            new CommandDefinition(
                AdminListSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        var tagLookup = await GetTagLookupAsync(connection, rows.Select(row => row.PostId).ToArray(), cancellationToken);

        var items = rows
            .Select(row => new AdminUnboxingListItemQueryModel(
                row.PostId,
                row.PostNo,
                row.UserId,
                row.AuthorName,
                row.AuthorAvatarUrl,
                row.Title,
                row.BookTitle,
                row.ProductLabel,
                row.Status,
                BuildLevel(row.LevelCode, row.LevelName, row.LevelIconUrl),
                row.CoverImageUrl,
                row.CoverThumbnailUrl,
                row.IsFeatured,
                row.PublishedAtUtc,
                row.CreatedAtUtc,
                row.UpdatedAtUtc,
                tagLookup.TryGetValue(row.PostId, out var tags) ? tags : Array.Empty<string>()))
            .ToArray();

        return new AdminUnboxingListQueryResultModel(items, totalCount);
    }

    public async Task<AdminUnboxingDetailQueryModel?> GetAdminDetailAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AdminUnboxingDetailRow>(
            new CommandDefinition(
                AdminDetailSql,
                new { p_post_no = postNo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var mediaRows = (await connection.QueryAsync<UnboxingMediaRow>(
            new CommandDefinition(
                MediaSql,
                new { p_post_id = row.PostId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        var tags = (await connection.QueryAsync<string>(
            new CommandDefinition(
                TagsByPostIdSql,
                new { p_post_id = row.PostId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        return new AdminUnboxingDetailQueryModel(
            row.PostId,
            row.PostNo,
            row.UserId,
            row.AuthorName,
            row.AuthorAvatarUrl,
            row.Title,
            row.BookTitle,
            row.ContentText,
            row.ProductLabel,
            row.Status,
            BuildLevel(row.LevelCode, row.LevelName, row.LevelIconUrl),
            row.IsFeatured,
            row.PublishedAtUtc,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            tags,
            mediaRows.Select(item => new UnboxingMediaModel(
                item.MediaType,
                item.StorageUrl,
                item.ThumbnailUrl,
                item.SortOrder,
                item.Width,
                item.Height)).ToArray());
    }

    public async Task<bool> DeleteAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<ExistingPostRow>(new CommandDefinition(
            ExistingPostSql,
            new { p_post_no = postNo },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            DeletePostSql,
            new { p_post_no = postNo },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UserExistsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                UserExistsSql,
                new { p_user_id = userId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<bool> PostNoExistsAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                PostNoExistsSql,
                new { p_post_no = postNo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<long> GetCurrentMaxNumericPostNoAsync(CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                CurrentMaxNumericPostNoSql,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<UnboxingLevelModel?> GetLevelByCodeAsync(
        string levelCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UnboxingLevelModel>(
            new CommandDefinition(
                LevelByCodeSql,
                new { p_level_code = levelCode },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<UnboxingWriteResultModel> CreateAsync(
        UnboxingCreateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertPostSql,
            new
            {
                p_post_no = command.PostNo,
                p_user_id = command.UserId,
                p_author_name = command.AuthorName,
                p_author_avatar_url = command.AuthorAvatarUrl,
                p_order_item_id = command.OrderItemId,
                p_project_version_id = command.ProjectVersionId,
                p_product_label = command.ProductLabel,
                p_book_title = command.BookTitle,
                p_level_code = command.LevelCode,
                p_title = command.Title,
                p_content_text = command.ContentText,
                p_status = command.Status,
                p_is_featured = command.IsFeatured,
                p_published_at_utc = command.PublishedAtUtc
            },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var postId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            GetLastInsertIdProcedure,
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        await ReplaceMediaAsync(connection, transaction, postId, command.Media, cancellationToken);
        await ReplaceTagsAsync(connection, transaction, postId, command.Tags, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new UnboxingWriteResultModel(
            postId,
            command.PostNo,
            command.UserId,
            command.Status,
            command.IsFeatured,
            command.Media.Count,
            command.Tags.Count,
            command.PublishedAtUtc);
    }

    public async Task<UnboxingWriteResultModel?> UpdateAsync(
        string postNo,
        UnboxingUpdateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<ExistingPostRow>(new CommandDefinition(
            ExistingPostSql,
            new { p_post_no = postNo },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            UpdatePostSql,
            new
            {
                p_post_no = postNo,
                p_author_name = command.AuthorName,
                p_author_avatar_url = command.AuthorAvatarUrl,
                p_order_item_id = command.OrderItemId,
                p_project_version_id = command.ProjectVersionId,
                p_product_label = command.ProductLabel,
                p_book_title = command.BookTitle,
                p_level_code = command.LevelCode,
                p_title = command.Title,
                p_content_text = command.ContentText,
                p_status = command.Status,
                p_is_featured = command.IsFeatured,
                p_published_at_utc = command.PublishedAtUtc
            },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        await ReplaceMediaAsync(connection, transaction, existing.PostId, command.Media, cancellationToken);
        await ReplaceTagsAsync(connection, transaction, existing.PostId, command.Tags, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new UnboxingWriteResultModel(
            existing.PostId,
            postNo,
            existing.UserId,
            command.Status,
            command.IsFeatured,
            command.Media.Count,
            command.Tags.Count,
            command.PublishedAtUtc);
    }

    public async Task<UnboxingListQueryResultModel> GetListAsync(
        UnboxingListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            p_level_code = filter.LevelCode,
            p_is_featured = filter.IsFeatured,
            p_page_size = filter.PageSize,
            p_offset = (filter.PageNumber - 1) * filter.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<UnboxingListRow>(
            new CommandDefinition(
                ListSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        var tagLookup = await GetTagLookupAsync(connection, rows.Select(row => row.PostId).ToArray(), cancellationToken);

        var items = rows
            .Select(row => new UnboxingListItemQueryModel(
                row.PostId,
                row.PostNo,
                row.AuthorName,
                row.AuthorAvatarUrl,
                row.Title,
                row.BookTitle,
                row.ContentText,
                row.ProductLabel,
                BuildLevel(row.LevelCode, row.LevelName, row.LevelIconUrl),
                row.CoverImageUrl,
                row.CoverThumbnailUrl,
                row.IsFeatured,
                row.PublishedAtUtc,
                tagLookup.TryGetValue(row.PostId, out var tags) ? tags : Array.Empty<string>()))
            .ToArray();

        return new UnboxingListQueryResultModel(items, totalCount);
    }

    public async Task<UnboxingDetailQueryModel?> GetDetailAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UnboxingDetailRow>(
            new CommandDefinition(
                DetailSql,
                new { p_post_no = postNo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var mediaRows = (await connection.QueryAsync<UnboxingMediaRow>(
            new CommandDefinition(
                MediaSql,
                new { p_post_id = row.PostId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        var media = mediaRows
            .Select(item => new UnboxingMediaModel(
                item.MediaType,
                item.StorageUrl,
                item.ThumbnailUrl,
                item.SortOrder,
                item.Width,
                item.Height))
            .ToArray();

        var tags = (await connection.QueryAsync<string>(
            new CommandDefinition(
                TagsByPostIdSql,
                new { p_post_id = row.PostId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        return new UnboxingDetailQueryModel(
            row.PostId,
            row.PostNo,
            row.AuthorName,
            row.AuthorAvatarUrl,
            row.Title,
            row.BookTitle,
            row.ContentText,
            row.ProductLabel,
            BuildLevel(row.LevelCode, row.LevelName, row.LevelIconUrl),
            row.IsFeatured,
            row.PublishedAtUtc,
            tags,
            media);
    }

    public async Task<IReadOnlyCollection<UnboxingLevelModel>> GetLevelsAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<UnboxingLevelModel>(
            new CommandDefinition(
                LevelsSql,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return rows.ToArray();
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static UnboxingLevelModel? BuildLevel(string? levelCode, string? levelName, string? iconUrl)
    {
        return string.IsNullOrWhiteSpace(levelCode) || string.IsNullOrWhiteSpace(levelName)
            ? null
            : new UnboxingLevelModel(levelCode, levelName, iconUrl);
    }

    private static async Task<Dictionary<long, IReadOnlyCollection<string>>> GetTagLookupAsync(
        MySqlConnection connection,
        long[] postIds,
        CancellationToken cancellationToken)
    {
        if (postIds.Length == 0)
        {
            return new Dictionary<long, IReadOnlyCollection<string>>();
        }

        var rows = (await connection.QueryAsync<PostTagRow>(
            new CommandDefinition(
                TagsByPostIdsSql,
                new { p_post_ids = JoinCsv(postIds) },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)))
            .ToArray();

        return rows
            .GroupBy(row => row.PostId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(item => item.TagName).ToArray());
    }

    private static async Task ReplaceMediaAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long postId,
        IReadOnlyCollection<UnboxingWriteMediaCommandModel> media,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            DeleteMediaSql,
            new { p_post_id = postId },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        if (media.Count == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            InsertMediaSql,
            media.Select(item => new
            {
                p_post_id = postId,
                p_media_type = item.MediaType,
                p_storage_url = item.StorageUrl,
                p_thumbnail_url = item.ThumbnailUrl,
                p_sort_order = item.SortOrder,
                p_width = item.Width,
                p_height = item.Height
            }),
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    private static async Task ReplaceTagsAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long postId,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            DeleteTagsSql,
            new { p_post_id = postId },
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        if (tags.Count == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            InsertTagSql,
            tags.Select(tag => new
            {
                p_post_id = postId,
                p_tag_name = tag
            }),
            transaction: transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

    }

    private static string JoinCsv(IEnumerable<long> values)
    {
        return string.Join(',', values);
    }

    private class UnboxingListRow
    {
        public long PostId { get; init; }

        public string PostNo { get; init; } = string.Empty;

        public string? AuthorName { get; init; }

        public string? AuthorAvatarUrl { get; init; }

        public string? Title { get; init; }

        public string? BookTitle { get; init; }

        public string? ContentText { get; init; }

        public string? ProductLabel { get; init; }

        public string? LevelCode { get; init; }

        public string? LevelName { get; init; }

        public string? LevelIconUrl { get; init; }

        public string? CoverImageUrl { get; init; }

        public string? CoverThumbnailUrl { get; init; }

        public bool IsFeatured { get; init; }

        public DateTime PublishedAtUtc { get; init; }
    }

    private sealed class UnboxingDetailRow : UnboxingListRow
    {
    }

    private class AdminUnboxingListRow
    {
        public long PostId { get; init; }

        public string PostNo { get; init; } = string.Empty;

        public long UserId { get; init; }

        public string? AuthorName { get; init; }

        public string? AuthorAvatarUrl { get; init; }

        public string? Title { get; init; }

        public string? BookTitle { get; init; }

        public string? ProductLabel { get; init; }

        public int Status { get; init; }

        public string? LevelCode { get; init; }

        public string? LevelName { get; init; }

        public string? LevelIconUrl { get; init; }

        public string? CoverImageUrl { get; init; }

        public string? CoverThumbnailUrl { get; init; }

        public bool IsFeatured { get; init; }

        public DateTime? PublishedAtUtc { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime UpdatedAtUtc { get; init; }
    }

    private sealed class AdminUnboxingDetailRow : AdminUnboxingListRow
    {
        public string? ContentText { get; init; }
    }

    private sealed class UnboxingMediaRow
    {
        public string MediaType { get; init; } = string.Empty;

        public string StorageUrl { get; init; } = string.Empty;

        public string? ThumbnailUrl { get; init; }

        public int SortOrder { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }
    }

    private sealed record PostTagRow(
        long PostId,
        string TagName);

    private sealed class ExistingPostRow
    {
        public long PostId { get; init; }

        public long UserId { get; init; }
    }
}
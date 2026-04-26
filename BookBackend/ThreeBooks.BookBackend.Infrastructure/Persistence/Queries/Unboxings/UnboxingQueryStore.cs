using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Unboxings.Models;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Unboxings;

public sealed class UnboxingQueryStore(string connectionString) : IUnboxingQueryStore
{
    private const string UserExistsSql = """
        SELECT COUNT(*)
        FROM identity_user
        WHERE id = @UserId;
        """;

    private const string PostNoExistsSql = """
        SELECT COUNT(*)
        FROM community_unboxing_post
        WHERE post_no = @PostNo;
        """;

    private const string CurrentMaxNumericPostNoSql = """
        SELECT COALESCE(MAX(CAST(post_no AS UNSIGNED)), 40000)
        FROM community_unboxing_post
        WHERE post_no REGEXP '^[0-9]+$';
        """;

    private const string LevelByCodeSql = """
        SELECT
            level_code AS LevelCode,
            level_name AS LevelName,
            icon_url AS IconUrl
        FROM community_unboxing_level
        WHERE level_code = @LevelCode
        LIMIT 1;
        """;

    private const string ExistingPostSql = """
        SELECT
            id AS PostId,
            user_id AS UserId
        FROM community_unboxing_post
        WHERE post_no = @PostNo
        LIMIT 1;
        """;

    private const string InsertPostSql = """
        INSERT INTO community_unboxing_post (
            post_no,
            user_id,
            author_name,
            author_avatar,
            order_item_id,
            project_version_id,
            product_label,
            book_title,
            level_id,
            title,
            content_text,
            status,
            is_featured,
            published_at)
        VALUES (
            @PostNo,
            @UserId,
            @AuthorName,
            @AuthorAvatarUrl,
            @OrderItemId,
            @ProjectVersionId,
            @ProductLabel,
            @BookTitle,
            (SELECT id FROM community_unboxing_level WHERE level_code = @LevelCode LIMIT 1),
            @Title,
            @ContentText,
            @Status,
            @IsFeatured,
            @PublishedAtUtc);
        """;

    private const string UpdatePostSql = """
        UPDATE community_unboxing_post
        SET author_name = @AuthorName,
            author_avatar = @AuthorAvatarUrl,
            order_item_id = @OrderItemId,
            project_version_id = @ProjectVersionId,
            product_label = @ProductLabel,
            book_title = @BookTitle,
            level_id = (SELECT id FROM community_unboxing_level WHERE level_code = @LevelCode LIMIT 1),
            title = @Title,
            content_text = @ContentText,
            status = @Status,
            is_featured = @IsFeatured,
            published_at = @PublishedAtUtc,
            updated_at = CURRENT_TIMESTAMP
        WHERE post_no = @PostNo;
        """;

    private const string DeleteMediaSql = """
        DELETE FROM community_unboxing_media
        WHERE post_id = @PostId;
        """;

    private const string DeleteTagsSql = """
        DELETE FROM community_unboxing_tag
        WHERE post_id = @PostId;
        """;

    private const string InsertMediaSql = """
        INSERT INTO community_unboxing_media (
            post_id,
            media_type,
            storage_url,
            thumbnail_url,
            sort_order,
            width,
            height)
        VALUES (
            @PostId,
            @MediaType,
            @StorageUrl,
            @ThumbnailUrl,
            @SortOrder,
            @Width,
            @Height);
        """;

    private const string InsertTagSql = """
        INSERT INTO community_unboxing_tag (
            post_id,
            tag_name)
        VALUES (
            @PostId,
            @TagName);
        """;

    private const string CountSql = """
        SELECT COUNT(*)
        FROM community_unboxing_post post
        LEFT JOIN community_unboxing_level level ON level.id = post.level_id
        WHERE post.status = 1
          AND post.published_at IS NOT NULL
          AND (@LevelCode IS NULL OR level.level_code = @LevelCode)
          AND (@IsFeatured IS NULL OR post.is_featured = @IsFeatured);
        """;

    private const string ListSql = """
        SELECT
            post.id AS PostId,
            post.post_no AS PostNo,
            post.author_name AS AuthorName,
            post.author_avatar AS AuthorAvatarUrl,
            post.title AS Title,
            post.book_title AS BookTitle,
            post.content_text AS ContentText,
            post.product_label AS ProductLabel,
            post.is_featured AS IsFeatured,
            post.published_at AS PublishedAtUtc,
            level.level_code AS LevelCode,
            level.level_name AS LevelName,
            level.icon_url AS LevelIconUrl,
            cover.storage_url AS CoverImageUrl,
            cover.thumbnail_url AS CoverThumbnailUrl
        FROM community_unboxing_post post
        LEFT JOIN community_unboxing_level level ON level.id = post.level_id
        LEFT JOIN community_unboxing_media cover ON cover.id = (
            SELECT media.id
            FROM community_unboxing_media media
            WHERE media.post_id = post.id
            ORDER BY CASE WHEN media.media_type = 'image' THEN 0 ELSE 1 END, media.sort_order, media.id
            LIMIT 1
        )
        WHERE post.status = 1
          AND post.published_at IS NOT NULL
          AND (@LevelCode IS NULL OR level.level_code = @LevelCode)
          AND (@IsFeatured IS NULL OR post.is_featured = @IsFeatured)
        ORDER BY post.published_at DESC, post.id DESC
        LIMIT @PageSize OFFSET @Offset;
        """;

    private const string DetailSql = """
        SELECT
            post.id AS PostId,
            post.post_no AS PostNo,
            post.author_name AS AuthorName,
            post.author_avatar AS AuthorAvatarUrl,
            post.title AS Title,
            post.book_title AS BookTitle,
            post.content_text AS ContentText,
            post.product_label AS ProductLabel,
            post.is_featured AS IsFeatured,
            post.published_at AS PublishedAtUtc,
            level.level_code AS LevelCode,
            level.level_name AS LevelName,
            level.icon_url AS LevelIconUrl
        FROM community_unboxing_post post
        LEFT JOIN community_unboxing_level level ON level.id = post.level_id
        WHERE post.post_no = @PostNo
          AND post.status = 1
          AND post.published_at IS NOT NULL
        LIMIT 1;
        """;

    private const string MediaSql = """
        SELECT
            media_type AS MediaType,
            storage_url AS StorageUrl,
            thumbnail_url AS ThumbnailUrl,
            sort_order AS SortOrder,
            width AS Width,
            height AS Height
        FROM community_unboxing_media
        WHERE post_id = @PostId
        ORDER BY sort_order, id;
        """;

    private const string TagsByPostIdsSql = """
        SELECT
            post_id AS PostId,
            tag_name AS TagName
        FROM community_unboxing_tag
        WHERE post_id IN @PostIds
        ORDER BY id;
        """;

    private const string TagsByPostIdSql = """
        SELECT tag_name AS TagName
        FROM community_unboxing_tag
        WHERE post_id = @PostId
        ORDER BY id;
        """;

    private const string LevelsSql = """
        SELECT
            level_code AS LevelCode,
            level_name AS LevelName,
            icon_url AS IconUrl
        FROM community_unboxing_level
        ORDER BY sort_order, id;
        """;

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Unboxing database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<bool> UserExistsAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(UserExistsSql, new { UserId = userId }, cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<bool> PostNoExistsAsync(
        string postNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(PostNoExistsSql, new { PostNo = postNo }, cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<long> GetCurrentMaxNumericPostNoAsync(CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(CurrentMaxNumericPostNoSql, cancellationToken: cancellationToken));
    }

    public async Task<UnboxingLevelModel?> GetLevelByCodeAsync(
        string levelCode,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UnboxingLevelModel>(
            new CommandDefinition(LevelByCodeSql, new { LevelCode = levelCode }, cancellationToken: cancellationToken));
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
                command.PostNo,
                command.UserId,
                command.AuthorName,
                command.AuthorAvatarUrl,
                command.OrderItemId,
                command.ProjectVersionId,
                command.ProductLabel,
                command.BookTitle,
                command.LevelCode,
                command.Title,
                command.ContentText,
                command.Status,
                command.IsFeatured,
                command.PublishedAtUtc
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        var postId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT LAST_INSERT_ID();",
            transaction: transaction,
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
            new { PostNo = postNo },
            transaction: transaction,
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
                PostNo = postNo,
                command.AuthorName,
                command.AuthorAvatarUrl,
                command.OrderItemId,
                command.ProjectVersionId,
                command.ProductLabel,
                command.BookTitle,
                command.LevelCode,
                command.Title,
                command.ContentText,
                command.Status,
                command.IsFeatured,
                command.PublishedAtUtc
            },
            transaction: transaction,
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
            filter.LevelCode,
            filter.IsFeatured,
            filter.PageSize,
            Offset = (filter.PageNumber - 1) * filter.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CountSql, parameters, cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<UnboxingListRow>(
            new CommandDefinition(ListSql, parameters, cancellationToken: cancellationToken)))
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
            new CommandDefinition(DetailSql, new { PostNo = postNo }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var mediaRows = (await connection.QueryAsync<UnboxingMediaRow>(
            new CommandDefinition(MediaSql, new { PostId = row.PostId }, cancellationToken: cancellationToken)))
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
            new CommandDefinition(TagsByPostIdSql, new { PostId = row.PostId }, cancellationToken: cancellationToken)))
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
            new CommandDefinition(LevelsSql, cancellationToken: cancellationToken));

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
            new CommandDefinition(TagsByPostIdsSql, new { PostIds = postIds }, cancellationToken: cancellationToken)))
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
            new { PostId = postId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (media.Count == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            InsertMediaSql,
            media.Select(item => new
            {
                PostId = postId,
                item.MediaType,
                item.StorageUrl,
                item.ThumbnailUrl,
                item.SortOrder,
                item.Width,
                item.Height
            }),
            transaction: transaction,
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
            new { PostId = postId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (tags.Count == 0)
        {
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            InsertTagSql,
            tags.Select(tag => new
            {
                PostId = postId,
                TagName = tag
            }),
            transaction: transaction,
            cancellationToken: cancellationToken));
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
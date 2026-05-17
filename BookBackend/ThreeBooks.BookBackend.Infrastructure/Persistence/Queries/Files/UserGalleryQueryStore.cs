using System.Data;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Files;

public sealed class UserGalleryQueryStore(string connectionString) : IUserGalleryQueryStore
{
    private const string UserExistsSql = "usp_UserGallery_UserExists";
    private const string CountSql = "usp_UserGallery_CountImages";
    private const string ListSql = "usp_UserGallery_ListImages";
    private const string GetByIdSql = "usp_UserGallery_GetImageById";
    private const string GetByObjectSql = "usp_UserGallery_GetImageByObject";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("User gallery database connection string is required.", nameof(connectionString))
        : connectionString;

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

    public async Task<PagedResult<UserGalleryImageQueryModel>> GetListAsync(
        UserGalleryImageListFilter filter,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            p_directory_prefix = UserGalleryPathBuilder.BuildDirectoryPrefix(filter.UserId),
            p_page_size = filter.PageSize,
            p_offset = (filter.PageNumber - 1) * filter.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<UserGalleryImageRow>(
            new CommandDefinition(
                ListSql,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return new PagedResult<UserGalleryImageQueryModel>(
            rows.Select(MapItem).ToArray(),
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    public async Task<UserGalleryImageQueryModel?> GetByIdAsync(
        long userId,
        long fileId,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserGalleryImageRow>(
            new CommandDefinition(
                GetByIdSql,
                new
                {
                    p_file_id = fileId,
                    p_directory_prefix = UserGalleryPathBuilder.BuildDirectoryPrefix(userId)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return row is null ? null : MapItem(row);
    }

    public async Task<UserGalleryImageQueryModel?> GetByBucketObjectKeyAsync(
        long userId,
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserGalleryImageRow>(
            new CommandDefinition(
                GetByObjectSql,
                new
                {
                    p_bucket_name = bucket,
                    p_object_key = objectKey,
                    p_directory_prefix = UserGalleryPathBuilder.BuildDirectoryPrefix(userId)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return row is null ? null : MapItem(row);
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static UserGalleryImageQueryModel MapItem(UserGalleryImageRow row)
    {
        return new UserGalleryImageQueryModel(
            row.FileId,
            row.Bucket,
            row.ObjectKey,
            row.FileName,
            row.OriginalFileName,
            row.ContentType,
            row.ContentLength,
            row.UploadedAtUtc);
    }

    private sealed class UserGalleryImageRow
    {
        public long FileId { get; init; }

        public string Bucket { get; init; } = string.Empty;

        public string ObjectKey { get; init; } = string.Empty;

        public string FileName { get; init; } = string.Empty;

        public string? OriginalFileName { get; init; }

        public string? ContentType { get; init; }

        public long ContentLength { get; init; }

        public DateTime UploadedAtUtc { get; init; }
    }
}
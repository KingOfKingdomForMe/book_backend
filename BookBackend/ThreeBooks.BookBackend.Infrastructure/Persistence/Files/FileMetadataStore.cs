using System.Data;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Files;

public sealed class FileMetadataStore(string connectionString) : IFileMetadataStore
{
    private const string StorageProvider = "seaweedfs-s3";
    private const string PresignedReadOperation = "presigned_read";
    private const string GetFileMetadataSql = "usp_FileObject_GetMetadata";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("File metadata database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<StoredFileMetadata?> GetAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<StoredFileMetadata>(
            new CommandDefinition(
                GetFileMetadataSql,
                new
                {
                    p_bucket_name = bucket,
                    p_object_key = objectKey
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task SaveUploadAsync(
        StoredFileObject file,
        string originalFileName,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "usp_FileObject_SaveUpload",
                new
                {
                    p_bucket_name = file.Bucket,
                    p_object_key = file.ObjectKey,
                    p_directory_path = GetDirectoryPath(file.ObjectKey),
                    p_file_name = file.FileName,
                    p_original_file_name = NormalizeNullable(originalFileName),
                    p_file_extension = GetFileExtension(file.FileName),
                    p_content_type = NormalizeNullable(file.ContentType),
                    p_content_length = file.ContentLength,
                    p_storage_provider = StorageProvider,
                    p_client_ip = NormalizeNullable(context.IpAddress),
                    p_client_user_agent = NormalizeNullable(context.UserAgent)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task RecordAccessAsync(
        string bucket,
        string objectKey,
        DateTimeOffset expiresAtUtc,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(
            new CommandDefinition(
                "usp_FileObject_RecordAccess",
                new
                {
                    p_bucket_name = bucket,
                    p_object_key = objectKey,
                    p_operation_type = PresignedReadOperation,
                    p_access_url_expires_at = expiresAtUtc.UtcDateTime,
                    p_client_ip = NormalizeNullable(context.IpAddress),
                    p_client_user_agent = NormalizeNullable(context.UserAgent)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task MarkDeletedAsync(
        string bucket,
        string objectKey,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(
            new CommandDefinition(
                "usp_FileObject_MarkDeleted",
                new
                {
                    p_bucket_name = bucket,
                    p_object_key = objectKey,
                    p_client_ip = NormalizeNullable(context.IpAddress),
                    p_client_user_agent = NormalizeNullable(context.UserAgent)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string? GetDirectoryPath(string objectKey)
    {
        var lastSlashIndex = objectKey.LastIndexOf('/');
        return lastSlashIndex <= 0 ? null : objectKey[..lastSlashIndex];
    }

    private static string? GetFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        return extension.TrimStart('.').Trim().ToLowerInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Services;

public sealed class FileStorageService(IFileObjectStore objectStore) : IFileStorageService
{
    private const int DefaultPresignedUrlExpiresInMinutes = 60;
    private const int MinPresignedUrlExpiresInMinutes = 1;
    private const int MaxPresignedUrlExpiresInMinutes = 24 * 60;

    public async Task<UploadFileResponse> UploadAsync(
        UploadFileRequest request,
        Stream content,
        string originalFileName,
        string? contentType,
        long contentLength,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);

        if (contentLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contentLength), "Uploaded file cannot be empty.");
        }

        var fileName = NormalizeFileName(request.FileName, originalFileName);
        var objectKey = BuildObjectKey(request.Directory, request.ObjectKey, fileName);
        var bucket = NormalizeBucket(request.Bucket, objectStore.DefaultBucket);

        var stored = await objectStore.UploadAsync(
            new FileUploadCommand(
                bucket,
                objectKey,
                fileName,
                NormalizeContentType(contentType),
                contentLength,
                content),
            cancellationToken);

        return new UploadFileResponse(
            stored.Bucket,
            stored.ObjectKey,
            stored.FileName,
            stored.ContentType,
            stored.ContentLength);
    }

    public async Task<FileAccessUrlResponse?> GetAccessUrlAsync(
        GetFileAccessUrlRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bucket = NormalizeBucket(request.Bucket, objectStore.DefaultBucket);
        var objectKey = NormalizeObjectKey(request.ObjectKey);
        var expiresIn = TimeSpan.FromMinutes(NormalizeExpiresInMinutes(request.ExpiresInMinutes));
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(expiresIn);

        var url = await objectStore.GetReadUrlAsync(bucket, objectKey, expiresIn, cancellationToken);
        if (url is null)
        {
            return null;
        }

        return new FileAccessUrlResponse(
            bucket,
            objectKey,
            url.ToString(),
            expiresAtUtc);
    }

    public Task<bool> DeleteAsync(
        string? bucket,
        string objectKey,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return objectStore.DeleteAsync(
            NormalizeBucket(bucket, objectStore.DefaultBucket),
            NormalizeObjectKey(objectKey),
            cancellationToken);
    }

    private static int NormalizeExpiresInMinutes(int expiresInMinutes)
    {
        if (expiresInMinutes < MinPresignedUrlExpiresInMinutes)
        {
            return DefaultPresignedUrlExpiresInMinutes;
        }

        return expiresInMinutes > MaxPresignedUrlExpiresInMinutes
            ? MaxPresignedUrlExpiresInMinutes
            : expiresInMinutes;
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

    private static string NormalizeFileName(string? fileName, string originalFileName)
    {
        var candidate = string.IsNullOrWhiteSpace(fileName) ? originalFileName : fileName.Trim();
        var normalized = Path.GetFileName(candidate);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("A valid file name is required.", nameof(fileName));
        }

        return normalized;
    }

    private static string NormalizeContentType(string? contentType)
    {
        var normalized = contentType?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "application/octet-stream" : normalized;
    }

    private static string BuildObjectKey(string? directory, string? objectKey, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(objectKey))
        {
            return NormalizeObjectKey(objectKey);
        }

        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var generatedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var prefix = string.IsNullOrWhiteSpace(directory) ? null : NormalizeObjectKey(directory);

        return string.IsNullOrWhiteSpace(prefix)
            ? $"{datePath}/{generatedFileName}"
            : $"{prefix}/{datePath}/{generatedFileName}";
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
}
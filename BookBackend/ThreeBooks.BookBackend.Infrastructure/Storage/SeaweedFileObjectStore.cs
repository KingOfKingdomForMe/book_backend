using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Infrastructure.Storage.Options;

namespace ThreeBooks.BookBackend.Infrastructure.Storage;

public sealed class SeaweedFileObjectStore(ObjectStorageOptions options) : IFileObjectStore
{
    private readonly ObjectStorageOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public string? DefaultBucket => string.IsNullOrWhiteSpace(_options.DefaultBucket)
        ? null
        : _options.DefaultBucket.Trim().ToLowerInvariant();

    public async Task<StoredFileObject> UploadAsync(
        FileUploadCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var client = CreateClient();
        await EnsureBucketExistsAsync(client, command.Bucket, cancellationToken);

        if (command.Content.CanSeek)
        {
            command.Content.Position = 0;
        }

        var request = new PutObjectRequest
        {
            BucketName = command.Bucket,
            Key = command.ObjectKey,
            InputStream = command.Content,
            AutoCloseStream = false,
            AutoResetStreamPosition = false,
            ContentType = command.ContentType
        };

        await client.PutObjectAsync(request, cancellationToken);

        return new StoredFileObject(
            command.Bucket,
            command.ObjectKey,
            command.FileName,
            command.ContentType,
            command.ContentLength);
    }

    public async Task<Uri?> GetReadUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();

        var exists = await ObjectExistsAsync(client, bucket, objectKey, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET,
            Protocol = ResolveProtocol()
        };

        return new Uri(client.GetPreSignedURL(request));
    }

    public async Task<StoredFileContent?> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        var client = CreateClient();

        try
        {
            var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucket,
                Key = objectKey
            }, cancellationToken);

            var fileName = Path.GetFileName(objectKey);

            return new StoredFileContent(
                response.ResponseStream,
                string.IsNullOrWhiteSpace(response.Headers.ContentType)
                    ? "application/octet-stream"
                    : response.Headers.ContentType,
                response.Headers.ContentLength,
                string.IsNullOrWhiteSpace(fileName) ? objectKey : fileName,
                new CompositeDisposable(response, client));
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == System.Net.HttpStatusCode.NotFound
            || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "NoSuchBucket", StringComparison.OrdinalIgnoreCase))
        {
            client.Dispose();
            return null;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();

        var exists = await ObjectExistsAsync(client, bucket, objectKey, cancellationToken);
        if (!exists)
        {
            return false;
        }

        await client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = objectKey
        }, cancellationToken);

        return true;
    }

    private AmazonS3Client CreateClient()
    {
        var serviceUrl = _options.ServiceUrl?.Trim();
        var accessKey = _options.AccessKey?.Trim();
        var secretKey = _options.SecretKey?.Trim();

        if (string.IsNullOrWhiteSpace(serviceUrl)
            || string.IsNullOrWhiteSpace(accessKey)
            || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "ObjectStorage configuration is incomplete. Configure ObjectStorage:ServiceUrl, AccessKey and SecretKey before using file endpoints.");
        }

        if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var endpointUri))
        {
            throw new InvalidOperationException("ObjectStorage:ServiceUrl must be a valid absolute URL.");
        }

        var config = new AmazonS3Config
        {
            ServiceURL = endpointUri.ToString(),
            ForcePathStyle = _options.ForcePathStyle,
            UseHttp = endpointUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        };

        return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
    }

    private async Task EnsureBucketExistsAsync(IAmazonS3 client, string bucket, CancellationToken cancellationToken)
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket);
        if (exists)
        {
            return;
        }

        await client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucket,
            UseClientRegion = true
        }, cancellationToken);
    }

    private static async Task<bool> ObjectExistsAsync(
        IAmazonS3 client,
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = objectKey
            }, cancellationToken);

            return true;
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == System.Net.HttpStatusCode.NotFound
            || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    private Protocol ResolveProtocol()
    {
        var serviceUrl = _options.ServiceUrl?.Trim();
        if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var endpointUri))
        {
            return Protocol.HTTP;
        }

        return endpointUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? Protocol.HTTPS
            : Protocol.HTTP;
    }

    private sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
    {
        private readonly IDisposable[] _disposables = disposables;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
namespace ThreeBooks.BookBackend.Infrastructure.Storage.Options;

public sealed class ObjectStorageOptions
{
    public string? ServiceUrl { get; set; }

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string? DefaultBucket { get; set; }

    public bool ForcePathStyle { get; set; } = true;

    public int DefaultPresignedUrlExpiresInMinutes { get; set; } = 60;
}
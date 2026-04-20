namespace ThreeBooks.BookBackend.Infrastructure.Storage.Options;

public sealed class ObjectStorageOptions
{
    public string? ServiceUrl { get; set; }

    public string? AccessKey { get; set; } = "admin";

    public string? SecretKey { get; set; } = "admin";

    public string? DefaultBucket { get; set; }

    public bool ForcePathStyle { get; set; } = true;

    public int DefaultPresignedUrlExpiresInMinutes { get; set; } = 60;
}
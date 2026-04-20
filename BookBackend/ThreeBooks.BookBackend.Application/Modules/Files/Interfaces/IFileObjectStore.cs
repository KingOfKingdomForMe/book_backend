using ThreeBooks.BookBackend.Application.Modules.Files.Models;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;

public interface IFileObjectStore
{
    string? DefaultBucket { get; }

    Task<StoredFileObject> UploadAsync(
        FileUploadCommand command,
        CancellationToken cancellationToken);

    Task<Uri?> GetReadUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken);

    Task<StoredFileContent?> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken);
}
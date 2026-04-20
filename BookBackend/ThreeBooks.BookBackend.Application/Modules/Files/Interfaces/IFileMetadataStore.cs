using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;

public interface IFileMetadataStore
{
    Task<StoredFileMetadata?> GetAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken);

    Task SaveUploadAsync(
        StoredFileObject file,
        string originalFileName,
        RequestContext context,
        CancellationToken cancellationToken);

    Task RecordAccessAsync(
        string bucket,
        string objectKey,
        DateTimeOffset expiresAtUtc,
        RequestContext context,
        CancellationToken cancellationToken);

    Task MarkDeletedAsync(
        string bucket,
        string objectKey,
        RequestContext context,
        CancellationToken cancellationToken);
}
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Files.Models;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;

public interface IFileStorageService
{
    Task<UploadFileResponse> UploadAsync(
        UploadFileRequest request,
        Stream content,
        string originalFileName,
        string? contentType,
        long contentLength,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<FileAccessUrlResponse?> GetAccessUrlAsync(
        GetFileAccessUrlRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<StoredFileContent?> DownloadAsync(
        string? bucket,
        string objectKey,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        string? bucket,
        string objectKey,
        RequestContext context,
        CancellationToken cancellationToken);
}
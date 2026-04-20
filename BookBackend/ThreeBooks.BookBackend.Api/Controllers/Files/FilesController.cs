using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Files;

[Route("api/files")]
public sealed class FilesController(IFileStorageService fileStorageService) : ApiControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadFileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UploadFileResponse>> UploadAsync(
        [FromForm] UploadFileForm form,
        CancellationToken cancellationToken)
    {
        var file = form.File;

        if (file is null)
        {
            return BadRequest("Form field 'file' is required.");
        }

        if (file.Length <= 0)
        {
            return BadRequest("Uploaded file cannot be empty.");
        }

        try
        {
            await using var stream = file.OpenReadStream();

            var response = await fileStorageService.UploadAsync(
                new UploadFileRequest(form.Bucket, form.Directory, form.ObjectKey, form.FileName),
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                BuildRequestContext(),
                cancellationToken);

            var accessUrl = await fileStorageService.GetAccessUrlAsync(
                new GetFileAccessUrlRequest(response.Bucket, response.ObjectKey),
                BuildRequestContext(),
                cancellationToken);

            return Ok(response with
            {
                ProxyUrl = BuildProxyUrl(response.Bucket, response.ObjectKey),
                AccessUrl = accessUrl is null ? null : BuildAbsoluteProxyUrl(response.Bucket, response.ObjectKey)
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "File storage is not configured.");
        }
    }

    [HttpGet("access-url")]
    [ProducesResponseType<FileAccessUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FileAccessUrlResponse>> GetAccessUrlAsync(
        [FromQuery] GetFileAccessUrlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await fileStorageService.GetAccessUrlAsync(request, BuildRequestContext(), cancellationToken);
            if (response is null)
            {
                return NotFound();
            }

            return Ok(response with
            {
                Url = BuildAbsoluteProxyUrl(response.Bucket, response.ObjectKey)
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "File storage is not configured.");
        }
    }

    [HttpGet("content/{bucket}/{**objectKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RedirectToContentAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await fileStorageService.DownloadAsync(bucket, objectKey, BuildRequestContext(), cancellationToken);

            if (response is null)
            {
                return NotFound();
            }

            if (response.Lease is not null)
            {
                HttpContext.Response.RegisterForDispose(response.Lease);
            }

            if (response.ContentLength.HasValue)
            {
                HttpContext.Response.ContentLength = response.ContentLength.Value;
            }

            HttpContext.Response.Headers.ContentDisposition = BuildContentDisposition(
                response.FileName,
                IsInlinePreviewable(response.ContentType));

            return File(
                response.Content,
                response.ContentType ?? "application/octet-stream",
                enableRangeProcessing: true);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "File storage is not configured.");
        }
    }

    [HttpDelete("{bucket}/{**objectKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await fileStorageService.DeleteAsync(bucket, objectKey, BuildRequestContext(), cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "File storage is not configured.");
        }
    }

    private string BuildProxyUrl(string bucket, string objectKey)
    {
        return $"/api/files/content/{Uri.EscapeDataString(bucket)}/{EncodeObjectKeyForPath(objectKey)}";
    }

    private string BuildAbsoluteProxyUrl(string bucket, string objectKey)
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}{BuildProxyUrl(bucket, objectKey)}";
    }

    private static string BuildContentDisposition(string fileName, bool inline)
    {
        var disposition = new ContentDispositionHeaderValue(inline ? "inline" : "attachment")
        {
            FileNameStar = fileName,
            FileName = fileName
        };

        return disposition.ToString();
    }

    private static bool IsInlinePreviewable(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string EncodeObjectKeyForPath(string objectKey)
    {
        return string.Join(
            '/',
            objectKey
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }
}
using Microsoft.AspNetCore.Mvc;
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
        [FromForm] UploadFileRequest request,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
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
                request,
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
                AccessUrl = accessUrl?.Url
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

            return Ok(response);
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
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RedirectToContentAsync(
        string bucket,
        string objectKey,
        [FromQuery] int expiresInMinutes = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await fileStorageService.GetAccessUrlAsync(
                new GetFileAccessUrlRequest(bucket, objectKey, expiresInMinutes),
                BuildRequestContext(),
                cancellationToken);

            if (response is null)
            {
                return NotFound();
            }

            return Redirect(response.Url);
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

    private static string EncodeObjectKeyForPath(string objectKey)
    {
        return string.Join(
            '/',
            objectKey
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
    }
}
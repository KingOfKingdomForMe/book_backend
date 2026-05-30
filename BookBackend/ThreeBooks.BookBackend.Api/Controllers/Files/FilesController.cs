using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ThreeBooks.BookBackend.Application.Modules.Files.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Files.Requests;
using ThreeBooks.BookBackend.Contracts.Files.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Files;

/// <summary>
/// 文件上传、访问与用户图库接口。
/// </summary>
[Route("api/files")]
public sealed class FilesController(
    IFileStorageService fileStorageService,
    IUserGalleryService userGalleryService) : ApiControllerBase
{
    /// <summary>
    /// 上传通用文件到对象存储。
    /// </summary>
    /// <remarks>
    /// 使用 `multipart/form-data` 提交，文件字段名固定为 `file`。若同时传入 `Bucket`、`Directory` 和 `ObjectKey`，服务端会优先按指定位置存储。
    /// </remarks>
    /// <param name="form">文件上传表单，包含目标桶、目录、对象键、自定义文件名和文件内容。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">上传成功，并返回文件标识与可访问地址。</response>
    /// <response code="400">上传表单不完整或文件内容为空。</response>
    /// <response code="503">底层文件存储当前不可用。</response>
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
                title: "File storage is unavailable.");
        }
    }

    /// <summary>
    /// 上传用户图库图片。
    /// </summary>
    /// <remarks>
    /// 该接口适用于编辑器素材库。建议仅上传图片文件，并在上传成功后使用返回的 `fileId` 关联相册页面或封面素材。
    /// </remarks>
    /// <param name="userId">业务用户标识。</param>
    /// <param name="form">图库图片上传表单，包含可选桶名、自定义文件名和图片文件内容。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">上传成功，并返回图库图片信息。</response>
    /// <response code="400">上传参数不合法或文件为空。</response>
    /// <response code="503">文件存储服务不可用。</response>
    [HttpPost("users/{userId:long}/gallery/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UserGalleryImageItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UserGalleryImageItemResponse>> UploadUserGalleryImageAsync(
        long userId,
        [FromForm] UploadUserGalleryImageForm form,
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

            var response = await userGalleryService.UploadImageAsync(
                userId,
                new UploadUserGalleryImageRequest(form.Bucket, form.FileName),
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                BuildRequestContext(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, PopulateGalleryUrls(response));
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
                title: "File storage is unavailable.");
        }
    }

    /// <summary>
    /// 分页获取用户图库图片。
    /// </summary>
    /// <remarks>
    /// 适用于编辑器图库弹窗。返回结果中的 `ProxyUrl` 与 `AccessUrl` 可直接用于图片预览。
    /// </remarks>
    /// <param name="userId">业务用户标识。</param>
    /// <param name="request">图库列表查询参数，包含分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页图库图片列表。</response>
    /// <response code="400">请求参数不合法。</response>
    [HttpGet("users/{userId:long}/gallery/images")]
    [ProducesResponseType<PagedResult<UserGalleryImageItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserGalleryImageItemResponse>>> GetUserGalleryImagesAsync(
        long userId,
        [FromQuery] ListUserGalleryImagesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await userGalleryService.GetImagesAsync(userId, request, BuildRequestContext(), cancellationToken);
            return Ok(PopulateGalleryUrls(response));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 删除用户图库中的一张图片。
    /// </summary>
    /// <remarks>
    /// 删除后对应素材通常无法再用于相册或封面编辑，建议客户端在执行前做二次确认。
    /// </remarks>
    /// <param name="userId">业务用户标识。</param>
    /// <param name="fileId">图库文件标识。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">删除成功。</response>
    /// <response code="404">目标文件不存在。</response>
    /// <response code="400">请求参数不合法。</response>
    /// <response code="503">文件存储服务不可用。</response>
    [HttpDelete("users/{userId:long}/gallery/images/{fileId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteUserGalleryImageAsync(
        long userId,
        long fileId,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await userGalleryService.DeleteImageAsync(userId, fileId, BuildRequestContext(), cancellationToken);
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
                title: "File storage is unavailable.");
        }
    }

    /// <summary>
    /// 获取文件访问地址。
    /// </summary>
    /// <remarks>
    /// 适合在前端需要短期可访问链接时调用。当前接口会优先返回经过本服务代理的绝对地址，便于统一鉴权和链路控制。
    /// </remarks>
    /// <param name="request">访问地址查询参数，包含桶名、对象键和链接过期时间。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回可访问文件地址。</response>
    /// <response code="404">目标文件不存在。</response>
    /// <response code="400">对象键或过期时间不合法。</response>
    /// <response code="503">文件存储服务不可用。</response>
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
                title: "File storage is unavailable.");
        }
    }

    /// <summary>
    /// 通过本服务代理读取文件内容。
    /// </summary>
    /// <remarks>
    /// 适用于浏览器直接预览图片或 PDF。返回内容支持 Range 请求，便于大文件断点续传和视频音频分段读取。
    /// </remarks>
    /// <param name="bucket">对象存储桶名称。</param>
    /// <param name="objectKey">对象键，支持多级路径。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">成功返回文件二进制内容。</response>
    /// <response code="404">目标文件不存在。</response>
    /// <response code="503">文件存储服务不可用。</response>
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
                title: "File storage is unavailable.");
        }
    }

    /// <summary>
    /// 删除对象存储中的指定文件。
    /// </summary>
    /// <remarks>
    /// 建议仅在确认文件不再被业务引用时调用。对于用户图库文件，优先使用图库删除接口以保持元数据与存储一致。
    /// </remarks>
    /// <param name="bucket">对象存储桶名称。</param>
    /// <param name="objectKey">对象键，支持多级路径。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">删除成功。</response>
    /// <response code="404">目标文件不存在。</response>
    /// <response code="400">桶名或对象键不合法。</response>
    /// <response code="503">文件存储服务不可用。</response>
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
                title: "File storage is unavailable.");
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

    private UserGalleryImageItemResponse PopulateGalleryUrls(UserGalleryImageItemResponse item)
    {
        var proxyUrl = BuildProxyUrl(item.Bucket, item.ObjectKey);

        return item with
        {
            ProxyUrl = proxyUrl,
            AccessUrl = BuildAbsoluteProxyUrl(item.Bucket, item.ObjectKey)
        };
    }

    private PagedResult<UserGalleryImageItemResponse> PopulateGalleryUrls(PagedResult<UserGalleryImageItemResponse> result)
    {
        return result with
        {
            Items = result.Items.Select(PopulateGalleryUrls).ToArray()
        };
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
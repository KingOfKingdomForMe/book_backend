namespace ThreeBooks.BookBackend.Api.Controllers.Files;

/// <summary>
/// 用户图库图片上传表单。
/// </summary>
public sealed class UploadUserGalleryImageForm
{
    /// <summary>
    /// 目标存储桶名称。
    /// </summary>
    public string? Bucket { get; init; }

    /// <summary>
    /// 自定义文件名。
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// 实际上传的图片文件，字段名固定为 `file`。
    /// </summary>
    public IFormFile? File { get; init; }
}
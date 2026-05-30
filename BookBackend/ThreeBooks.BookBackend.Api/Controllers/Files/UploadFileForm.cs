namespace ThreeBooks.BookBackend.Api.Controllers.Files;

/// <summary>
/// 通用文件上传表单。
/// </summary>
public sealed class UploadFileForm
{
    /// <summary>
    /// 目标存储桶名称。
    /// </summary>
    public string? Bucket { get; init; }

    /// <summary>
    /// 目标目录。
    /// </summary>
    public string? Directory { get; init; }

    /// <summary>
    /// 对象键，适合显式指定完整存储路径。
    /// </summary>
    public string? ObjectKey { get; init; }

    /// <summary>
    /// 自定义文件名。
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// 实际上传的文件内容，字段名固定为 `file`。
    /// </summary>
    public IFormFile? File { get; init; }
}
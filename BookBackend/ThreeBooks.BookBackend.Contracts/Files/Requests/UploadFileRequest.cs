namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

/// <summary>
/// 通用文件上传请求参数。
/// </summary>
/// <param name="Bucket">目标存储桶名称。</param>
/// <param name="Directory">目标目录。</param>
/// <param name="ObjectKey">对象键，传入后可精确控制存储路径。</param>
/// <param name="FileName">自定义文件名。</param>
public sealed record UploadFileRequest(
    string? Bucket,
    string? Directory,
    string? ObjectKey,
    string? FileName);
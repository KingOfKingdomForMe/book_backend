namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

/// <summary>
/// 用户图库图片上传请求参数。
/// </summary>
/// <param name="Bucket">目标存储桶名称。</param>
/// <param name="FileName">自定义文件名。</param>
public sealed record UploadUserGalleryImageRequest(
    string? Bucket,
    string? FileName = null);
namespace ThreeBooks.BookBackend.Contracts.Files.Requests;

/// <summary>
/// 文件访问地址查询参数。
/// </summary>
/// <param name="Bucket">文件所在存储桶名称。</param>
/// <param name="ObjectKey">对象键。</param>
/// <param name="ExpiresInMinutes">访问地址有效期，单位分钟。</param>
public sealed record GetFileAccessUrlRequest(
    string? Bucket,
    string ObjectKey,
    int ExpiresInMinutes = 60);
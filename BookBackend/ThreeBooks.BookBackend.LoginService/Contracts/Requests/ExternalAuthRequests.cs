using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Contracts.Requests;

/// <summary>
/// 发起微信授权或绑定流程的请求参数。
/// </summary>
/// <param name="RedirectUri">微信授权完成后跳转或回调的地址，未传时由服务端使用默认配置。</param>
public sealed record StartWeChatLoginRequest(string? RedirectUri = null);

/// <summary>
/// 完成外部登录的请求参数。
/// </summary>
/// <param name="SessionId">外部认证会话标识。</param>
/// <param name="CompletionToken">会话完成令牌，用于校验调用方是否有权完成登录。</param>
public sealed record CompleteExternalLoginRequest(Guid SessionId, string CompletionToken);

/// <summary>
/// 微信授权回调透传参数。
/// </summary>
/// <param name="Code">微信授权成功后返回的授权码。</param>
/// <param name="State">授权发起时透传的状态值。</param>
/// <param name="Error">微信返回的错误码。</param>
/// <param name="ErrorDescription">微信返回的错误描述。</param>
public sealed record ExternalAuthCallbackRequest(string? Code, string? State, string? Error = null, string? ErrorDescription = null);

/// <summary>
/// 将外部身份绑定到当前账号的请求参数。
/// </summary>
/// <param name="SessionId">外部认证会话标识。</param>
/// <param name="CompletionToken">允许完成绑定的会话令牌。</param>
public sealed record BindExternalIdentityRequest(Guid SessionId, string CompletionToken);

/// <summary>
/// 查询外部登录状态的请求参数。
/// </summary>
/// <param name="SessionId">外部认证会话标识。</param>
/// <param name="CompletionToken">用于轮询与后续完成操作的令牌。</param>
public sealed record ExternalLoginStatusRequest(Guid SessionId, string CompletionToken);
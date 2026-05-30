namespace ThreeBooks.BookBackend.LoginService.Contracts.Requests;

/// <summary>
/// 注册本地账号的请求参数。
/// </summary>
/// <param name="Username">登录用户名，建议使用稳定且可读的唯一值。</param>
/// <param name="DisplayName">用户展示名称，可为空。</param>
/// <param name="Mobile">用户手机号，可用于后续登录或找回流程。</param>
/// <param name="Password">用户明文密码，需满足服务端密码策略。</param>
public sealed record RegisterRequest(
    string Username,
    string? DisplayName,
    string? Mobile,
    string Password);

/// <summary>
/// 使用账号密码登录的请求参数。
/// </summary>
/// <param name="Identity">登录标识，通常为用户名或手机号。</param>
/// <param name="Password">与登录标识对应的密码。</param>
public sealed record PasswordLoginRequest(
    string Identity,
    string Password);

/// <summary>
/// 刷新访问令牌的请求参数。
/// </summary>
/// <param name="RefreshToken">当前仍然有效的刷新令牌。</param>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// 注销会话的请求参数。
/// </summary>
/// <param name="RefreshToken">需要立即失效的刷新令牌。</param>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>
/// 修改当前用户密码的请求参数。
/// </summary>
/// <param name="CurrentPassword">当前生效的旧密码。</param>
/// <param name="NewPassword">准备设置的新密码。</param>
/// <param name="ConfirmNewPassword">新密码确认值，应与 `NewPassword` 完全一致。</param>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
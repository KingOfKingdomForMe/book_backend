using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

/// <summary>
/// 认证、令牌续期与微信外部登录相关接口。
/// </summary>
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IExternalAuthService externalAuthService) : ApiControllerBase
{
    /// <summary>
    /// 注册新账号并直接返回登录令牌。
    /// </summary>
    /// <remarks>
    /// 适用于首次创建本地账号。建议在客户端先完成基础字段校验，注册成功后直接保存返回的访问令牌与刷新令牌。
    /// </remarks>
    /// <param name="request">注册请求体。包含登录用户名、显示名称、手机号和明文密码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">注册成功，并返回当前会话的访问令牌与刷新令牌。</response>
    /// <response code="400">请求参数不合法，例如用户名或密码不满足约束。</response>
    /// <response code="409">用户名或手机号已存在，无法重复注册。</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthenticationResponse>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 使用账号密码登录并换取访问令牌。
    /// </summary>
    /// <remarks>
    /// `Identity` 可以是系统支持的登录标识，通常为用户名或手机号。建议客户端在收到 401 或 423 时分别提示“凭证错误”和“账户已锁定”。
    /// </remarks>
    /// <param name="request">密码登录请求体。`Identity` 为登录标识，`Password` 为当前密码。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">登录成功，并返回访问令牌与刷新令牌。</response>
    /// <response code="401">账号或密码错误。</response>
    /// <response code="423">账户因安全策略被锁定。</response>
    [AllowAnonymous]
    [HttpPost("login/password")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthenticationResponse>> LoginByPasswordAsync(
        [FromBody] PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginByPasswordAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 使用刷新令牌续期登录状态。
    /// </summary>
    /// <remarks>
    /// 建议在访问令牌即将过期时调用，而不是每次请求都调用。续期成功后应覆盖本地保存的旧刷新令牌。
    /// </remarks>
    /// <param name="request">续期请求体，包含当前有效的刷新令牌。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">续期成功，并返回新的令牌对。</response>
    /// <response code="401">刷新令牌无效、已吊销或已过期。</response>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 注销当前会话使用的刷新令牌。
    /// </summary>
    /// <remarks>
    /// 建议在用户主动退出登录时调用，同时清理客户端保存的访问令牌与刷新令牌。
    /// </remarks>
    /// <param name="request">注销请求体，包含需要失效的刷新令牌。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">注销成功，令牌已失效。</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(GetRequiredUserId(), request, BuildRequestContext(), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 修改当前登录用户的密码。
    /// </summary>
    /// <remarks>
    /// `NewPassword` 与 `ConfirmNewPassword` 应保持一致。修改成功后，建议客户端提示用户重新登录或刷新本地凭证。
    /// </remarks>
    /// <param name="request">修改密码请求体。包含旧密码、新密码和新密码确认值。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">密码修改成功。</response>
    /// <response code="400">请求数据不合法，例如新密码确认不一致。</response>
    /// <response code="401">当前访问令牌无效或旧密码校验失败。</response>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(GetRequiredUserId(), request, BuildRequestContext(), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 发起微信扫码登录流程。
    /// </summary>
    /// <remarks>
    /// 成功后会返回二维码会话信息。前端应轮询会话状态接口，待扫码授权完成后再调用完成登录接口换取本系统令牌。
    /// </remarks>
    /// <param name="request">扫码登录启动参数。`RedirectUri` 可指定微信授权完成后的回调地址。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">成功创建扫码登录会话。</response>
    [AllowAnonymous]
    [HttpPost("wechat/qr/start")]
    [ProducesResponseType<StartExternalAuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StartExternalAuthResponse>> StartQrLoginAsync(
        [FromBody] StartWeChatLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await externalAuthService.StartWeChatQrLoginAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 处理微信扫码登录回调。
    /// </summary>
    /// <remarks>
    /// 该接口通常由微信开放平台回调，不建议业务前端直接调用。若微信回调失败，可通过 `error` 与 `error_description` 排查原因。
    /// </remarks>
    /// <param name="code">微信授权成功后返回的授权码。</param>
    /// <param name="state">发起授权时透传的状态值，用于防重放与会话匹配。</param>
    /// <param name="error">微信返回的错误码。</param>
    /// <param name="errorDescription">驼峰格式的错误描述参数。</param>
    /// <param name="errorDescriptionSnakeCase">下划线格式的错误描述参数，兼容微信标准字段名。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">回调处理完成，返回外部登录会话状态。</response>
    [AllowAnonymous]
    [HttpGet("wechat/qr/callback")]
    [ProducesResponseType<ExternalAuthCallbackResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExternalAuthCallbackResponse>> HandleQrCallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "errorDescription")] string? errorDescription,
        [FromQuery(Name = "error_description")] string? errorDescriptionSnakeCase,
        CancellationToken cancellationToken)
    {
        var request = new ExternalAuthCallbackRequest(code, state, error, errorDescription ?? errorDescriptionSnakeCase);
        var response = await externalAuthService.HandleWeChatCallbackAsync(
            AuthProvider.WeChatQr,
            request,
            BuildRequestContext(),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// 发起当前用户绑定微信公众号身份的流程。
    /// </summary>
    /// <remarks>
    /// 该接口需要用户已登录。常见用法是先调用本接口获取授权地址，再引导用户到微信完成绑定。
    /// </remarks>
    /// <param name="request">微信公众号绑定启动参数。`RedirectUri` 用于接收绑定完成后的回调。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">成功创建公众号绑定会话。</response>
    [Authorize]
    [HttpPost("wechat/official-account/bind/start")]
    [ProducesResponseType<StartExternalAuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StartExternalAuthResponse>> StartOfficialAccountBindAsync(
        [FromBody] StartWeChatLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await externalAuthService.StartWeChatOfficialAccountBindAsync(
            GetRequiredUserId(),
            request,
            BuildRequestContext(),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// 处理微信公众号绑定或登录回调。
    /// </summary>
    /// <remarks>
    /// 该接口主要供微信服务器回调。客户端应结合会话状态接口确认授权是否成功，再执行绑定或登录完成动作。
    /// </remarks>
    /// <param name="code">微信授权成功后返回的授权码。</param>
    /// <param name="state">发起授权时生成的状态值。</param>
    /// <param name="error">微信返回的错误码。</param>
    /// <param name="errorDescription">驼峰格式的错误描述。</param>
    /// <param name="errorDescriptionSnakeCase">下划线格式的错误描述。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">回调处理完成，返回外部认证会话结果。</response>
    [AllowAnonymous]
    [HttpGet("wechat/official-account/callback")]
    [ProducesResponseType<ExternalAuthCallbackResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExternalAuthCallbackResponse>> HandleOfficialAccountCallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "errorDescription")] string? errorDescription,
        [FromQuery(Name = "error_description")] string? errorDescriptionSnakeCase,
        CancellationToken cancellationToken)
    {
        var request = new ExternalAuthCallbackRequest(code, state, error, errorDescription ?? errorDescriptionSnakeCase);
        var response = await externalAuthService.HandleWeChatCallbackAsync(
            AuthProvider.WeChatOfficialAccount,
            request,
            BuildRequestContext(),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// 查询微信外部认证会话状态。
    /// </summary>
    /// <remarks>
    /// 建议前端在扫码或跳转授权后轮询此接口。`completionToken` 是完成登录或绑定动作时的短期凭证，应妥善保存。
    /// </remarks>
    /// <param name="sessionId">外部认证会话标识。</param>
    /// <param name="completionToken">会话完成令牌，用于确认当前轮询方具备后续完成操作权限。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回当前会话状态、外部身份信息和是否可继续下一步。</response>
    [AllowAnonymous]
    [HttpGet("wechat/sessions/{sessionId:guid}")]
    [ProducesResponseType<ExternalAuthSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExternalAuthSessionResponse>> GetSessionStatusAsync(
        [FromRoute] Guid sessionId,
        [FromQuery] string completionToken,
        CancellationToken cancellationToken)
    {
        var response = await externalAuthService.GetSessionStatusAsync(sessionId, completionToken, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 完成微信登录并签发本系统令牌。
    /// </summary>
    /// <remarks>
    /// 应在外部认证会话状态变为可完成后调用。调用成功后即可把该用户视为已登录。
    /// </remarks>
    /// <param name="request">完成登录请求体，包含会话标识与完成令牌。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">登录完成，返回访问令牌与刷新令牌。</response>
    [AllowAnonymous]
    [HttpPost("wechat/sessions/complete")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResponse>> CompleteLoginAsync(
        [FromBody] CompleteExternalLoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await externalAuthService.CompleteLoginAsync(
            request.SessionId,
            request.CompletionToken,
            BuildRequestContext(),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// 将外部身份绑定到当前已登录用户。
    /// </summary>
    /// <remarks>
    /// 通常用于用户在个人中心绑定微信。前提是外部认证流程已经完成且会话状态允许绑定。
    /// </remarks>
    /// <param name="request">绑定请求体，包含外部认证会话标识与完成令牌。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">绑定成功，返回更新后的外部认证会话信息。</response>
    [Authorize]
    [HttpPost("wechat/sessions/bind-current-user")]
    [ProducesResponseType<ExternalAuthSessionResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExternalAuthSessionResponse>> BindCurrentUserAsync(
        [FromBody] BindExternalIdentityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await externalAuthService.BindCurrentUserAsync(
            request.SessionId,
            request.CompletionToken,
            GetRequiredUserId(),
            cancellationToken);

        return Ok(response);
    }
}
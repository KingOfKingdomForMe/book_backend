using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IExternalAuthService externalAuthService) : ApiControllerBase
{
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
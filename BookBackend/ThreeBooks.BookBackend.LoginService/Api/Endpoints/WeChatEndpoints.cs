using Microsoft.AspNetCore.Authorization;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;
using ThreeBooks.BookBackend.LoginService.Extensions;

namespace ThreeBooks.BookBackend.LoginService.Api.Endpoints;

public static class WeChatEndpoints
{
    public static IEndpointRouteBuilder MapWeChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth/wechat").WithTags("WeChat");

        group.MapPost("/qr/start", StartQrLoginAsync)
            .AllowAnonymous()
            .Produces<StartExternalAuthResponse>(StatusCodes.Status200OK);

        group.MapGet("/qr/callback", HandleQrCallbackAsync)
            .AllowAnonymous()
            .Produces<ExternalAuthCallbackResponse>(StatusCodes.Status200OK);

        group.MapPost("/official-account/bind/start", StartOfficialAccountBindAsync)
            .RequireAuthorization()
            .Produces<StartExternalAuthResponse>(StatusCodes.Status200OK);

        group.MapGet("/official-account/callback", HandleOfficialAccountCallbackAsync)
            .AllowAnonymous()
            .Produces<ExternalAuthCallbackResponse>(StatusCodes.Status200OK);

        group.MapGet("/sessions/{sessionId:guid}", GetSessionStatusAsync)
            .AllowAnonymous()
            .Produces<ExternalAuthSessionResponse>(StatusCodes.Status200OK);

        group.MapPost("/sessions/complete", CompleteLoginAsync)
            .AllowAnonymous()
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK);

        group.MapPost("/sessions/bind-current-user", BindCurrentUserAsync)
            .RequireAuthorization()
            .Produces<ExternalAuthSessionResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static Task<StartExternalAuthResponse> StartQrLoginAsync(
        StartWeChatLoginRequest request,
        HttpContext httpContext,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken) =>
        externalAuthService.StartWeChatQrLoginAsync(request, BuildRequestContext(httpContext), cancellationToken);

    private static Task<ExternalAuthCallbackResponse> HandleQrCallbackAsync(
        [AsParameters] ExternalAuthCallbackRequest request,
        HttpContext httpContext,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken) =>
        externalAuthService.HandleWeChatCallbackAsync(AuthProvider.WeChatQr, request, BuildRequestContext(httpContext), cancellationToken);

    private static Task<StartExternalAuthResponse> StartOfficialAccountBindAsync(
        StartWeChatLoginRequest request,
        HttpContext httpContext,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken)
    {
        var currentUserId = httpContext.User.GetRequiredUserId();
        return externalAuthService.StartWeChatOfficialAccountBindAsync(currentUserId, request, BuildRequestContext(httpContext), cancellationToken);
    }

    private static Task<ExternalAuthCallbackResponse> HandleOfficialAccountCallbackAsync(
        [AsParameters] ExternalAuthCallbackRequest request,
        HttpContext httpContext,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken) =>
        externalAuthService.HandleWeChatCallbackAsync(AuthProvider.WeChatOfficialAccount, request, BuildRequestContext(httpContext), cancellationToken);

    private static Task<ExternalAuthSessionResponse> GetSessionStatusAsync(
        Guid sessionId,
        string completionToken,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken) =>
        externalAuthService.GetSessionStatusAsync(sessionId, completionToken, cancellationToken);

    private static Task<AuthenticationResponse> CompleteLoginAsync(
        CompleteExternalLoginRequest request,
        HttpContext httpContext,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken) =>
        externalAuthService.CompleteLoginAsync(request.SessionId, request.CompletionToken, BuildRequestContext(httpContext), cancellationToken);

    private static Task<ExternalAuthSessionResponse> BindCurrentUserAsync(
        BindExternalIdentityRequest request,
        HttpContext httpContext,
        IExternalAuthService externalAuthService,
        CancellationToken cancellationToken)
    {
        var currentUserId = httpContext.User.GetRequiredUserId();
        return externalAuthService.BindCurrentUserAsync(request.SessionId, request.CompletionToken, currentUserId, cancellationToken);
    }

    private static RequestContext BuildRequestContext(HttpContext httpContext) =>
        new(
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers["User-Agent"].ToString());
}
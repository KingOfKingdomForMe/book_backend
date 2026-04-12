using Microsoft.AspNetCore.Authorization;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Extensions;

namespace ThreeBooks.BookBackend.LoginService.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login/password", LoginByPasswordAsync)
            .AllowAnonymous()
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status423Locked);

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .Produces<AuthenticationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static Task<AuthenticationResponse> RegisterAsync(
        RegisterRequest request,
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken) =>
        authService.RegisterAsync(request, BuildRequestContext(httpContext), cancellationToken);

    private static Task<AuthenticationResponse> LoginByPasswordAsync(
        PasswordLoginRequest request,
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken) =>
        authService.LoginByPasswordAsync(request, BuildRequestContext(httpContext), cancellationToken);

    private static Task<AuthenticationResponse> RefreshAsync(
        RefreshTokenRequest request,
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken) =>
        authService.RefreshAsync(request, BuildRequestContext(httpContext), cancellationToken);

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetRequiredUserId();
        await authService.LogoutAsync(userId, request, BuildRequestContext(httpContext), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        HttpContext httpContext,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetRequiredUserId();
        await authService.ChangePasswordAsync(userId, request, BuildRequestContext(httpContext), cancellationToken);
        return Results.NoContent();
    }

    private static Application.Abstractions.RequestContext BuildRequestContext(HttpContext httpContext) =>
        new(
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers["User-Agent"].ToString());
}
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Extensions;

namespace ThreeBooks.BookBackend.LoginService.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUserAsync)
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static Task<CurrentUserResponse> GetCurrentUserAsync(
        HttpContext httpContext,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetRequiredUserId();
        return userService.GetCurrentUserAsync(userId, cancellationToken);
    }
}
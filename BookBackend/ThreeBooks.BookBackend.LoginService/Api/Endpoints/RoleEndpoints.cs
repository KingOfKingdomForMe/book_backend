using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Api.Endpoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rolesGroup = endpoints.MapGroup("/roles")
            .WithTags("Roles")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        rolesGroup.MapGet("/", GetRolesAsync)
            .Produces<IReadOnlyCollection<RoleResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        rolesGroup.MapPost("/", CreateRoleAsync)
            .Produces<RoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost("/users/{userId:guid}/roles", AssignRolesAsync)
            .WithTags("Roles")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(
        IRoleService roleService,
        CancellationToken cancellationToken) =>
        roleService.GetRolesAsync(cancellationToken);

    private static Task<RoleResponse> CreateRoleAsync(
        CreateRoleRequest request,
        IRoleService roleService,
        CancellationToken cancellationToken) =>
        roleService.CreateRoleAsync(request, cancellationToken);

    private static async Task<IResult> AssignRolesAsync(
        Guid userId,
        AssignUserRolesRequest request,
        IRoleService roleService,
        CancellationToken cancellationToken)
    {
        await roleService.AssignRolesAsync(userId, request, cancellationToken);
        return Results.NoContent();
    }
}
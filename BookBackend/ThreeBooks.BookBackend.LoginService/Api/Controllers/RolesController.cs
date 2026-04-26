using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/roles")]
public sealed class RolesController(IRoleService roleService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<RoleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var response = await roleService.GetRolesAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("permissions")]
    [ProducesResponseType<IReadOnlyCollection<PermissionDefinitionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionDefinitionResponse>>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        var response = await roleService.GetPermissionsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> CreateRoleAsync(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await roleService.CreateRoleAsync(request, cancellationToken);
        return Ok(response);
    }
}
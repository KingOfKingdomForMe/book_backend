using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService, IRoleService roleService) : ApiControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await userService.GetCurrentUserAsync(GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRolesAsync(
        [FromRoute] Guid userId,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        await roleService.AssignRolesAsync(userId, request, cancellationToken);
        return NoContent();
    }
}
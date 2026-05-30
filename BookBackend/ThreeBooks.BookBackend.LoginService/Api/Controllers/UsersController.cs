using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

/// <summary>
/// 当前用户信息与用户角色分配接口。
/// </summary>
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserService userService, IRoleService roleService) : ApiControllerBase
{
    /// <summary>
    /// 获取当前登录用户的资料、角色与权限信息。
    /// </summary>
    /// <remarks>
    /// 适用于前端应用初始化或刷新登录态后拉取用户上下文。建议在访问需要鉴权的页面前先调用一次。
    /// </remarks>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回当前用户的完整登录上下文。</response>
    /// <response code="401">访问令牌无效或已过期。</response>
    /// <response code="404">令牌对应的用户不存在。</response>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var response = await userService.GetCurrentUserAsync(GetRequiredUserId(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 为指定用户分配角色集合。
    /// </summary>
    /// <remarks>
    /// 该操作会以请求体中的角色编码集合作为目标状态进行更新。建议先调用角色列表接口获取可选角色编码后再提交。
    /// </remarks>
    /// <param name="userId">需要调整角色的用户标识。</param>
    /// <param name="request">角色分配请求体，`RoleCodes` 为最终要绑定到用户上的角色编码集合。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="204">角色分配成功。</response>
    /// <response code="404">目标用户或角色不存在。</response>
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

/// <summary>
/// 管理角色与权限定义的后台接口。
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/roles")]
public sealed class RolesController(IRoleService roleService) : ApiControllerBase
{
    /// <summary>
    /// 获取系统内可分配的角色列表。
    /// </summary>
    /// <remarks>
    /// 适用于后台角色管理页面初始化。返回结果可直接用于角色下拉框或授权配置页面。
    /// </remarks>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回所有角色定义。</response>
    /// <response code="401">未提供有效登录凭证。</response>
    /// <response code="403">当前用户不具备管理员权限。</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<RoleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var response = await roleService.GetRolesAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 获取系统支持的权限定义列表。
    /// </summary>
    /// <remarks>
    /// 建议在创建或编辑角色时先调用本接口，前端可将返回的权限编码作为勾选项展示。
    /// </remarks>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回全部权限定义。</response>
    /// <response code="401">未提供有效登录凭证。</response>
    /// <response code="403">当前用户不具备管理员权限。</response>
    [HttpGet("permissions")]
    [ProducesResponseType<IReadOnlyCollection<PermissionDefinitionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PermissionDefinitionResponse>>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        var response = await roleService.GetPermissionsAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 创建新的角色定义。
    /// </summary>
    /// <remarks>
    /// `Code` 应保持全局唯一且稳定，适合作为程序内权限判断常量；`PermissionCodes` 建议直接使用权限定义接口返回的编码值。
    /// </remarks>
    /// <param name="request">创建角色请求体。包含角色编码、角色名称以及关联的权限编码集合。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">角色创建成功。</response>
    /// <response code="400">请求参数不合法。</response>
    /// <response code="409">角色编码已存在。</response>
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
namespace ThreeBooks.BookBackend.LoginService.Contracts.Requests;

/// <summary>
/// 创建角色的请求参数。
/// </summary>
/// <param name="Code">角色编码，建议使用稳定的英文标识并保持唯一。</param>
/// <param name="Name">角色显示名称。</param>
/// <param name="PermissionCodes">要授予该角色的权限编码集合。</param>
public sealed record CreateRoleRequest(
    string Code,
    string Name,
    IReadOnlyCollection<string>? PermissionCodes);

/// <summary>
/// 分配用户角色的请求参数。
/// </summary>
/// <param name="RoleCodes">目标用户最终应持有的角色编码集合。</param>
public sealed record AssignUserRolesRequest(IReadOnlyCollection<string> RoleCodes);
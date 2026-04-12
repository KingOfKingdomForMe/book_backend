namespace ThreeBooks.BookBackend.LoginService.Contracts.Requests;

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    IReadOnlyCollection<string>? PermissionCodes);

public sealed record AssignUserRolesRequest(IReadOnlyCollection<string> RoleCodes);
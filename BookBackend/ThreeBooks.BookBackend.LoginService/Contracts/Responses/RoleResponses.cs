namespace ThreeBooks.BookBackend.LoginService.Contracts.Responses;

public sealed record RoleResponse(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyCollection<string> Permissions);

public sealed record PermissionDefinitionResponse(
    string Code,
    string Name,
    string Group,
    string? Description,
    bool IsSystem,
    bool GrantedToAdminByDefault);
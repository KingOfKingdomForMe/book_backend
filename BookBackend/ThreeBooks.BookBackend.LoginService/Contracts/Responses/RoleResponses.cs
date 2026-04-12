namespace ThreeBooks.BookBackend.LoginService.Contracts.Responses;

public sealed record RoleResponse(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyCollection<string> Permissions);
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;

namespace ThreeBooks.BookBackend.LoginService.Application.Abstractions;

public sealed record RequestContext(string? IpAddress, string? UserAgent);

public sealed record AccessTokenDescriptor(string Token, DateTimeOffset ExpiresAtUtc);

public interface IAuthService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<AuthenticationResponse> LoginByPasswordAsync(PasswordLoginRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<AuthenticationResponse> RefreshAsync(RefreshTokenRequest request, RequestContext context, CancellationToken cancellationToken);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, RequestContext context, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, LogoutRequest request, RequestContext context, CancellationToken cancellationToken);
}

public interface IJwtTokenService
{
    AccessTokenDescriptor CreateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
}

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);

    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task AssignRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
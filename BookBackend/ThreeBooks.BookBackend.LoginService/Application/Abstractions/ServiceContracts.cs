using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Contracts.WeChat;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Application.Abstractions;

public sealed record RequestContext(string? IpAddress, string? UserAgent);

public sealed record AccessTokenDescriptor(string Token, DateTimeOffset ExpiresAtUtc);

public interface IAuthService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<AuthenticationResponse> LoginByPasswordAsync(PasswordLoginRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<AuthenticationResponse> SignInUserAsync(Guid userId, RequestContext context, string auditAction, CancellationToken cancellationToken);

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

    Task<IReadOnlyCollection<PermissionDefinitionResponse>> GetPermissionsAsync(CancellationToken cancellationToken);

    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task AssignRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IExternalAuthService
{
    Task<StartExternalAuthResponse> StartWeChatQrLoginAsync(StartWeChatLoginRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<StartExternalAuthResponse> StartWeChatOfficialAccountBindAsync(Guid currentUserId, StartWeChatLoginRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<ExternalAuthCallbackResponse> HandleWeChatCallbackAsync(AuthProvider provider, ExternalAuthCallbackRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<ExternalAuthSessionResponse> GetSessionStatusAsync(Guid sessionId, string completionToken, CancellationToken cancellationToken);

    Task<AuthenticationResponse> CompleteLoginAsync(Guid sessionId, string completionToken, RequestContext context, CancellationToken cancellationToken);

    Task<ExternalAuthSessionResponse> BindCurrentUserAsync(Guid sessionId, string completionToken, Guid currentUserId, CancellationToken cancellationToken);
}

public interface IWeChatAuthClient
{
    string BuildAuthorizeUrl(WeChatAuthorizeRequest request);

    Task<WeChatIdentityProfile> ExchangeCodeForProfileAsync(AuthProvider provider, string code, CancellationToken cancellationToken);
}
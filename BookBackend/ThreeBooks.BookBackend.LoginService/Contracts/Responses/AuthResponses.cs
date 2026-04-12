namespace ThreeBooks.BookBackend.LoginService.Contracts.Responses;

public sealed record AuthUserResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string? Mobile,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record AuthenticationResponse(
    AuthUserResponse User,
    AuthTokensResponse Tokens);

public sealed record CurrentUserResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string? Mobile,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
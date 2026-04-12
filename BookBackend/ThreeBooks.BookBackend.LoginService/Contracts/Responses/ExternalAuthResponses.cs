using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Contracts.Responses;

public sealed record ExternalPrincipalResponse(
    AuthProvider Provider,
    string? AppId,
    string? OpenId,
    string? UnionId,
    string? Nickname,
    string? AvatarUrl);

public sealed record ExternalAuthSessionResponse(
    Guid SessionId,
    ExternalAuthPurpose Purpose,
    AuthProvider Provider,
    ExternalAuthSessionStatus Status,
    DateTimeOffset ExpiresAtUtc,
    bool RequiresBinding,
    string? FailureCode,
    string? FailureMessage,
    Guid? ResolvedUserId,
    ExternalPrincipalResponse? ExternalPrincipal,
    string? CompletionToken);

public sealed record StartExternalAuthResponse(
    Guid SessionId,
    AuthProvider Provider,
    ExternalAuthPurpose Purpose,
    string AuthorizeUrl,
    DateTimeOffset ExpiresAtUtc,
    string State,
    string CompletionToken);

public sealed record ExternalAuthCallbackResponse(
    Guid SessionId,
    AuthProvider Provider,
    ExternalAuthSessionStatus Status,
    string Message,
    bool RequiresBinding,
    AuthenticationResponse? Authentication,
    ExternalPrincipalResponse? ExternalPrincipal,
    string? CompletionToken);
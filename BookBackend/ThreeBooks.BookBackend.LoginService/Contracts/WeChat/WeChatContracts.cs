using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Contracts.WeChat;

public sealed record WeChatAuthorizeRequest(
    AuthProvider Provider,
    string AppId,
    string RedirectUri,
    string State,
    bool RequireUserInfoScope);

public sealed record WeChatIdentityProfile(
    AuthProvider Provider,
    string AppId,
    string OpenId,
    string? UnionId,
    string? Nickname,
    string? AvatarUrl,
    string RawProfileJson);

public sealed record WeChatTokenExchangeResult(
    string OpenId,
    string? UnionId,
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string RawJson);
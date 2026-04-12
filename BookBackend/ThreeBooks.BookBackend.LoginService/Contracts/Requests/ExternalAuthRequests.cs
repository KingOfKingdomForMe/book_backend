using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Contracts.Requests;

public sealed record StartWeChatLoginRequest(string? RedirectUri = null);

public sealed record CompleteExternalLoginRequest(Guid SessionId, string CompletionToken);

public sealed record ExternalAuthCallbackRequest(string? Code, string? State, string? Error = null, string? ErrorDescription = null);

public sealed record BindExternalIdentityRequest(Guid SessionId, string CompletionToken);

public sealed record ExternalLoginStatusRequest(Guid SessionId, string CompletionToken);
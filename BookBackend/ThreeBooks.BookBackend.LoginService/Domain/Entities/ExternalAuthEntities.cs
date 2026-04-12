using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Domain.Entities;

public sealed class ExternalIdentity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AuthProvider Provider { get; set; }

    public string AppId { get; set; } = string.Empty;

    public string OpenId { get; set; } = string.Empty;

    public string? UnionId { get; set; }

    public string? Nickname { get; set; }

    public string? AvatarUrl { get; set; }

    public string? RawProfileJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public User User { get; set; } = null!;
}

public sealed class ExternalAuthSession
{
    public Guid Id { get; set; }

    public ExternalAuthPurpose Purpose { get; set; }

    public AuthProvider Provider { get; set; }

    public ExternalAuthSessionStatus Status { get; set; }

    public string State { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public Guid? RequestedByUserId { get; set; }

    public Guid? ResolvedUserId { get; set; }

    public string? AppId { get; set; }

    public string? ExternalOpenId { get; set; }

    public string? ExternalUnionId { get; set; }

    public string? ExternalNickname { get; set; }

    public string? ExternalAvatarUrl { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public string? RedirectUri { get; set; }

    public string? CompletionTokenHash { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? AuthorizedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset? ConsumedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public User? RequestedByUser { get; set; }

    public User? ResolvedUser { get; set; }
}
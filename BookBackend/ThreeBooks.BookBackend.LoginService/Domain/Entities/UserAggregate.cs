using ThreeBooks.BookBackend.LoginService.Domain.Enums;

namespace ThreeBooks.BookBackend.LoginService.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Mobile { get; set; }

    public bool IsActive { get; set; } = true;

    public int FailedPasswordAttemptCount { get; set; }

    public DateTimeOffset? LockoutEndUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<UserCredential> Credentials { get; set; } = [];

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public sealed class UserCredential
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AuthProvider Provider { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string SecretHash { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? CreatedByIp { get; set; }

    public string? CreatedByUserAgent { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? RevokedReason { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public User User { get; set; } = null!;
}
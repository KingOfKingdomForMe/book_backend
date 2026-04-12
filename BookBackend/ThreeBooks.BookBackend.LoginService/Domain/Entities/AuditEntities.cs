namespace ThreeBooks.BookBackend.LoginService.Domain.Entities;

public sealed class LoginAttempt
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Identity { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public User? User { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public User? User { get; set; }
}
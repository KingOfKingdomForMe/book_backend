namespace ThreeBooks.BookBackend.LoginService.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public sealed class Permission
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public sealed class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}

public sealed class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Role Role { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}
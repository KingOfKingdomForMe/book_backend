using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;
using ThreeBooks.BookBackend.LoginService.Options;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;

namespace ThreeBooks.BookBackend.LoginService.Infrastructure.Seeding;

public sealed class LoginDbSeeder(
    LoginDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IOptions<SeedAdminOptions> seedAdminOptionsAccessor)
{
    private readonly LoginDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly SeedAdminOptions _seedAdminOptions = seedAdminOptionsAccessor.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminRole = await EnsureRoleAsync(
            SystemRoles.Admin,
            "Administrator",
            SystemPermissionCatalog.DefaultAdminPermissionCodes,
            cancellationToken);

        var userRole = await EnsureRoleAsync(
            SystemRoles.User,
            "User",
            [SystemPermissions.AuthSelf],
            cancellationToken);

        if (!_seedAdminOptions.Enabled || string.IsNullOrWhiteSpace(_seedAdminOptions.Password))
        {
            return;
        }

        var username = NormalizeUsername(_seedAdminOptions.Username);
        var existingUser = await _dbContext.Users
            .Include(user => user.Credentials)
            .Include(user => user.UserRoles)
            .FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

        if (existingUser is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(_seedAdminOptions.DisplayName)
                ? "System Administrator"
                : _seedAdminOptions.DisplayName.Trim(),
            Mobile = NormalizeMobile(_seedAdminOptions.Mobile),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsActive = true
        };

        user.Credentials.Add(new UserCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = AuthProvider.Password,
            Subject = username,
            SecretHash = _passwordHasher.HashPassword(user, _seedAdminOptions.Password),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsPrimary = true
        });

        var seedRoles = _seedAdminOptions.Roles.Length == 0
            ? [SystemRoles.Admin]
            : _seedAdminOptions.Roles;

        foreach (var roleCode in seedRoles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = string.Equals(roleCode, SystemRoles.User, StringComparison.OrdinalIgnoreCase)
                ? userRole
                : string.Equals(roleCode, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase)
                    ? adminRole
                    : await EnsureRoleAsync(roleCode.Trim(), roleCode.Trim(), [], cancellationToken);

            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAtUtc = now
            });
        }

        _dbContext.Users.Add(user);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = "seed.admin.created",
            Detail = $"Created bootstrap admin '{user.Username}'.",
            CreatedAtUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> EnsureRoleAsync(
        string roleCode,
        string roleName,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .Include(entity => entity.RolePermissions)
            .ThenInclude(entity => entity.Permission)
            .FirstOrDefaultAsync(entity => entity.Code == roleCode, cancellationToken);

        if (role is null)
        {
            var now = DateTimeOffset.UtcNow;
            role = new Role
            {
                Id = Guid.NewGuid(),
                Code = roleCode,
                Name = roleName,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.Roles.Add(role);
        }
        else if (!string.Equals(role.Name, roleName, StringComparison.Ordinal))
        {
            role.Name = roleName;
            role.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        var permissions = await EnsurePermissionsAsync(permissionCodes, cancellationToken);

        foreach (var permission in permissions)
        {
            if (role.RolePermissions.Any(entity => entity.PermissionId == permission.Id))
            {
                continue;
            }

            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task<List<Permission>> EnsurePermissionsAsync(
        IEnumerable<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var normalizedCodes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedCodes.Length == 0)
        {
            return [];
        }

        var permissions = await _dbContext.Permissions
            .Where(entity => normalizedCodes.Contains(entity.Code))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var code in normalizedCodes)
        {
            var existing = permissions.FirstOrDefault(entity => string.Equals(entity.Code, code, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                if (SystemPermissionCatalog.TryGet(code, out var definition)
                    && definition is not null
                    && !string.Equals(existing.Name, definition.Name, StringComparison.Ordinal))
                {
                    existing.Name = definition.Name;
                }

                continue;
            }

            var name = SystemPermissionCatalog.TryGet(code, out var newDefinition) && newDefinition is not null
                ? newDefinition.Name
                : code;

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                CreatedAtUtc = now
            };

            permissions.Add(permission);
            _dbContext.Permissions.Add(permission);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return permissions;
    }

    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    private static string? NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return null;
        }

        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
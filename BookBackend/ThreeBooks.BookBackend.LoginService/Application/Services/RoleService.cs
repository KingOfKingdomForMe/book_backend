using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;
using ThreeBooks.BookBackend.LoginService.Options;

namespace ThreeBooks.BookBackend.LoginService.Application.Services;

public sealed class RoleService(LoginDbContext dbContext) : IRoleService
{
    private readonly LoginDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
    {
        await EnsureBuiltInRolesAndPermissionsAsync(cancellationToken);

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Include(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .OrderBy(role => role.Code)
            .ToListAsync(cancellationToken);

        return roles.Select(MapRole).ToArray();
    }

    public async Task<IReadOnlyCollection<PermissionDefinitionResponse>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        await EnsureBuiltInRolesAndPermissionsAsync(cancellationToken);

        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Code)
            .ToListAsync(cancellationToken);

        return permissions.Select(MapPermission).ToArray();
    }

    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        await EnsureBuiltInRolesAndPermissionsAsync(cancellationToken);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            errors[nameof(request.Code)] = ["Role code is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Role name is required."];
        }

        if (errors.Count > 0)
        {
            throw ApiException.Validation("Create role request is invalid.", errors);
        }

        var roleCode = request.Code.Trim();
        if (await _dbContext.Roles.AnyAsync(role => role.Code == roleCode, cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict, $"Role '{roleCode}' already exists.");
        }

        var permissionCodes = request.PermissionCodes?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        var now = DateTimeOffset.UtcNow;
        var permissionLookup = await ResolvePermissionsAsync(permissionCodes, cancellationToken);

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = roleCode,
            Name = request.Name.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var permissionCode in permissionCodes)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionLookup[permissionCode].Id,
                CreatedAtUtc = now,
                Permission = permissionLookup[permissionCode],
                Role = role
            });
        }

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapRole(role);
    }

    public async Task AssignRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken cancellationToken)
    {
        await EnsureBuiltInRolesAndPermissionsAsync(cancellationToken);

        if (request.RoleCodes.Count == 0)
        {
            throw ApiException.Validation("At least one role code is required.", new Dictionary<string, string[]>
            {
                [nameof(request.RoleCodes)] = ["At least one role code is required."]
            });
        }

        var user = await _dbContext.Users
            .Include(entity => entity.UserRoles)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User was not found.");

        var requestedRoleCodes = request.RoleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roles = await _dbContext.Roles
            .Where(role => requestedRoleCodes.Contains(role.Code))
            .ToListAsync(cancellationToken);

        if (roles.Count != requestedRoleCodes.Length)
        {
            var foundCodes = roles.Select(role => role.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingCodes = requestedRoleCodes.Where(code => !foundCodes.Contains(code)).ToArray();
            throw new ApiException(StatusCodes.Status404NotFound, $"Roles were not found: {string.Join(", ", missingCodes)}.");
        }

        _dbContext.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles.Clear();

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = "auth.roles.assigned",
            Detail = $"Assigned roles: {string.Join(", ", requestedRoleCodes)}.",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureBuiltInRolesAndPermissionsAsync(CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(
            SystemRoles.Admin,
            "Administrator",
            SystemPermissionCatalog.DefaultAdminPermissionCodes,
            cancellationToken);

        await EnsureRoleAsync(
            SystemRoles.User,
            "User",
            [SystemPermissions.AuthSelf],
            cancellationToken);
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

        var now = DateTimeOffset.UtcNow;
        if (role is null)
        {
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
            role.UpdatedAtUtc = now;
        }

        var permissionLookup = await EnsurePermissionsAsync(permissionCodes, cancellationToken);

        foreach (var permissionCode in permissionCodes)
        {
            var permission = permissionLookup[permissionCode];
            if (role.RolePermissions.Any(entity => entity.PermissionId == permission.Id))
            {
                continue;
            }

            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                CreatedAtUtc = now,
                Permission = permission,
                Role = role
            });

            role.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task<Dictionary<string, Permission>> EnsurePermissionsAsync(
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        if (permissionCodes.Count == 0)
        {
            return new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);
        }

        var normalizedCodes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await _dbContext.Permissions
            .Where(permission => normalizedCodes.Contains(permission.Code))
            .ToListAsync(cancellationToken);

        var changed = false;
        var now = DateTimeOffset.UtcNow;
        foreach (var permissionCode in normalizedCodes)
        {
            var existing = permissions.FirstOrDefault(permission => string.Equals(permission.Code, permissionCode, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                if (SystemPermissionCatalog.TryGet(permissionCode, out var existingDefinition)
                    && existingDefinition is not null
                    && !string.Equals(existing.Name, existingDefinition.Name, StringComparison.Ordinal))
                {
                    existing.Name = existingDefinition.Name;
                    changed = true;
                }

                continue;
            }

            var name = SystemPermissionCatalog.TryGet(permissionCode, out var definition) && definition is not null
                ? definition.Name
                : permissionCode;

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = permissionCode,
                Name = name,
                CreatedAtUtc = now
            };

            permissions.Add(permission);
            _dbContext.Permissions.Add(permission);
            changed = true;
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return permissions.ToDictionary(permission => permission.Code, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, Permission>> ResolvePermissionsAsync(
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        if (permissionCodes.Count == 0)
        {
            return new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);
        }

        var normalizedCodes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await _dbContext.Permissions
            .Where(permission => normalizedCodes.Contains(permission.Code))
            .ToListAsync(cancellationToken);

        if (permissions.Count != normalizedCodes.Length)
        {
            var foundCodes = permissions.Select(permission => permission.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingCodes = normalizedCodes.Where(code => !foundCodes.Contains(code)).ToArray();

            throw ApiException.Validation(
                "Create role request contains unknown permission codes.",
                new Dictionary<string, string[]>
                {
                    [nameof(CreateRoleRequest.PermissionCodes)] = [$"Unknown permission codes: {string.Join(", ", missingCodes)}."]
                });
        }

        return permissions.ToDictionary(permission => permission.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static RoleResponse MapRole(Role role)
    {
        var permissions = role.RolePermissions
            .Select(rolePermission => rolePermission.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToArray();

        return new RoleResponse(role.Id, role.Code, role.Name, permissions);
    }

    private static PermissionDefinitionResponse MapPermission(Permission permission)
    {
        if (SystemPermissionCatalog.TryGet(permission.Code, out var definition) && definition is not null)
        {
            return new PermissionDefinitionResponse(
                definition.Code,
                definition.Name,
                definition.Group,
                definition.Description,
                true,
                definition.GrantToAdminByDefault);
        }

        return new PermissionDefinitionResponse(
            permission.Code,
            permission.Name,
            "Custom",
            null,
            false,
            false);
    }
}
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Requests;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Domain.Entities;
using ThreeBooks.BookBackend.LoginService.Domain.Enums;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;
using ThreeBooks.BookBackend.LoginService.Options;

using LoginLockoutOptions = ThreeBooks.BookBackend.LoginService.Options.LockoutOptions;

namespace ThreeBooks.BookBackend.LoginService.Application.Services;

public sealed class AuthService(
    LoginDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptionsAccessor,
    IOptions<PasswordPolicyOptions> passwordPolicyOptionsAccessor,
    IOptions<LoginLockoutOptions> lockoutOptionsAccessor) : IAuthService
{
    private readonly LoginDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly JwtOptions _jwtOptions = jwtOptionsAccessor.Value;
    private readonly PasswordPolicyOptions _passwordPolicyOptions = passwordPolicyOptionsAccessor.Value;
    private readonly LoginLockoutOptions _lockoutOptions = lockoutOptionsAccessor.Value;

    public async Task<AuthenticationResponse> RegisterAsync(
        RegisterRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ValidateRegistrationRequest(request);

        var username = NormalizeUsername(request.Username);
        var mobile = NormalizeMobile(request.Mobile);
        var displayName = request.DisplayName?.Trim();

        if (await _dbContext.Users.AnyAsync(user => user.Username == username, cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict, $"Username '{request.Username}' is already taken.");
        }

        if (!string.IsNullOrWhiteSpace(mobile) && await _dbContext.Users.AnyAsync(user => user.Mobile == mobile, cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict, $"Mobile '{request.Mobile}' is already registered.");
        }

        var now = DateTimeOffset.UtcNow;
        var isFirstUser = !await _dbContext.Users.AnyAsync(cancellationToken);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            Mobile = mobile,
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
            SecretHash = _passwordHasher.HashPassword(user, request.Password),
            IsPrimary = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var roleCodes = isFirstUser
            ? new[] { SystemRoles.Admin, SystemRoles.User }
            : new[] { SystemRoles.User };

        var roles = await EnsureRolesAsync(roleCodes, cancellationToken);
        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAtUtc = now,
                Role = role,
                User = user
            });
        }

        _dbContext.Users.Add(user);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = "auth.register",
            Detail = $"User '{user.Username}' registered.",
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            CreatedAtUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await IssueAuthenticationResponseAsync(user, context, cancellationToken, "auth.register.completed");
    }

    public async Task<AuthenticationResponse> LoginByPasswordAsync(
        PasswordLoginRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ValidateLoginRequest(request);

        var normalizedIdentity = NormalizeUsername(request.Identity);
        var normalizedMobile = NormalizeMobile(request.Identity);
        var user = await FindUserForAuthenticationAsync(normalizedIdentity, normalizedMobile, cancellationToken);

        if (user is null)
        {
            await RecordLoginAttemptAsync(null, request.Identity.Trim(), false, "user_not_found", context, cancellationToken);
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid username/mobile or password.");
        }

        if (!user.IsActive)
        {
            await RecordLoginAttemptAsync(user, request.Identity.Trim(), false, "user_inactive", context, cancellationToken);
            throw new ApiException(StatusCodes.Status403Forbidden, "The user account is disabled.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTimeOffset.UtcNow)
        {
            await RecordLoginAttemptAsync(user, request.Identity.Trim(), false, "user_locked", context, cancellationToken);
            throw new ApiException(StatusCodes.Status423Locked, $"The user account is locked until {user.LockoutEndUtc.Value:O}.");
        }

        var passwordCredential = user.Credentials.FirstOrDefault(credential => credential.Provider == AuthProvider.Password);
        if (passwordCredential is null)
        {
            await RecordLoginAttemptAsync(user, request.Identity.Trim(), false, "password_credential_missing", context, cancellationToken);
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid username/mobile or password.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, passwordCredential.SecretHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            user.FailedPasswordAttemptCount++;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;

            if (user.FailedPasswordAttemptCount >= _lockoutOptions.MaxFailedAccessAttempts)
            {
                user.LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(_lockoutOptions.LockoutMinutes);
                user.FailedPasswordAttemptCount = 0;
            }

            await RecordLoginAttemptAsync(user, request.Identity.Trim(), false, "invalid_password", context, cancellationToken);
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid username/mobile or password.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            passwordCredential.SecretHash = _passwordHasher.HashPassword(user, request.Password);
            passwordCredential.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        user.FailedPasswordAttemptCount = 0;
        user.LockoutEndUtc = null;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await RecordLoginAttemptAsync(user, request.Identity.Trim(), true, null, context, cancellationToken);
        return await IssueAuthenticationResponseAsync(user, context, cancellationToken, "auth.login.password");
    }

    public async Task<AuthenticationResponse> SignInUserAsync(
        Guid userId,
        RequestContext context,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var user = await QueryUserGraph()
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User was not found.");

        if (!user.IsActive)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "The user account is disabled.");
        }

        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        return await IssueAuthenticationResponseAsync(user, context, cancellationToken, auditAction);
    }

    public async Task<AuthenticationResponse> RefreshAsync(
        RefreshTokenRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw ApiException.Validation("Refresh token is required.", new Dictionary<string, string[]>
            {
                [nameof(request.RefreshToken)] = ["Refresh token is required."]
            });
        }

        var tokenHash = ComputeTokenHash(request.RefreshToken);
        var refreshToken = await _dbContext.RefreshTokens
            .Include(entity => entity.User)
            .ThenInclude(entity => entity.Credentials)
            .Include(entity => entity.User)
            .ThenInclude(entity => entity.UserRoles)
            .ThenInclude(entity => entity.Role)
            .ThenInclude(entity => entity.RolePermissions)
            .ThenInclude(entity => entity.Permission)
            .FirstOrDefaultAsync(entity => entity.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.User is null)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token is invalid.");
        }

        if (refreshToken.RevokedAtUtc.HasValue)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token has already been revoked.");
        }

        if (refreshToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token has expired.");
        }

        if (!refreshToken.User.IsActive)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "The user account is disabled.");
        }

        refreshToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        refreshToken.RevokedReason = "rotated";
        refreshToken.User.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return await IssueAuthenticationResponseAsync(refreshToken.User, context, cancellationToken, "auth.refresh", refreshToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ValidateChangePasswordRequest(request);

        var user = await _dbContext.Users
            .Include(entity => entity.Credentials)
            .Include(entity => entity.RefreshTokens)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User was not found.");

        var passwordCredential = user.Credentials.FirstOrDefault(entity => entity.Provider == AuthProvider.Password)
            ?? throw new ApiException(StatusCodes.Status400BadRequest, "Password login is not configured for the current user.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, passwordCredential.SecretHash, request.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Current password is incorrect.");
        }

        passwordCredential.SecretHash = _passwordHasher.HashPassword(user, request.NewPassword);
        passwordCredential.UpdatedAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        foreach (var token in user.RefreshTokens.Where(token => token.RevokedAtUtc is null && token.ExpiresAtUtc > DateTimeOffset.UtcNow))
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevokedReason = "password_changed";
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = "auth.password.changed",
            Detail = "User changed their password and active refresh tokens were revoked.",
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAsync(
        Guid userId,
        LogoutRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw ApiException.Validation("Refresh token is required.", new Dictionary<string, string[]>
            {
                [nameof(request.RefreshToken)] = ["Refresh token is required."]
            });
        }

        var refreshTokenHash = ComputeTokenHash(request.RefreshToken);
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(entity => entity.UserId == userId && entity.TokenHash == refreshTokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return;
        }

        if (!refreshToken.RevokedAtUtc.HasValue)
        {
            refreshToken.RevokedAtUtc = DateTimeOffset.UtcNow;
            refreshToken.RevokedReason = "logout";

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = "auth.logout",
                Detail = "Refresh token was revoked during logout.",
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthenticationResponse> IssueAuthenticationResponseAsync(
        User user,
        RequestContext context,
        CancellationToken cancellationToken,
        string auditAction,
        RefreshToken? replacedToken = null)
    {
        var roleCodes = user.UserRoles.Select(entity => entity.Role.Code).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToArray();
        var permissionCodes = user.UserRoles
            .SelectMany(entity => entity.Role.RolePermissions)
            .Select(entity => entity.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToArray();

        var accessToken = _jwtTokenService.CreateAccessToken(user, roleCodes, permissionCodes);
        var rawRefreshToken = GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = ComputeTokenHash(rawRefreshToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByIp = context.IpAddress,
            CreatedByUserAgent = context.UserAgent
        };

        if (replacedToken is not null)
        {
            replacedToken.ReplacedByTokenId = refreshToken.Id;
        }

        _dbContext.RefreshTokens.Add(refreshToken);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = auditAction,
            Detail = "Issued access token and refresh token.",
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticationResponse(
            new AuthUserResponse(user.Id, user.Username, user.DisplayName, user.Mobile, roleCodes, permissionCodes),
            new AuthTokensResponse(accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken, refreshToken.ExpiresAtUtc));
    }

    private async Task<User?> FindUserForAuthenticationAsync(
        string normalizedIdentity,
        string? normalizedMobile,
        CancellationToken cancellationToken)
    {
        var user = await QueryUserGraph()
            .FirstOrDefaultAsync(entity => entity.Username == normalizedIdentity, cancellationToken);

        if (user is not null || string.IsNullOrWhiteSpace(normalizedMobile))
        {
            return user;
        }

        return await QueryUserGraph()
            .FirstOrDefaultAsync(entity => entity.Mobile == normalizedMobile, cancellationToken);
    }

    private IQueryable<User> QueryUserGraph() => _dbContext.Users
        .Include(entity => entity.Credentials)
        .Include(entity => entity.UserRoles)
        .ThenInclude(entity => entity.Role)
        .ThenInclude(entity => entity.RolePermissions)
        .ThenInclude(entity => entity.Permission)
        .AsSplitQuery();

    private async Task<List<Role>> EnsureRolesAsync(IEnumerable<string> requestedRoleCodes, CancellationToken cancellationToken)
    {
        var roleCodes = requestedRoleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roles = await _dbContext.Roles
            .Include(entity => entity.RolePermissions)
            .ThenInclude(entity => entity.Permission)
            .Where(entity => roleCodes.Contains(entity.Code))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var roleCode in roleCodes)
        {
            if (roles.Any(entity => entity.Code == roleCode))
            {
                continue;
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Code = roleCode,
                Name = roleCode,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            var permissions = await EnsurePermissionsAsync(GetDefaultPermissions(roleCode), cancellationToken);
            foreach (var permission in permissions)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    CreatedAtUtc = now,
                    Permission = permission,
                    Role = role
                });
            }

            roles.Add(role);
            _dbContext.Roles.Add(role);
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return roles;
    }

    private async Task<List<Permission>> EnsurePermissionsAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken)
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

        foreach (var code in normalizedCodes)
        {
            if (permissions.Any(entity => entity.Code == code))
            {
                continue;
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = code,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            permissions.Add(permission);
            _dbContext.Permissions.Add(permission);
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return permissions;
    }

    private async Task RecordLoginAttemptAsync(
        User? user,
        string identity,
        bool succeeded,
        string? failureReason,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        _dbContext.LoginAttempts.Add(new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            Identity = identity,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void ValidateRegistrationRequest(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors[nameof(request.Username)] = ["Username is required."];
        }

        AppendPasswordPolicyErrors(request.Password, nameof(request.Password), errors);

        if (errors.Count > 0)
        {
            throw ApiException.Validation("Registration request is invalid.", errors);
        }
    }

    private void ValidateLoginRequest(PasswordLoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Identity))
        {
            errors[nameof(request.Identity)] = ["Identity is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors[nameof(request.Password)] = ["Password is required."];
        }

        if (errors.Count > 0)
        {
            throw ApiException.Validation("Login request is invalid.", errors);
        }
    }

    private void ValidateChangePasswordRequest(ChangePasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors[nameof(request.CurrentPassword)] = ["Current password is required."];
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            errors[nameof(request.ConfirmNewPassword)] = ["Confirm password does not match the new password."];
        }

        AppendPasswordPolicyErrors(request.NewPassword, nameof(request.NewPassword), errors);

        if (errors.Count > 0)
        {
            throw ApiException.Validation("Change password request is invalid.", errors);
        }
    }

    private void AppendPasswordPolicyErrors(string password, string fieldName, IDictionary<string, string[]> errors)
    {
        var ruleFailures = new List<string>();
        if (string.IsNullOrWhiteSpace(password) || password.Length < _passwordPolicyOptions.MinLength)
        {
            ruleFailures.Add($"Password must be at least {_passwordPolicyOptions.MinLength} characters long.");
        }

        //if (_passwordPolicyOptions.RequireUppercase && !password.Any(char.IsUpper))
        //{
        //    ruleFailures.Add("Password must contain at least one uppercase letter.");
        //}

        //if (_passwordPolicyOptions.RequireLowercase && !password.Any(char.IsLower))
        //{
        //    ruleFailures.Add("Password must contain at least one lowercase letter.");
        //}

        //if (_passwordPolicyOptions.RequireDigit && !password.Any(char.IsDigit))
        //{
        //    ruleFailures.Add("Password must contain at least one digit.");
        //}

        //if (_passwordPolicyOptions.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        //{
        //    ruleFailures.Add("Password must contain at least one non-alphanumeric character.");
        //}

        if (ruleFailures.Count > 0)
        {
            errors[fieldName] = ruleFailures.ToArray();
        }
    }

    private static IEnumerable<string> GetDefaultPermissions(string roleCode)
    {
        if (string.Equals(roleCode, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return [SystemPermissions.AuthSelf, SystemPermissions.UserManage, SystemPermissions.RoleManage];
        }

        if (string.Equals(roleCode, SystemRoles.User, StringComparison.OrdinalIgnoreCase))
        {
            return [SystemPermissions.AuthSelf];
        }

        return [];
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeTokenHash(string rawToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes);
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
namespace ThreeBooks.BookBackend.LoginService.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ThreeBooks.BookBackend.LoginService";

    public string Audience { get; set; } = "ThreeBooks.Clients";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 30;

    public int RefreshTokenLifetimeDays { get; set; } = 14;
}

public sealed class LoginDbOptions
{
    public const string SectionName = "LoginDb";

    public bool ApplyMigrationsOnStartup { get; set; }

    public bool EnableDetailedErrors { get; set; }
}

public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int MinLength { get; set; } = 8;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireDigit { get; set; } = true;

    public bool RequireNonAlphanumeric { get; set; }
}

public sealed class LockoutOptions
{
    public const string SectionName = "Lockout";

    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;
}

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public bool Enabled { get; set; }

    public string Username { get; set; } = "admin";

    public string DisplayName { get; set; } = "System Administrator";

    public string? Mobile { get; set; }

    public string Password { get; set; } = string.Empty;

    public string[] Roles { get; set; } = [SystemRoles.Admin];
}

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
}

public static class SystemRoles
{
    public const string Admin = "Admin";

    public const string User = "User";
}

public static class SystemPermissions
{
    public const string AuthSelf = "auth:self";

    public const string UserManage = "user:manage";

    public const string RoleManage = "role:manage";
}

public static class CustomClaimTypes
{
    public const string Permission = "permission";
}
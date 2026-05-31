using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ThreeBooks.BookBackend.Api.Authorization;

public sealed class ApiJwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ThreeBooks.BookBackend.LoginService";

    public string Audience { get; set; } = "ThreeBooks.Clients";

    public string SigningKey { get; set; } = string.Empty;
}

public sealed class DevelopmentSwaggerBearerOptions
{
    public const string SectionName = "DevelopmentSwaggerBearer";

    public bool Enabled { get; set; }

    public string Token { get; set; } = string.Empty;

    public string UserId { get; set; } = "11111111-1111-1111-1111-111111111111";

    public string Username { get; set; } = "swagger-dev-admin";

    public string DisplayName { get; set; } = "Swagger Dev Admin";

    public string[] Roles { get; set; } = [BookBackendApiRoles.Admin];

    public string[] Permissions { get; set; } =
    [
        BookBackendApiPermissions.Admin,
        BookBackendApiPermissions.OrderManage,
        BookBackendApiPermissions.UnboxingModerate,
        BookBackendApiPermissions.TemplateManage
    ];

    public int GeneratedAccessTokenLifetimeMinutes { get; set; } = 30;

    public bool IsEnabled => Enabled && !string.IsNullOrWhiteSpace(Token);
}

public static class BookBackendApiAuthorizationPolicies
{
    public const string OrderManage = "BookBackendOrderManage";

    public const string UnboxingModerate = "BookBackendUnboxingModerate";

    public const string TemplateManage = "BookBackendTemplateManage";
}

public static class BookBackendApiPermissions
{
    public const string Admin = "bookbackend:admin";

    public const string OrderManage = "bookbackend:order:manage";

    public const string UnboxingModerate = "bookbackend:unboxing:moderate";

    public const string TemplateManage = "bookbackend:template:manage";
}

public static class BookBackendApiRoles
{
    public const string Admin = "Admin";
}

public static class BookBackendApiClaimTypes
{
    public const string Permission = "permission";
}

public static class BookBackendApiAuthorizationEvaluator
{
    public static bool IsAdminOrHasAnyPermission(ClaimsPrincipal user, params string[] permissions)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole(BookBackendApiRoles.Admin))
        {
            return true;
        }

        var requiredPermissions = permissions
            .Append(BookBackendApiPermissions.Admin)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return user.Claims.Any(claim =>
            string.Equals(claim.Type, BookBackendApiClaimTypes.Permission, StringComparison.OrdinalIgnoreCase)
            && requiredPermissions.Contains(claim.Value, StringComparer.OrdinalIgnoreCase));
    }
}

public static class DevelopmentSwaggerBearerTokenFactory
{
    public static string CreateJwt(ApiJwtOptions jwtOptions, DevelopmentSwaggerBearerOptions options)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(options.GeneratedAccessTokenLifetimeMinutes < 1
            ? 30
            : options.GeneratedAccessTokenLifetimeMinutes);

        var userId = string.IsNullOrWhiteSpace(options.UserId)
            ? "11111111-1111-1111-1111-111111111111"
            : options.UserId.Trim();
        var username = string.IsNullOrWhiteSpace(options.Username)
            ? "swagger-dev-admin"
            : options.Username.Trim();
        var displayName = string.IsNullOrWhiteSpace(options.DisplayName)
            ? username
            : options.DisplayName.Trim();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new("display_name", displayName)
        };

        claims.AddRange(NormalizeClaims(options.Roles, BookBackendApiRoles.Admin).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(NormalizeClaims(options.Permissions, BookBackendApiPermissions.Admin).Select(permission => new Claim(BookBackendApiClaimTypes.Permission, permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string BuildSwaggerSecurityDescription(string baseDescription, DevelopmentSwaggerBearerOptions options)
    {
        if (!options.IsEnabled)
        {
            return baseDescription;
        }

        return $"{baseDescription} Local development shortcut token: {options.Token}.";
    }

    private static IReadOnlyCollection<string> NormalizeClaims(string[]? values, string fallback)
    {
        var normalized = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized is { Length: > 0 }
            ? normalized
            : [fallback];
    }
}
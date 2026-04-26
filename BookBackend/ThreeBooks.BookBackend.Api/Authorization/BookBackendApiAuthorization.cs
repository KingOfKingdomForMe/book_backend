using System.Security.Claims;

namespace ThreeBooks.BookBackend.Api.Authorization;

public sealed class ApiJwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ThreeBooks.BookBackend.LoginService";

    public string Audience { get; set; } = "ThreeBooks.Clients";

    public string SigningKey { get; set; } = string.Empty;
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
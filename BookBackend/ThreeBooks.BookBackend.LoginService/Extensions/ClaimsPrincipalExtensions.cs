using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;

namespace ThreeBooks.BookBackend.LoginService.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var rawValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(rawValue, out var userId))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "The current access token does not contain a valid user id.");
        }

        return userId;
    }
}
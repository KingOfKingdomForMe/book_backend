using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ThreeBooks.BookBackend.LoginService.Api.ErrorHandling;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Contracts.Responses;
using ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence;

namespace ThreeBooks.BookBackend.LoginService.Application.Services;

public sealed class UserService(LoginDbContext dbContext) : IUserService
{
    private readonly LoginDbContext _dbContext = dbContext;

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(entity => entity.UserRoles)
            .ThenInclude(entity => entity.Role)
            .ThenInclude(entity => entity.RolePermissions)
            .ThenInclude(entity => entity.Permission)
            .FirstOrDefaultAsync(entity => entity.Id == userId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User was not found.");

        var roleCodes = user.UserRoles
            .Select(entity => entity.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToArray();

        var permissionCodes = user.UserRoles
            .SelectMany(entity => entity.Role.RolePermissions)
            .Select(entity => entity.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToArray();

        return new CurrentUserResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Mobile,
            user.IsActive,
            roleCodes,
            permissionCodes);
    }
}
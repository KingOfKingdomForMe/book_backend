using Microsoft.AspNetCore.Authorization;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

[Authorize]
public abstract class AdminApiControllerBase : ApiControllerBase
{
}
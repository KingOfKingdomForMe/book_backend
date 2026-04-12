using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.LoginService.Application.Abstractions;
using ThreeBooks.BookBackend.LoginService.Extensions;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected RequestContext BuildRequestContext() =>
        new(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

    protected Guid GetRequiredUserId() => User.GetRequiredUserId();
}
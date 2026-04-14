using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Common;

namespace ThreeBooks.BookBackend.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected RequestContext BuildRequestContext()
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        return new RequestContext(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
    }
}
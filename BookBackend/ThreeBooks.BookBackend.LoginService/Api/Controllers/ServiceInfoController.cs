using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

[Route("api")]
public sealed class ServiceInfoController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus() =>
        Ok(new
        {
            service = "ThreeBooks.BookBackend.LoginService",
            status = "running",
            timestampUtc = DateTimeOffset.UtcNow
        });
}
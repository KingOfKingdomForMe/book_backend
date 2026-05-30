using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ThreeBooks.BookBackend.LoginService.Api.Controllers;

/// <summary>
/// 登录服务基础运行状态接口。
/// </summary>
[Route("api")]
public sealed class ServiceInfoController : ApiControllerBase
{
    /// <summary>
    /// 获取登录服务健康状态与当前时间。
    /// </summary>
    /// <remarks>
    /// 可用于部署探活、反向代理健康检查或本地联调时确认服务是否已启动。
    /// </remarks>
    /// <response code="200">服务运行正常。</response>
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
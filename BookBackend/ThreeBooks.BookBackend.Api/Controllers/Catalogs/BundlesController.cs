using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Catalogs;

/// <summary>
/// 套装商品查询接口。
/// </summary>
[Route("api/bundles")]
public sealed class BundlesController(ICatalogService catalogService) : ApiControllerBase
{
    /// <summary>
    /// 分页获取套装列表。
    /// </summary>
    /// <remarks>
    /// 适用于展示组合套餐、促销套装等内容。建议结合分页参数逐步加载，避免首页请求过重。
    /// </remarks>
    /// <param name="request">套装分页查询参数。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页套装列表。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<BundleListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BundleListItemResponse>>> GetListAsync(
        [FromQuery] ListBundlesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetBundlesAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }
}

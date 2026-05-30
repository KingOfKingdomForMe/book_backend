using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Catalogs;

/// <summary>
/// 商品分类查询接口。
/// </summary>
[Route("api/categories")]
public sealed class CategoriesController(ICatalogService catalogService) : ApiControllerBase
{
    /// <summary>
    /// 分页获取商品分类列表。
    /// </summary>
    /// <remarks>
    /// 适用于商城首页分类导航或筛选条件初始化。分类通常变化较少，可在客户端做短期缓存。
    /// </remarks>
    /// <param name="request">分类分页查询参数。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页分类列表。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> GetListAsync(
        [FromQuery] ListCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetCategoriesAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }
}

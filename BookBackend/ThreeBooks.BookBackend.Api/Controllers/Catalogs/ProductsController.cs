using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Catalogs.Interfaces;
using ThreeBooks.BookBackend.Contracts.Catalogs.Requests;
using ThreeBooks.BookBackend.Contracts.Catalogs.Responses;
using ThreeBooks.BookBackend.Contracts.Common;

namespace ThreeBooks.BookBackend.Api.Controllers.Catalogs;

/// <summary>
/// 商品目录查询接口。
/// </summary>
[Route("api/products")]
public sealed class ProductsController(ICatalogService catalogService) : ApiControllerBase
{
    /// <summary>
    /// 分页查询商品列表。
    /// </summary>
    /// <remarks>
    /// 可以按分类 ID、分类别名或关键字筛选。返回结果同时包含当前产品绑定的默认相册编码，便于前端在选择产品后直接关联默认相册初始化作品。
    /// </remarks>
    /// <param name="request">商品列表查询参数，包含分类过滤条件、关键字和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页商品列表。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListItemResponse>>> GetListAsync(
        [FromQuery] ListProductsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetProductsAsync(request, BuildRequestContext(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// 获取单个商品详情。
    /// </summary>
    /// <remarks>
    /// 适用于商品详情页加载。返回结果通常包含 SKU、价格及商品展示信息，以及当前产品绑定的默认相册编码。
    /// </remarks>
    /// <param name="id">商品主键标识。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回商品详情。</response>
    /// <response code="404">商品不存在。</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailResponse>> GetDetailAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await catalogService.GetProductDetailAsync(id, BuildRequestContext(), cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }
}

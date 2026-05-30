using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Orders.Requests;
using ThreeBooks.BookBackend.Contracts.Orders.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Orders;

/// <summary>
/// 用户订单与收货地址接口。
/// </summary>
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
    /// <summary>
    /// 获取指定用户的收货地址列表。
    /// </summary>
    /// <remarks>
    /// 建议在结算页初始化时调用。`userId` 应传当前业务用户标识，而不是登录服务中的 GUID 用户主键。
    /// </remarks>
    /// <param name="userId">业务用户标识。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回该用户可用的收货地址集合。</response>
    /// <response code="400">用户标识非法。</response>
    [HttpGet("shipping-addresses")]
    [ProducesResponseType<IReadOnlyCollection<ShippingAddressResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<ShippingAddressResponse>>> GetShippingAddressesAsync(
        [FromQuery] long userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.GetShippingAddressesAsync(userId, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 新增或更新收货地址。
    /// </summary>
    /// <remarks>
    /// 当 `AddressId` 为空时表示新增，否则表示更新已有地址。若设置 `IsDefault=true`，建议前端同步刷新地址列表。
    /// </remarks>
    /// <param name="request">收货地址请求体，包含用户标识、收件信息、行政区划和是否默认等字段。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">地址保存成功，并返回最新地址信息。</response>
    /// <response code="400">地址数据不合法。</response>
    [HttpPost("shipping-addresses")]
    [ProducesResponseType<ShippingAddressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShippingAddressResponse>> UpsertShippingAddressAsync(
        [FromBody] UpsertShippingAddressRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.UpsertShippingAddressAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 创建订单。
    /// </summary>
    /// <remarks>
    /// 下单前建议先确认商品 SKU、收货地址和项目版本等业务数据已准备完成。金额相关字段默认以服务端校验结果为准。
    /// </remarks>
    /// <param name="request">订单创建请求体，包含用户、项目、收货地址、SKU、数量、支付方式及优惠金额信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="201">订单创建成功。</response>
    /// <response code="400">订单参数不合法。</response>
    [HttpPost]
    [ProducesResponseType<CreateOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateOrderResponse>> CreateAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.CreateOrderAsync(request, BuildRequestContext(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 分页查询用户订单列表。
    /// </summary>
    /// <remarks>
    /// 建议订单中心按状态分页拉取。`Status` 为空时表示不过滤订单状态。
    /// </remarks>
    /// <param name="request">订单列表查询参数，包含用户标识、订单状态和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回分页订单列表。</response>
    /// <response code="400">查询参数非法。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderListItemResponse>>> GetListAsync(
        [FromQuery] ListOrdersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.GetOrdersAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取单个订单详情。
    /// </summary>
    /// <remarks>
    /// 需要同时传入 `orderNo` 与对应的 `userId`，便于服务端校验订单归属。适合订单详情页或售后页加载时调用。
    /// </remarks>
    /// <param name="orderNo">订单编号。</param>
    /// <param name="userId">业务用户标识。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回订单详情。</response>
    /// <response code="404">订单不存在或不属于指定用户。</response>
    /// <response code="400">请求参数非法。</response>
    [HttpGet("{orderNo}")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDetailResponse>> GetDetailAsync(
        string orderNo,
        [FromQuery] long userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.GetOrderDetailAsync(userId, orderNo, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
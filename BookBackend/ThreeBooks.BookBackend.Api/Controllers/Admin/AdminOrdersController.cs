using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Orders.Requests;
using ThreeBooks.BookBackend.Contracts.Orders.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

/// <summary>
/// 后台订单管理接口。
/// </summary>
[Authorize(Policy = BookBackendApiAuthorizationPolicies.OrderManage)]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(IOrderService orderService) : AdminApiControllerBase
{
    /// <summary>
    /// 分页查询后台订单列表。
    /// </summary>
    /// <remarks>
    /// 支持按用户、关键字和状态筛选，适合后台订单管理列表页。关键字通常可用于模糊搜索订单号或收件信息。
    /// </remarks>
    /// <param name="request">后台订单查询参数，包含筛选条件和分页信息。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回后台订单分页列表。</response>
    /// <response code="400">查询参数非法。</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OrderListItemResponse>>> GetListAsync(
        [FromQuery] AdminListOrdersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.GetAdminOrdersAsync(request, BuildRequestContext(), cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 获取后台订单详情。
    /// </summary>
    /// <remarks>
    /// 返回结果通常包含订单基础信息、收货信息、支付信息和物流轨迹，适合客服或运营后台查看。
    /// </remarks>
    /// <param name="orderNo">订单编号。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">返回订单详情。</response>
    /// <response code="404">订单不存在。</response>
    /// <response code="400">订单编号非法。</response>
    [HttpGet("{orderNo}")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDetailResponse>> GetDetailAsync(
        string orderNo,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.GetAdminOrderDetailAsync(orderNo, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 更新后台订单状态与物流字段。
    /// </summary>
    /// <remarks>
    /// 适用于支付确认、关闭订单、录入运单等后台操作。时间字段统一使用 UTC，便于多时区一致处理。
    /// </remarks>
    /// <param name="orderNo">订单编号。</param>
    /// <param name="request">后台订单更新请求体，包含订单状态、备注、支付时间和物流字段。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">订单更新成功。</response>
    /// <response code="404">订单不存在。</response>
    /// <response code="400">请求参数不合法。</response>
    [HttpPut("{orderNo}")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDetailResponse>> UpdateAsync(
        string orderNo,
        [FromBody] AdminUpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.UpdateAdminOrderAsync(orderNo, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// 为订单追加物流事件。
    /// </summary>
    /// <remarks>
    /// 常用于手动补录或同步物流轨迹。若同时传入 `ShipmentStatus`、`ShippedAtUtc` 或 `DeliveredAtUtc`，服务端会同步更新订单物流状态。
    /// </remarks>
    /// <param name="orderNo">订单编号。</param>
    /// <param name="request">物流事件请求体，包含事件描述、时间、地点以及可选的物流状态更新字段。</param>
    /// <param name="cancellationToken">请求取消令牌。</param>
    /// <response code="200">物流事件添加成功，并返回最新订单详情。</response>
    /// <response code="404">订单不存在。</response>
    /// <response code="400">事件数据不合法。</response>
    [HttpPost("{orderNo}/shipment-events")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDetailResponse>> AddShipmentEventAsync(
        string orderNo,
        [FromBody] AdminAddShipmentEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await orderService.AddAdminShipmentEventAsync(orderNo, request, BuildRequestContext(), cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Api.Authorization;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Orders.Requests;
using ThreeBooks.BookBackend.Contracts.Orders.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Admin;

[Authorize(Policy = BookBackendApiAuthorizationPolicies.OrderManage)]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(IOrderService orderService) : AdminApiControllerBase
{
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
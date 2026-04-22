using Microsoft.AspNetCore.Mvc;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Orders.Requests;
using ThreeBooks.BookBackend.Contracts.Orders.Responses;

namespace ThreeBooks.BookBackend.Api.Controllers.Orders;

[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
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
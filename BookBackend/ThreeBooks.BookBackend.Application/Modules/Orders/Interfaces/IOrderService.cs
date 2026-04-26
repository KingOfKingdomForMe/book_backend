using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Orders.Requests;
using ThreeBooks.BookBackend.Contracts.Orders.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;

public interface IOrderService
{
    Task<PagedResult<OrderListItemResponse>> GetAdminOrdersAsync(
        AdminListOrdersRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<OrderDetailResponse?> GetAdminOrderDetailAsync(
        string orderNo,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<OrderDetailResponse?> UpdateAdminOrderAsync(
        string orderNo,
        AdminUpdateOrderRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<OrderDetailResponse?> AddAdminShipmentEventAsync(
        string orderNo,
        AdminAddShipmentEventRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShippingAddressResponse>> GetShippingAddressesAsync(
        long userId,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<ShippingAddressResponse> UpsertShippingAddressAsync(
        UpsertShippingAddressRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<CreateOrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<PagedResult<OrderListItemResponse>> GetOrdersAsync(
        ListOrdersRequest request,
        RequestContext context,
        CancellationToken cancellationToken);

    Task<OrderDetailResponse?> GetOrderDetailAsync(
        long userId,
        string orderNo,
        RequestContext context,
        CancellationToken cancellationToken);
}
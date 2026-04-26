using ThreeBooks.BookBackend.Application.Modules.Orders.Models;

namespace ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;

public interface IOrderQueryStore
{
    Task<OrderListQueryResultModel> GetAdminOrdersAsync(
        AdminOrderListFilter filter,
        CancellationToken cancellationToken);

    Task<OrderDetailQueryModel?> GetAdminOrderDetailAsync(
        string orderNo,
        CancellationToken cancellationToken);

    Task<bool> UpdateAdminOrderAsync(
        string orderNo,
        AdminOrderUpdateCommandModel command,
        CancellationToken cancellationToken);

    Task<bool> AddAdminShipmentEventAsync(
        string orderNo,
        OrderShipmentEventCreateCommandModel command,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShippingAddressQueryModel>> GetShippingAddressesAsync(
        long userId,
        CancellationToken cancellationToken);

    Task<ShippingAddressQueryModel> UpsertShippingAddressAsync(
        ShippingAddressWriteCommandModel command,
        CancellationToken cancellationToken);

    Task<OrderCreateResultModel> CreateOrderAsync(
        OrderCreateCommandModel command,
        CancellationToken cancellationToken);

    Task<OrderListQueryResultModel> GetOrdersAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken);

    Task<OrderDetailQueryModel?> GetOrderDetailAsync(
        long userId,
        string orderNo,
        CancellationToken cancellationToken);
}
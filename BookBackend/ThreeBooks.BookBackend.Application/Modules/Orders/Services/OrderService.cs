using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ThreeBooks.BookBackend.Application.Common;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Orders.Models;
using ThreeBooks.BookBackend.Contracts.Common;
using ThreeBooks.BookBackend.Contracts.Orders.Requests;
using ThreeBooks.BookBackend.Contracts.Orders.Responses;

namespace ThreeBooks.BookBackend.Application.Modules.Orders.Services;

public sealed class OrderService(IOrderQueryStore queryStore) : IOrderService
{
    private const int DefaultPageNumber = 1;

    private const int DefaultPageSize = 20;

    private const int MaxPageSize = 100;

    private static readonly HashSet<string> AllowedPayChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "wechat_pay",
        "alipay",
        "agent_transfer"
    };

    private static readonly Regex OrderNoPattern = new("^[A-Za-z0-9_-]{6,32}$", RegexOptions.Compiled);

    public Task<IReadOnlyCollection<ShippingAddressResponse>> GetShippingAddressesAsync(
        long userId,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        return GetShippingAddressesCoreAsync(
            NormalizePositiveId(userId, nameof(userId)),
            cancellationToken);
    }

    public async Task<ShippingAddressResponse> UpsertShippingAddressAsync(
        UpsertShippingAddressRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ShippingAddressWriteCommandModel(
            NormalizeNullableId(request.AddressId, nameof(request.AddressId)),
            NormalizePositiveId(request.UserId, nameof(request.UserId)),
            NormalizeRequiredText(request.ReceiverName, nameof(request.ReceiverName), 64),
            NormalizeRequiredText(request.Phone, nameof(request.Phone), 20),
            NormalizeRequiredText(request.Province, nameof(request.Province), 32),
            NormalizeRequiredText(request.City, nameof(request.City), 32),
            NormalizeOptionalText(request.District, 32, nameof(request.District)),
            NormalizeRequiredText(request.AddressDetail, nameof(request.AddressDetail), 256),
            NormalizeOptionalText(request.PostalCode, 10, nameof(request.PostalCode)),
            request.IsDefault);

        var result = await queryStore.UpsertShippingAddressAsync(command, cancellationToken);
        return MapShippingAddress(result);
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new OrderCreateCommandModel(
            NormalizePositiveId(request.UserId, nameof(request.UserId)),
            NormalizePositiveId(request.ProjectId, nameof(request.ProjectId)),
            NormalizePositiveId(request.ShippingAddressId, nameof(request.ShippingAddressId)),
            NormalizePositiveId(request.SkuId, nameof(request.SkuId)),
            NormalizeQuantity(request.Quantity),
            NormalizePayChannel(request.PayChannel),
            NormalizeAmount(request.DiscountAmount, nameof(request.DiscountAmount)),
            NormalizeAmount(request.FreightAmount, nameof(request.FreightAmount)),
            NormalizeOptionalText(request.Remark, 500, nameof(request.Remark)));

        var result = await queryStore.CreateOrderAsync(command, cancellationToken);

        return new CreateOrderResponse(
            result.OrderId,
            result.OrderNo,
            result.Status,
            result.TotalAmount,
            result.DiscountAmount,
            result.FreightAmount,
            result.PayAmount,
            result.PaymentId,
            result.PaymentNo,
            result.PaymentExpiredAt);
    }

    public async Task<PagedResult<OrderListItemResponse>> GetOrdersAsync(
        ListOrdersRequest request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new OrderListFilter(
            NormalizePositiveId(request.UserId, nameof(request.UserId)),
            NormalizeStatusFilter(request.Status),
            NormalizePageNumber(request.PageNumber),
            NormalizePageSize(request.PageSize));

        var result = await queryStore.GetOrdersAsync(filter, cancellationToken);

        var items = result.Items
            .Select(item => new OrderListItemResponse(
                item.OrderId,
                item.OrderNo,
                item.Status,
                item.ItemCount,
                item.TotalAmount,
                item.PayAmount,
                item.ProjectTitle,
                item.ProductName,
                item.SkuCode,
                item.Quantity,
                item.CreatedAt,
                item.PaidAt,
                item.ShipmentNo,
                item.ShipmentStatus))
            .ToArray();

        return new PagedResult<OrderListItemResponse>(
            items,
            filter.PageNumber,
            filter.PageSize,
            result.TotalCount);
    }

    public async Task<OrderDetailResponse?> GetOrderDetailAsync(
        long userId,
        string orderNo,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var normalizedUserId = NormalizePositiveId(userId, nameof(userId));
        var normalizedOrderNo = NormalizeOrderNo(orderNo);

        var detail = await queryStore.GetOrderDetailAsync(normalizedUserId, normalizedOrderNo, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        return new OrderDetailResponse(
            detail.OrderId,
            detail.OrderNo,
            detail.UserId,
            detail.OrderType,
            detail.Status,
            detail.ItemCount,
            detail.TotalAmount,
            detail.DiscountAmount,
            detail.FreightAmount,
            detail.PayAmount,
            new OrderShippingSnapshotResponse(
                detail.ShippingSnapshot.ReceiverName,
                detail.ShippingSnapshot.Phone,
                detail.ShippingSnapshot.Province,
                detail.ShippingSnapshot.City,
                detail.ShippingSnapshot.District,
                detail.ShippingSnapshot.Address,
                detail.ShippingSnapshot.PostalCode),
            detail.Remark,
            detail.CreatedAt,
            detail.PaidAt,
            detail.ClosedAt,
            detail.Items.Select(MapOrderItem).ToArray(),
            detail.Payment is null
                ? null
                : new OrderPaymentResponse(
                    detail.Payment.PaymentId,
                    detail.Payment.PaymentNo,
                    detail.Payment.PayChannel,
                    detail.Payment.PayAmount,
                    detail.Payment.Status,
                    detail.Payment.PaidAt,
                    detail.Payment.ExpiredAt),
            detail.Shipment is null
                ? null
                : new OrderShipmentResponse(
                    detail.Shipment.ShipmentId,
                    detail.Shipment.ShipmentNo,
                    detail.Shipment.CarrierCode,
                    detail.Shipment.CarrierName,
                    detail.Shipment.Status,
                    detail.Shipment.ShippedAt,
                    detail.Shipment.DeliveredAt,
                    detail.Shipment.Events.Select(eventItem => new OrderShipmentEventResponse(
                        eventItem.EventTime,
                        eventItem.EventDescription,
                        eventItem.Location)).ToArray()));
    }

    private async Task<IReadOnlyCollection<ShippingAddressResponse>> GetShippingAddressesCoreAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var addresses = await queryStore.GetShippingAddressesAsync(userId, cancellationToken);
        return addresses.Select(MapShippingAddress).ToArray();
    }

    private static ShippingAddressResponse MapShippingAddress(ShippingAddressQueryModel address)
    {
        return new ShippingAddressResponse(
            address.AddressId,
            address.ReceiverName,
            address.Phone,
            address.Province,
            address.City,
            address.District,
            address.AddressDetail,
            address.PostalCode,
            address.IsDefault);
    }

    private static OrderDetailItemResponse MapOrderItem(OrderItemDetailModel item)
    {
        return new OrderDetailItemResponse(
            item.ItemNo,
            item.ProjectId,
            item.ProjectVersionId,
            item.SkuId,
            item.Quantity,
            item.UnitPrice,
            item.Subtotal,
            item.Status,
            item.ProjectTitle,
            item.PriceSnapshot is null
                ? null
                : new OrderPriceSnapshotResponse(
                    item.PriceSnapshot.SpuCode,
                    item.PriceSnapshot.SpuName,
                    item.PriceSnapshot.SkuCode,
                    item.PriceSnapshot.SizeLabel,
                    item.PriceSnapshot.BindingLabel,
                    item.PriceSnapshot.LayoutLabel,
                    item.PriceSnapshot.CoverDescription,
                    item.PriceSnapshot.PageCount,
                    item.PriceSnapshot.ImageCount,
                    item.PriceSnapshot.BasePrice,
                    item.PriceSnapshot.PageUnitPrice,
                    item.PriceSnapshot.CalculatedPrice,
                    item.PriceSnapshot.AgentMarkup,
                    item.PriceSnapshot.Discount,
                    item.PriceSnapshot.FinalPrice,
                    item.PriceSnapshot.IsBundleItem,
                    item.PriceSnapshot.BundleCode,
                    item.PriceSnapshot.BundleName,
                    item.PriceSnapshot.EstimatedShipDate));
    }

    private static long NormalizePositiveId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than 0.");
        }

        return value;
    }

    private static long? NormalizeNullableId(long? value, string parameterName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return NormalizePositiveId(value.Value, parameterName);
    }

    private static string NormalizeRequiredText(string? value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value length cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static int NormalizeQuantity(int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than or equal to 1.");
        }

        if (quantity > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot exceed 99.");
        }

        return quantity;
    }

    private static string NormalizePayChannel(string? payChannel)
    {
        var normalized = payChannel?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Pay channel is required.", nameof(payChannel));
        }

        if (!AllowedPayChannels.Contains(normalized))
        {
            throw new ArgumentException("Unsupported pay channel.", nameof(payChannel));
        }

        return normalized;
    }

    private static decimal NormalizeAmount(decimal amount, string parameterName)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Amount cannot be negative.");
        }

        return RoundMoney(amount);
    }

    private static int NormalizePageNumber(int pageNumber)
    {
        return pageNumber < 1 ? DefaultPageNumber : pageNumber;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1)
        {
            return DefaultPageSize;
        }

        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }

    private static int? NormalizeStatusFilter(int? status)
    {
        if (!status.HasValue)
        {
            return null;
        }

        if (status.Value is < 0 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Status must be between 0 and 8.");
        }

        return status.Value;
    }

    private static string NormalizeOrderNo(string orderNo)
    {
        var normalized = orderNo?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Order number is required.", nameof(orderNo));
        }

        if (!OrderNoPattern.IsMatch(normalized))
        {
            throw new ArgumentException("Order number format is invalid.", nameof(orderNo));
        }

        return normalized;
    }

    private static decimal RoundMoney(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
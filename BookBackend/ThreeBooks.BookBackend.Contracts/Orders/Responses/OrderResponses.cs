namespace ThreeBooks.BookBackend.Contracts.Orders.Responses;

public sealed record ShippingAddressResponse(
    long AddressId,
    string ReceiverName,
    string Phone,
    string Province,
    string City,
    string? District,
    string AddressDetail,
    string? PostalCode,
    bool IsDefault);

public sealed record CreateOrderResponse(
    long OrderId,
    string OrderNo,
    int Status,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FreightAmount,
    decimal PayAmount,
    long PaymentId,
    string PaymentNo,
    DateTime? PaymentExpiredAt);

public sealed record OrderListItemResponse(
    long OrderId,
    string OrderNo,
    int Status,
    int ItemCount,
    decimal TotalAmount,
    decimal PayAmount,
    string? ProjectTitle,
    string ProductName,
    string SkuCode,
    int Quantity,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? ShipmentNo,
    int? ShipmentStatus);

public sealed record OrderDetailResponse(
    long OrderId,
    string OrderNo,
    long UserId,
    string OrderType,
    int Status,
    int ItemCount,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FreightAmount,
    decimal PayAmount,
    OrderShippingSnapshotResponse Shipping,
    string? Remark,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? ClosedAt,
    IReadOnlyCollection<OrderDetailItemResponse> Items,
    OrderPaymentResponse? Payment,
    OrderShipmentResponse? Shipment);

public sealed record OrderShippingSnapshotResponse(
    string ReceiverName,
    string Phone,
    string Province,
    string City,
    string? District,
    string Address,
    string? PostalCode);

public sealed record OrderDetailItemResponse(
    int ItemNo,
    long? ProjectId,
    long? ProjectVersionId,
    long? SkuId,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    int Status,
    string? ProjectTitle,
    OrderPriceSnapshotResponse? PriceSnapshot);

public sealed record OrderPriceSnapshotResponse(
    string SpuCode,
    string SpuName,
    string SkuCode,
    string? SizeLabel,
    string? BindingLabel,
    string? LayoutLabel,
    string? CoverDescription,
    int PageCount,
    int ImageCount,
    decimal BasePrice,
    decimal PageUnitPrice,
    decimal CalculatedPrice,
    decimal AgentMarkup,
    decimal Discount,
    decimal FinalPrice,
    bool IsBundleItem,
    string? BundleCode,
    string? BundleName,
    DateTime? EstimatedShipDate);

public sealed record OrderPaymentResponse(
    long PaymentId,
    string PaymentNo,
    string PayChannel,
    decimal PayAmount,
    int Status,
    DateTime? PaidAt,
    DateTime? ExpiredAt);

public sealed record OrderShipmentResponse(
    long ShipmentId,
    string ShipmentNo,
    string CarrierCode,
    string? CarrierName,
    int Status,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    IReadOnlyCollection<OrderShipmentEventResponse> Events);

public sealed record OrderShipmentEventResponse(
    DateTime EventTime,
    string EventDescription,
    string? Location);
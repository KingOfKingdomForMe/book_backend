namespace ThreeBooks.BookBackend.Application.Modules.Orders.Models;

public sealed record ShippingAddressWriteCommandModel(
    long? AddressId,
    long UserId,
    string ReceiverName,
    string Phone,
    string Province,
    string City,
    string? District,
    string AddressDetail,
    string? PostalCode,
    bool IsDefault);

public sealed record ShippingAddressQueryModel(
    long AddressId,
    string ReceiverName,
    string Phone,
    string Province,
    string City,
    string? District,
    string AddressDetail,
    string? PostalCode,
    bool IsDefault);

public sealed record OrderCreateCommandModel(
    long UserId,
    long ProjectId,
    long ShippingAddressId,
    long SkuId,
    int Quantity,
    string PayChannel,
    decimal DiscountAmount,
    decimal FreightAmount,
    string? Remark);

public sealed record OrderCreateResultModel(
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

public sealed record OrderListFilter(
    long UserId,
    int? Status,
    int PageNumber,
    int PageSize);

public sealed record OrderListQueryResultModel(
    IReadOnlyCollection<OrderListItemQueryModel> Items,
    int TotalCount);

public sealed record OrderListItemQueryModel(
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

public sealed record OrderDetailQueryModel(
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
    OrderShippingSnapshotModel ShippingSnapshot,
    string? Remark,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? ClosedAt,
    IReadOnlyCollection<OrderItemDetailModel> Items,
    OrderPaymentDetailModel? Payment,
    OrderShipmentDetailModel? Shipment);

public sealed record OrderShippingSnapshotModel(
    string ReceiverName,
    string Phone,
    string Province,
    string City,
    string? District,
    string Address,
    string? PostalCode);

public sealed record OrderItemDetailModel(
    int ItemNo,
    long? ProjectId,
    long? ProjectVersionId,
    long? SkuId,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    int Status,
    string? ProjectTitle,
    OrderPriceSnapshotModel? PriceSnapshot);

public sealed record OrderPriceSnapshotModel(
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

public sealed record OrderPaymentDetailModel(
    long PaymentId,
    string PaymentNo,
    string PayChannel,
    decimal PayAmount,
    int Status,
    DateTime? PaidAt,
    DateTime? ExpiredAt);

public sealed record OrderShipmentDetailModel(
    long ShipmentId,
    string ShipmentNo,
    string CarrierCode,
    string? CarrierName,
    int Status,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    IReadOnlyCollection<OrderShipmentEventModel> Events);

public sealed record OrderShipmentEventModel(
    DateTime EventTime,
    string EventDescription,
    string? Location);
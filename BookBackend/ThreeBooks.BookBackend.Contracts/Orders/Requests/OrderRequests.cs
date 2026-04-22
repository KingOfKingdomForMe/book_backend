namespace ThreeBooks.BookBackend.Contracts.Orders.Requests;

public sealed record UpsertShippingAddressRequest(
    long UserId,
    long? AddressId,
    string ReceiverName,
    string Phone,
    string Province,
    string City,
    string? District,
    string AddressDetail,
    string? PostalCode,
    bool IsDefault = false);

public sealed record CreateOrderRequest(
    long UserId,
    long ProjectId,
    long ShippingAddressId,
    long SkuId,
    int Quantity = 1,
    string PayChannel = "wechat_pay",
    decimal DiscountAmount = 0,
    decimal FreightAmount = 0,
    string? Remark = null);

public sealed record ListOrdersRequest(
    long UserId,
    int? Status = null,
    int PageNumber = 1,
    int PageSize = 20);
namespace ThreeBooks.BookBackend.Contracts.Orders.Requests;

public sealed record AdminListOrdersRequest(
    long? UserId = null,
    string? Keyword = null,
    int? Status = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record AdminUpdateOrderRequest(
    int Status,
    string? Remark = null,
    DateTime? PaidAtUtc = null,
    DateTime? ClosedAtUtc = null,
    string? ShipmentNo = null,
    string? CarrierCode = null,
    string? CarrierName = null,
    int? ShipmentStatus = null,
    DateTime? ShippedAtUtc = null,
    DateTime? DeliveredAtUtc = null);

public sealed record AdminAddShipmentEventRequest(
    string EventDescription,
    DateTime? EventTimeUtc = null,
    string? Location = null,
    int? ShipmentStatus = null,
    DateTime? ShippedAtUtc = null,
    DateTime? DeliveredAtUtc = null);
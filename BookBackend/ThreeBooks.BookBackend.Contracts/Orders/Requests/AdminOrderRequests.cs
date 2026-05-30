namespace ThreeBooks.BookBackend.Contracts.Orders.Requests;

/// <summary>
/// 后台订单列表查询参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="Keyword">关键字，可用于搜索订单号或收件信息。</param>
/// <param name="Status">订单状态。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record AdminListOrdersRequest(
    long? UserId = null,
    string? Keyword = null,
    int? Status = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// 后台更新订单请求参数。
/// </summary>
/// <param name="Status">订单状态值。</param>
/// <param name="Remark">后台备注。</param>
/// <param name="PaidAtUtc">支付完成时间，UTC。</param>
/// <param name="ClosedAtUtc">订单关闭时间，UTC。</param>
/// <param name="ShipmentNo">物流单号。</param>
/// <param name="CarrierCode">承运商编码。</param>
/// <param name="CarrierName">承运商名称。</param>
/// <param name="ShipmentStatus">物流状态值。</param>
/// <param name="ShippedAtUtc">发货时间，UTC。</param>
/// <param name="DeliveredAtUtc">签收时间，UTC。</param>
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

/// <summary>
/// 后台追加物流事件请求参数。
/// </summary>
/// <param name="EventDescription">物流事件描述。</param>
/// <param name="EventTimeUtc">事件时间，UTC。</param>
/// <param name="Location">事件发生地点。</param>
/// <param name="ShipmentStatus">物流状态值。</param>
/// <param name="ShippedAtUtc">发货时间，UTC。</param>
/// <param name="DeliveredAtUtc">签收时间，UTC。</param>
public sealed record AdminAddShipmentEventRequest(
    string EventDescription,
    DateTime? EventTimeUtc = null,
    string? Location = null,
    int? ShipmentStatus = null,
    DateTime? ShippedAtUtc = null,
    DateTime? DeliveredAtUtc = null);
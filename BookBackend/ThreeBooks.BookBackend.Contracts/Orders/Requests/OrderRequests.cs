namespace ThreeBooks.BookBackend.Contracts.Orders.Requests;

/// <summary>
/// 新增或更新收货地址请求参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="AddressId">地址标识，为空表示新增。</param>
/// <param name="ReceiverName">收件人姓名。</param>
/// <param name="Phone">联系电话。</param>
/// <param name="Province">省份。</param>
/// <param name="City">城市。</param>
/// <param name="District">区县。</param>
/// <param name="AddressDetail">详细地址。</param>
/// <param name="PostalCode">邮编。</param>
/// <param name="IsDefault">是否默认地址。</param>
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

/// <summary>
/// 创建订单请求参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="ProjectId">关联项目标识。</param>
/// <param name="ShippingAddressId">收货地址标识。</param>
/// <param name="SkuId">商品 SKU 标识。</param>
/// <param name="Quantity">购买数量。</param>
/// <param name="PayChannel">支付渠道编码。</param>
/// <param name="DiscountAmount">优惠金额。</param>
/// <param name="FreightAmount">运费金额。</param>
/// <param name="Remark">订单备注。</param>
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

/// <summary>
/// 用户订单列表查询参数。
/// </summary>
/// <param name="UserId">业务用户标识。</param>
/// <param name="Status">订单状态，可为空。</param>
/// <param name="PageNumber">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
public sealed record ListOrdersRequest(
    long UserId,
    int? Status = null,
    int PageNumber = 1,
    int PageSize = 20);
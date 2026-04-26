using System.Data;
using System.Security.Cryptography;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Orders.Models;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Orders;

public sealed class OrderQueryStore(string connectionString) : IOrderQueryStore
{
    private const string FindUserSql = "usp_Order_FindUser";
    private const string ListShippingAddressesSql = "usp_Order_ListShippingAddresses";
    private const string FindShippingAddressSql = "usp_Order_FindShippingAddress";
    private const string CountOtherAddressesSql = "usp_Order_CountOtherAddresses";
    private const string CountOtherDefaultAddressesSql = "usp_Order_CountOtherDefaultAddresses";
    private const string ClearDefaultShippingAddressesSql = "usp_Order_ClearDefaultShippingAddresses";
    private const string InsertShippingAddressSql = "usp_Order_InsertShippingAddress";
    private const string UpdateShippingAddressSql = "usp_Order_UpdateShippingAddress";
    private const string FindProjectForOrderSql = "usp_Order_FindProjectForOrder";
    private const string FindSkuForOrderSql = "usp_Order_FindSkuForOrder";
    private const string InsertOrderSql = "usp_Order_InsertOrder";
    private const string InsertOrderItemSql = "usp_Order_InsertOrderItem";
    private const string InsertPriceSnapshotSql = "usp_Order_InsertPriceSnapshot";
    private const string UpdateOrderItemSnapshotSql = "usp_Order_UpdateOrderItemSnapshot";
    private const string InsertPaymentSql = "usp_Order_InsertPayment";
    private const string UpdateProjectStatusSql = "usp_Order_UpdateProjectStatus";
    private const string CountOrdersSql = "usp_Order_CountOrders";
    private const string ListOrdersSql = "usp_Order_ListOrders";
    private const string FindOrderHeaderSql = "usp_Order_FindOrderHeader";
    private const string FindOrderItemsSql = "usp_Order_FindOrderItems";
    private const string FindShipmentEventsSql = "usp_Order_FindShipmentEvents";
    private const string UpdateAdminOrderSql = "usp_Order_UpdateAdminOrder";
    private const string InsertShipmentSql = "usp_Order_InsertShipment";
    private const string UpdateShipmentSql = "usp_Order_UpdateShipment";
    private const string InsertShipmentEventSql = "usp_Order_InsertShipmentEvent";
    private const string GetLastInsertIdSql = "usp_Common_GetLastInsertId";

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Order database connection string is required.", nameof(connectionString))
        : connectionString;

    public Task<OrderListQueryResultModel> GetAdminOrdersAsync(
        AdminOrderListFilter filter,
        CancellationToken cancellationToken)
    {
        return GetOrdersCoreAsync(
            filter.UserId,
            filter.Keyword,
            filter.Status,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken);
    }

    public Task<OrderDetailQueryModel?> GetAdminOrderDetailAsync(
        string orderNo,
        CancellationToken cancellationToken)
    {
        return GetOrderDetailCoreAsync(null, orderNo, cancellationToken);
    }

    public async Task<bool> UpdateAdminOrderAsync(
        string orderNo,
        AdminOrderUpdateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<OrderHeaderRow>(
            new CommandDefinition(
                FindOrderHeaderSql,
                new { p_user_id = (long?)null, p_order_no = orderNo },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (header is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateAdminOrderSql,
                new
                {
                    p_order_id = header.OrderId,
                    p_status = command.Status,
                    p_remark = NormalizeNullable(command.Remark),
                    p_paid_at = command.PaidAtUtc,
                    p_closed_at = command.ClosedAtUtc
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (!string.IsNullOrWhiteSpace(command.ShipmentNo) && !string.IsNullOrWhiteSpace(command.CarrierCode))
        {
            var shipmentParameters = new
            {
                p_order_id = header.OrderId,
                p_shipment_id = header.ShipmentId,
                p_shipment_no = command.ShipmentNo,
                p_carrier_code = command.CarrierCode,
                p_carrier_name = NormalizeNullable(command.CarrierName),
                p_status = command.ShipmentStatus ?? 0,
                p_shipped_at = command.ShippedAtUtc,
                p_delivered_at = command.DeliveredAtUtc
            };

            await connection.ExecuteAsync(
                new CommandDefinition(
                    header.ShipmentId.HasValue ? UpdateShipmentSql : InsertShipmentSql,
                    shipmentParameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddAdminShipmentEventAsync(
        string orderNo,
        OrderShipmentEventCreateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<OrderHeaderRow>(
            new CommandDefinition(
                FindOrderHeaderSql,
                new { p_user_id = (long?)null, p_order_no = orderNo },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (header is null || !header.ShipmentId.HasValue || string.IsNullOrWhiteSpace(header.ShipmentNo) || string.IsNullOrWhiteSpace(header.CarrierCode))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertShipmentEventSql,
                new
                {
                    p_shipment_id = header.ShipmentId.Value,
                    p_event_time = command.EventTimeUtc,
                    p_event_desc = command.EventDescription,
                    p_location = NormalizeNullable(command.Location)
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateShipmentSql,
                new
                {
                    p_order_id = header.OrderId,
                    p_shipment_id = header.ShipmentId.Value,
                    p_shipment_no = header.ShipmentNo,
                    p_carrier_code = header.CarrierCode,
                    p_carrier_name = NormalizeNullable(header.CarrierName),
                    p_status = command.ShipmentStatus,
                    p_shipped_at = command.ShippedAtUtc,
                    p_delivered_at = command.DeliveredAtUtc
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ShippingAddressQueryModel>> GetShippingAddressesAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ShippingAddressRow>(
            new CommandDefinition(
                ListShippingAddressesSql,
                new { p_user_id = userId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return rows.Select(MapShippingAddress).ToArray();
    }

    public async Task<ShippingAddressQueryModel> UpsertShippingAddressAsync(
        ShippingAddressWriteCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var userId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                FindUserSql,
                new { p_user_id = command.UserId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (!userId.HasValue)
        {
            throw new ArgumentException("User does not exist.", nameof(command.UserId));
        }

        ShippingAddressRow? existing = null;
        if (command.AddressId.HasValue)
        {
            existing = await connection.QuerySingleOrDefaultAsync<ShippingAddressRow>(
                new CommandDefinition(
                    FindShippingAddressSql,
                    new { p_address_id = command.AddressId.Value, p_user_id = command.UserId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            if (existing is null)
            {
                throw new ArgumentException("Shipping address does not exist.", nameof(command.AddressId));
            }
        }

        var otherAddressCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountOtherAddressesSql,
                new { p_user_id = command.UserId, p_address_id = command.AddressId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var otherDefaultCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountOtherDefaultAddressesSql,
                new { p_user_id = command.UserId, p_address_id = command.AddressId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var shouldSetDefault = command.IsDefault
            || otherAddressCount == 0
            || (existing is not null && existing.IsDefault && otherDefaultCount == 0);

        if (shouldSetDefault)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    ClearDefaultShippingAddressesSql,
                    new { p_user_id = command.UserId, p_address_id = command.AddressId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }

        long addressId;
        if (existing is null)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertShippingAddressSql,
                    new
                    {
                        p_user_id = command.UserId,
                        p_receiver_name = command.ReceiverName,
                        p_phone = command.Phone,
                        p_province = command.Province,
                        p_city = command.City,
                        p_district = NormalizeNullable(command.District),
                        p_address_detail = command.AddressDetail,
                        p_postal_code = NormalizeNullable(command.PostalCode),
                        p_is_default = shouldSetDefault
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            addressId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    GetLastInsertIdSql,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
        }
        else
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    UpdateShippingAddressSql,
                    new
                    {
                        p_address_id = existing.AddressId,
                        p_user_id = command.UserId,
                        p_receiver_name = command.ReceiverName,
                        p_phone = command.Phone,
                        p_province = command.Province,
                        p_city = command.City,
                        p_district = NormalizeNullable(command.District),
                        p_address_detail = command.AddressDetail,
                        p_postal_code = NormalizeNullable(command.PostalCode),
                        p_is_default = shouldSetDefault
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            addressId = existing.AddressId;
        }

        var saved = await connection.QuerySingleAsync<ShippingAddressRow>(
            new CommandDefinition(
                FindShippingAddressSql,
                new { p_address_id = addressId, p_user_id = command.UserId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return MapShippingAddress(saved);
    }

    public async Task<OrderCreateResultModel> CreateOrderAsync(
        OrderCreateCommandModel command,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var project = await connection.QuerySingleOrDefaultAsync<ProjectForOrderRow>(
            new CommandDefinition(
                FindProjectForOrderSql,
                new { p_project_id = command.ProjectId, p_user_id = command.UserId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (project is null)
        {
            throw new ArgumentException("Project does not exist for the current user.", nameof(command.ProjectId));
        }

        if (!project.ProjectVersionId.HasValue)
        {
            throw new ArgumentException("Project does not have a frozen version yet.", nameof(command.ProjectId));
        }

        if (project.ProjectStatus >= 3)
        {
            throw new ArgumentException("Project has already been ordered or archived.", nameof(command.ProjectId));
        }

        var pageCount = project.VersionPageCount > 0 ? project.VersionPageCount : project.ProjectPageCount;
        if (pageCount <= 0)
        {
            throw new ArgumentException("Project page count must be greater than 0 before creating an order.", nameof(command.ProjectId));
        }

        var imageCount = project.VersionImageCount > 0 ? project.VersionImageCount : project.ProjectImageCount;

        var sku = await connection.QuerySingleOrDefaultAsync<SkuForOrderRow>(
            new CommandDefinition(
                FindSkuForOrderSql,
                new { p_sku_id = command.SkuId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (sku is null)
        {
            throw new ArgumentException("SKU does not exist or is inactive.", nameof(command.SkuId));
        }

        if (!sku.PriceRuleId.HasValue)
        {
            throw new ArgumentException("SKU does not have an active price rule.", nameof(command.SkuId));
        }

        if (pageCount < sku.MinPages)
        {
            throw new ArgumentException($"Project page count must be at least {sku.MinPages} for the selected SKU.", nameof(command.ProjectId));
        }

        if (sku.MaxPages.HasValue && pageCount > sku.MaxPages.Value)
        {
            throw new ArgumentException($"Project page count cannot exceed {sku.MaxPages.Value} for the selected SKU.", nameof(command.ProjectId));
        }

        if (project.SpuId.HasValue && project.SpuId.Value != sku.SpuId)
        {
            throw new ArgumentException("SKU does not match the project product.", nameof(command.SkuId));
        }

        var shippingAddress = await connection.QuerySingleOrDefaultAsync<ShippingAddressRow>(
            new CommandDefinition(
                FindShippingAddressSql,
                new { p_address_id = command.ShippingAddressId, p_user_id = command.UserId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (shippingAddress is null)
        {
            throw new ArgumentException("Shipping address does not exist for the current user.", nameof(command.ShippingAddressId));
        }

        var now = DateTime.UtcNow;
        var calculatedUnitPrice = RoundMoney(sku.BasePrice + (sku.PageUnitPrice * pageCount));
        var totalAmount = RoundMoney(calculatedUnitPrice * command.Quantity);
        if (command.DiscountAmount > totalAmount)
        {
            throw new ArgumentException("Discount amount cannot exceed the order total amount.", nameof(command.DiscountAmount));
        }

        var discountedSubtotal = RoundMoney(totalAmount - command.DiscountAmount);
        var payAmount = RoundMoney(discountedSubtotal + command.FreightAmount);
        var unitPrice = RoundMoney(discountedSubtotal / command.Quantity);
        var estimatedShipDate = now.Date.AddDays(7);
        var paidAt = payAmount == 0 ? now : (DateTime?)null;
        var expiredAt = payAmount == 0 ? null : (DateTime?)now.AddMinutes(30);
        var orderStatus = payAmount == 0 ? 1 : 0;
        var paymentStatus = payAmount == 0 ? 2 : 0;
        var orderNo = GenerateBusinessNo("ORD");
        var paymentNo = GenerateBusinessNo("PAY");

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertOrderSql,
                new
                {
                    p_order_no = orderNo,
                    p_user_id = command.UserId,
                    p_status = orderStatus,
                    p_total_amount = totalAmount,
                    p_discount_amount = command.DiscountAmount,
                    p_freight_amount = command.FreightAmount,
                    p_pay_amount = payAmount,
                    p_ship_receiver = shippingAddress.ReceiverName,
                    p_ship_phone = shippingAddress.Phone,
                    p_ship_province = shippingAddress.Province,
                    p_ship_city = shippingAddress.City,
                    p_ship_district = NormalizeNullable(shippingAddress.District),
                    p_ship_address = shippingAddress.AddressDetail,
                    p_ship_postal = NormalizeNullable(shippingAddress.PostalCode),
                    p_remark = NormalizeNullable(command.Remark),
                    p_estimated_ship_date = estimatedShipDate,
                    p_paid_at = paidAt
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var orderId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                GetLastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertOrderItemSql,
                new
                {
                    p_order_id = orderId,
                    p_project_id = command.ProjectId,
                    p_project_version_id = project.ProjectVersionId.Value,
                    p_sku_id = command.SkuId,
                    p_quantity = command.Quantity,
                    p_unit_price = unitPrice,
                    p_subtotal = discountedSubtotal
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var orderItemId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                GetLastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertPriceSnapshotSql,
                new
                {
                    p_order_item_id = orderItemId,
                    p_spu_code = sku.SpuCode,
                    p_spu_name = sku.SpuName,
                    p_sku_code = sku.SkuCode,
                    p_size_label = sku.SizeLabel,
                    p_binding_label = sku.BindingLabel,
                    p_layout_label = sku.LayoutLabel,
                    p_cover_description = BuildCoverDescription(project),
                    p_page_count = pageCount,
                    p_image_count = imageCount,
                    p_base_price = sku.BasePrice,
                    p_page_unit_price = sku.PageUnitPrice,
                    p_calculated_price = calculatedUnitPrice,
                    p_discount = command.DiscountAmount,
                    p_final_price = unitPrice,
                    p_estimated_ship_date = estimatedShipDate
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var priceSnapshotId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                GetLastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateOrderItemSnapshotSql,
                new { p_price_snapshot_id = priceSnapshotId, p_order_item_id = orderItemId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertPaymentSql,
                new
                {
                    p_payment_no = paymentNo,
                    p_order_id = orderId,
                    p_user_id = command.UserId,
                    p_pay_channel = command.PayChannel,
                    p_pay_amount = payAmount,
                    p_status = paymentStatus,
                    p_paid_at = paidAt,
                    p_expired_at = expiredAt
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var paymentId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                GetLastInsertIdSql,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectStatusSql,
                new { p_project_id = command.ProjectId },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new OrderCreateResultModel(
            orderId,
            orderNo,
            orderStatus,
            totalAmount,
            command.DiscountAmount,
            command.FreightAmount,
            payAmount,
            paymentId,
            paymentNo,
            expiredAt);
    }

    public async Task<OrderListQueryResultModel> GetOrdersAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken)
    {
        return await GetOrdersCoreAsync(
            filter.UserId,
            null,
            filter.Status,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken);
    }

    public Task<OrderDetailQueryModel?> GetOrderDetailAsync(
        long userId,
        string orderNo,
        CancellationToken cancellationToken)
    {
        return GetOrderDetailCoreAsync(userId, orderNo, cancellationToken);
    }

    private async Task<OrderListQueryResultModel> GetOrdersCoreAsync(
        long? userId,
        string? keyword,
        int? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountOrdersSql,
                new { p_user_id = userId, p_keyword = NormalizeNullable(keyword), p_status = status },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<OrderListRow>(
            new CommandDefinition(
                ListOrdersSql,
                new
                {
                    p_user_id = userId,
                    p_keyword = NormalizeNullable(keyword),
                    p_status = status,
                    p_page_size = pageSize,
                    p_offset = (pageNumber - 1) * pageSize
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var items = rows
            .Select(row => new OrderListItemQueryModel(
                row.OrderId,
                row.OrderNo,
                row.Status,
                row.ItemCount,
                row.TotalAmount,
                row.PayAmount,
                row.ProjectTitle,
                row.ProductName,
                row.SkuCode,
                row.Quantity,
                row.CreatedAt,
                row.PaidAt,
                row.ShipmentNo,
                row.ShipmentStatus))
            .ToArray();

        return new OrderListQueryResultModel(items, totalCount);
    }

    private async Task<OrderDetailQueryModel?> GetOrderDetailCoreAsync(
        long? userId,
        string orderNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<OrderHeaderRow>(
            new CommandDefinition(
                FindOrderHeaderSql,
                new { p_user_id = userId, p_order_no = orderNo },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var itemRows = await connection.QueryAsync<OrderItemRow>(
            new CommandDefinition(
                FindOrderItemsSql,
                new { p_order_id = header.OrderId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        IReadOnlyCollection<OrderShipmentEventModel> shipmentEvents = Array.Empty<OrderShipmentEventModel>();
        if (header.ShipmentId.HasValue)
        {
            var eventRows = await connection.QueryAsync<ShipmentEventRow>(
                new CommandDefinition(
                    FindShipmentEventsSql,
                    new { p_shipment_id = header.ShipmentId.Value },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            shipmentEvents = eventRows
                .Select(item => new OrderShipmentEventModel(item.EventTime, item.EventDescription, item.Location))
                .ToArray();
        }

        var items = itemRows
            .Select(row => new OrderItemDetailModel(
                row.ItemNo,
                row.ProjectId,
                row.ProjectVersionId,
                row.SkuId,
                row.Quantity,
                row.UnitPrice,
                row.Subtotal,
                row.Status,
                row.ProjectTitle,
                string.IsNullOrWhiteSpace(row.SpuCode) || string.IsNullOrWhiteSpace(row.SpuName) || string.IsNullOrWhiteSpace(row.SnapshotSkuCode)
                    ? null
                    : new OrderPriceSnapshotModel(
                        row.SpuCode,
                        row.SpuName,
                        row.SnapshotSkuCode,
                        row.SizeLabel,
                        row.BindingLabel,
                        row.LayoutLabel,
                        row.CoverDescription,
                        row.PageCount ?? 0,
                        row.ImageCount ?? 0,
                        row.BasePrice ?? 0,
                        row.PageUnitPrice ?? 0,
                        row.CalculatedPrice ?? 0,
                        row.AgentMarkup ?? 0,
                        row.Discount ?? 0,
                        row.FinalPrice ?? 0,
                        row.IsBundleItem,
                        row.BundleCode,
                        row.BundleName,
                        row.EstimatedShipDate)))
            .ToArray();

        var payment = header.PaymentId.HasValue && !string.IsNullOrWhiteSpace(header.PaymentNo) && !string.IsNullOrWhiteSpace(header.PayChannel)
            ? new OrderPaymentDetailModel(
                header.PaymentId.Value,
                header.PaymentNo,
                header.PayChannel,
                header.PaymentAmount ?? 0,
                header.PaymentStatus ?? 0,
                header.PaymentPaidAt,
                header.PaymentExpiredAt)
            : null;

        var shipment = header.ShipmentId.HasValue && !string.IsNullOrWhiteSpace(header.ShipmentNo) && !string.IsNullOrWhiteSpace(header.CarrierCode)
            ? new OrderShipmentDetailModel(
                header.ShipmentId.Value,
                header.ShipmentNo,
                header.CarrierCode,
                header.CarrierName,
                header.ShipmentStatus ?? 0,
                header.ShippedAt,
                header.DeliveredAt,
                shipmentEvents)
            : null;

        return new OrderDetailQueryModel(
            header.OrderId,
            header.OrderNo,
            header.UserId,
            header.OrderType,
            header.Status,
            header.ItemCount,
            header.TotalAmount,
            header.DiscountAmount,
            header.FreightAmount,
            header.PayAmount,
            new OrderShippingSnapshotModel(
                header.ReceiverName,
                header.Phone,
                header.Province,
                header.City,
                header.District,
                header.Address,
                header.PostalCode),
            header.Remark,
            header.CreatedAt,
            header.PaidAt,
            header.ClosedAt,
            items,
            payment,
            shipment);
    }

    private async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static ShippingAddressQueryModel MapShippingAddress(ShippingAddressRow row)
    {
        return new ShippingAddressQueryModel(
            row.AddressId,
            row.ReceiverName,
            row.Phone,
            row.Province,
            row.City,
            row.District,
            row.AddressDetail,
            row.PostalCode,
            row.IsDefault);
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string BuildCoverDescription(ProjectForOrderRow project)
    {
        var coverText = NormalizeNullable(project.CoverText);
        if (!string.IsNullOrWhiteSpace(coverText))
        {
            return coverText;
        }

        var coverSource = NormalizeNullable(project.CoverSource);
        if (!string.IsNullOrWhiteSpace(coverSource))
        {
            return coverSource;
        }

        return project.ProjectTitle;
    }

    private static decimal RoundMoney(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static string GenerateBusinessNo(string prefix)
    {
        return $"{prefix}{DateTime.UtcNow:yyyyMMddHHmmssfff}{RandomNumberGenerator.GetInt32(1000, 10000)}";
    }

    private sealed class ShippingAddressRow
    {
        public long AddressId { get; init; }

        public long UserId { get; init; }

        public string ReceiverName { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string Province { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string? District { get; init; }

        public string AddressDetail { get; init; } = string.Empty;

        public string? PostalCode { get; init; }

        public bool IsDefault { get; init; }
    }

    private sealed class ProjectForOrderRow
    {
        public long ProjectId { get; init; }

        public long UserId { get; init; }

        public string ProjectTitle { get; init; } = string.Empty;

        public string? ProjectSubtitle { get; init; }

        public int ProjectStatus { get; init; }

        public int ProjectPageCount { get; init; }

        public int ProjectImageCount { get; init; }

        public string BookType { get; init; } = string.Empty;

        public long? SpuId { get; init; }

        public string? ProductCode { get; init; }

        public long? ProjectVersionId { get; init; }

        public int VersionPageCount { get; init; }

        public int VersionImageCount { get; init; }

        public string? CoverText { get; init; }

        public string? CoverSource { get; init; }

        public string? SizeCode { get; init; }

        public string? BindingCode { get; init; }

        public string? LayoutCode { get; init; }
    }

    private sealed class SkuForOrderRow
    {
        public long SkuId { get; init; }

        public long SpuId { get; init; }

        public string SkuCode { get; init; } = string.Empty;

        public int MinPages { get; init; }

        public int? MaxPages { get; init; }

        public string SpuCode { get; init; } = string.Empty;

        public string SpuName { get; init; } = string.Empty;

        public string? SizeLabel { get; init; }

        public string? BindingLabel { get; init; }

        public string? LayoutLabel { get; init; }

        public long? PriceRuleId { get; init; }

        public decimal BasePrice { get; init; }

        public decimal PageUnitPrice { get; init; }
    }

    private sealed class OrderListRow
    {
        public long OrderId { get; init; }

        public string OrderNo { get; init; } = string.Empty;

        public int Status { get; init; }

        public int ItemCount { get; init; }

        public decimal TotalAmount { get; init; }

        public decimal PayAmount { get; init; }

        public string? ProjectTitle { get; init; }

        public string ProductName { get; init; } = string.Empty;

        public string SkuCode { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? PaidAt { get; init; }

        public string? ShipmentNo { get; init; }

        public int? ShipmentStatus { get; init; }
    }

    private sealed class OrderHeaderRow
    {
        public long OrderId { get; init; }

        public string OrderNo { get; init; } = string.Empty;

        public long UserId { get; init; }

        public string OrderType { get; init; } = string.Empty;

        public int Status { get; init; }

        public int ItemCount { get; init; }

        public decimal TotalAmount { get; init; }

        public decimal DiscountAmount { get; init; }

        public decimal FreightAmount { get; init; }

        public decimal PayAmount { get; init; }

        public string ReceiverName { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string Province { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string? District { get; init; }

        public string Address { get; init; } = string.Empty;

        public string? PostalCode { get; init; }

        public string? Remark { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? PaidAt { get; init; }

        public DateTime? ClosedAt { get; init; }

        public long? PaymentId { get; init; }

        public string? PaymentNo { get; init; }

        public string? PayChannel { get; init; }

        public decimal? PaymentAmount { get; init; }

        public int? PaymentStatus { get; init; }

        public DateTime? PaymentPaidAt { get; init; }

        public DateTime? PaymentExpiredAt { get; init; }

        public long? ShipmentId { get; init; }

        public string? ShipmentNo { get; init; }

        public string? CarrierCode { get; init; }

        public string? CarrierName { get; init; }

        public int? ShipmentStatus { get; init; }

        public DateTime? ShippedAt { get; init; }

        public DateTime? DeliveredAt { get; init; }
    }

    private sealed class OrderItemRow
    {
        public int ItemNo { get; init; }

        public long? ProjectId { get; init; }

        public long? ProjectVersionId { get; init; }

        public long? SkuId { get; init; }

        public int Quantity { get; init; }

        public decimal UnitPrice { get; init; }

        public decimal Subtotal { get; init; }

        public int Status { get; init; }

        public string? ProjectTitle { get; init; }

        public string? SpuCode { get; init; }

        public string? SpuName { get; init; }

        public string? SnapshotSkuCode { get; init; }

        public string? SizeLabel { get; init; }

        public string? BindingLabel { get; init; }

        public string? LayoutLabel { get; init; }

        public string? CoverDescription { get; init; }

        public int? PageCount { get; init; }

        public int? ImageCount { get; init; }

        public decimal? BasePrice { get; init; }

        public decimal? PageUnitPrice { get; init; }

        public decimal? CalculatedPrice { get; init; }

        public decimal? AgentMarkup { get; init; }

        public decimal? Discount { get; init; }

        public decimal? FinalPrice { get; init; }

        public bool IsBundleItem { get; init; }

        public string? BundleCode { get; init; }

        public string? BundleName { get; init; }

        public DateTime? EstimatedShipDate { get; init; }
    }

    private sealed record ShipmentEventRow(
        DateTime EventTime,
        string EventDescription,
        string? Location);
}
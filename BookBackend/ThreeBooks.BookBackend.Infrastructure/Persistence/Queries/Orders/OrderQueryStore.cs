using System.Security.Cryptography;
using Dapper;
using MySqlConnector;
using ThreeBooks.BookBackend.Application.Modules.Orders.Interfaces;
using ThreeBooks.BookBackend.Application.Modules.Orders.Models;

namespace ThreeBooks.BookBackend.Infrastructure.Persistence.Queries.Orders;

public sealed class OrderQueryStore(string connectionString) : IOrderQueryStore
{
    private const string FindUserSql = """
        SELECT id
        FROM identity_user
        WHERE id = @UserId
        LIMIT 1;
        """;

    private const string ListShippingAddressesSql = """
        SELECT
            id AS AddressId,
            receiver_name AS ReceiverName,
            phone AS Phone,
            province AS Province,
            city AS City,
            district AS District,
            address_detail AS AddressDetail,
            postal_code AS PostalCode,
            is_default AS IsDefault
        FROM order_shipping_address
        WHERE user_id = @UserId
        ORDER BY is_default DESC, updated_at DESC, id DESC;
        """;

    private const string FindShippingAddressSql = """
        SELECT
            id AS AddressId,
            user_id AS UserId,
            receiver_name AS ReceiverName,
            phone AS Phone,
            province AS Province,
            city AS City,
            district AS District,
            address_detail AS AddressDetail,
            postal_code AS PostalCode,
            is_default AS IsDefault
        FROM order_shipping_address
        WHERE id = @AddressId
          AND user_id = @UserId
        LIMIT 1;
        """;

    private const string CountOtherAddressesSql = """
        SELECT COUNT(*)
        FROM order_shipping_address
        WHERE user_id = @UserId
          AND (@AddressId IS NULL OR id <> @AddressId);
        """;

    private const string CountOtherDefaultAddressesSql = """
        SELECT COUNT(*)
        FROM order_shipping_address
        WHERE user_id = @UserId
          AND is_default = 1
          AND (@AddressId IS NULL OR id <> @AddressId);
        """;

    private const string ClearDefaultShippingAddressesSql = """
        UPDATE order_shipping_address
        SET is_default = 0,
            updated_at = CURRENT_TIMESTAMP
        WHERE user_id = @UserId
          AND (@AddressId IS NULL OR id <> @AddressId);
        """;

    private const string InsertShippingAddressSql = """
        INSERT INTO order_shipping_address (
            user_id,
            receiver_name,
            phone,
            province,
            city,
            district,
            address_detail,
            postal_code,
            is_default)
        VALUES (
            @UserId,
            @ReceiverName,
            @Phone,
            @Province,
            @City,
            @District,
            @AddressDetail,
            @PostalCode,
            @IsDefault);
        """;

    private const string UpdateShippingAddressSql = """
        UPDATE order_shipping_address
        SET receiver_name = @ReceiverName,
            phone = @Phone,
            province = @Province,
            city = @City,
            district = @District,
            address_detail = @AddressDetail,
            postal_code = @PostalCode,
            is_default = @IsDefault,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @AddressId
          AND user_id = @UserId;
        """;

    private const string FindProjectForOrderSql = """
        SELECT
            p.id AS ProjectId,
            p.user_id AS UserId,
            p.title AS ProjectTitle,
            p.subtitle AS ProjectSubtitle,
            p.status AS ProjectStatus,
            p.page_count AS ProjectPageCount,
            p.image_count AS ProjectImageCount,
            p.book_type AS BookType,
            p.spu_id AS SpuId,
            COALESCE(spu.spu_code, p.book_type) AS ProductCode,
            latest_version.id AS ProjectVersionId,
            COALESCE(latest_version.page_count, p.page_count) AS VersionPageCount,
            COALESCE(latest_version.image_count, p.image_count) AS VersionImageCount,
            cover.cover_text AS CoverText,
            cover.cover_source AS CoverSource,
            layout.size_code AS SizeCode,
            layout.binding_code AS BindingCode,
            layout.layout_code AS LayoutCode
        FROM book_project p
        LEFT JOIN catalog_product_spu spu ON spu.id = p.spu_id
        LEFT JOIN book_project_cover cover ON cover.project_id = p.id
        LEFT JOIN book_project_layout layout ON layout.project_id = p.id
        LEFT JOIN book_project_version latest_version ON latest_version.id = (
            SELECT version.id
            FROM book_project_version version
            WHERE version.project_id = p.id
            ORDER BY version.version_no DESC, version.id DESC
            LIMIT 1
        )
        WHERE p.id = @ProjectId
          AND p.user_id = @UserId
        LIMIT 1;
        """;

    private const string FindSkuForOrderSql = """
        SELECT
            sku.id AS SkuId,
            sku.spu_id AS SpuId,
            sku.sku_code AS SkuCode,
            sku.min_pages AS MinPages,
            sku.max_pages AS MaxPages,
            spu.spu_code AS SpuCode,
            spu.name AS SpuName,
            size_value.value_label AS SizeLabel,
            binding_value.value_label AS BindingLabel,
            layout_value.value_label AS LayoutLabel,
            price.id AS PriceRuleId,
            COALESCE(price.base_price, 0) AS BasePrice,
            COALESCE(price.page_unit_price, 0) AS PageUnitPrice
        FROM catalog_product_sku sku
        INNER JOIN catalog_product_spu spu ON spu.id = sku.spu_id AND spu.is_active = 1
        LEFT JOIN catalog_spec_value size_value ON size_value.id = sku.size_value_id
        LEFT JOIN catalog_spec_value binding_value ON binding_value.id = sku.binding_value_id
        LEFT JOIN catalog_spec_value layout_value ON layout_value.id = sku.layout_value_id
        LEFT JOIN catalog_price_rule price ON price.id = (
            SELECT pr.id
            FROM catalog_price_rule pr
            WHERE pr.sku_id = sku.id
              AND pr.is_active = 1
              AND pr.effective_from <= UTC_TIMESTAMP()
              AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
            ORDER BY pr.effective_from DESC, pr.id DESC
            LIMIT 1
        )
        WHERE sku.id = @SkuId
          AND sku.is_active = 1
        LIMIT 1;
        """;

    private const string InsertOrderSql = """
        INSERT INTO order_order (
            order_no,
            user_id,
            order_type,
            status,
            item_count,
            total_amount,
            discount_amount,
            freight_amount,
            pay_amount,
            ship_receiver,
            ship_phone,
            ship_province,
            ship_city,
            ship_district,
            ship_address,
            ship_postal,
            agent_account_id,
            agent_share_link_id,
            bundle_id,
            remark,
            estimated_ship_date,
            paid_at,
            closed_at)
        VALUES (
            @OrderNo,
            @UserId,
            'normal',
            @Status,
            1,
            @TotalAmount,
            @DiscountAmount,
            @FreightAmount,
            @PayAmount,
            @ShipReceiver,
            @ShipPhone,
            @ShipProvince,
            @ShipCity,
            @ShipDistrict,
            @ShipAddress,
            @ShipPostal,
            NULL,
            NULL,
            NULL,
            @Remark,
            @EstimatedShipDate,
            @PaidAt,
            NULL);
        """;

    private const string InsertOrderItemSql = """
        INSERT INTO order_order_item (
            order_id,
            item_no,
            project_id,
            project_version_id,
            sku_id,
            price_snapshot_id,
            quantity,
            unit_price,
            subtotal,
            status)
        VALUES (
            @OrderId,
            1,
            @ProjectId,
            @ProjectVersionId,
            @SkuId,
            NULL,
            @Quantity,
            @UnitPrice,
            @Subtotal,
            0);
        """;

    private const string InsertPriceSnapshotSql = """
        INSERT INTO order_price_snapshot (
            order_item_id,
            spu_code,
            spu_name,
            sku_code,
            size_label,
            binding_label,
            layout_label,
            cover_desc,
            page_count,
            image_count,
            base_price,
            page_unit_price,
            calculated_price,
            agent_markup,
            discount,
            final_price,
            is_bundle_item,
            bundle_code,
            bundle_name,
            estimated_ship_date,
            created_at)
        VALUES (
            @OrderItemId,
            @SpuCode,
            @SpuName,
            @SkuCode,
            @SizeLabel,
            @BindingLabel,
            @LayoutLabel,
            @CoverDescription,
            @PageCount,
            @ImageCount,
            @BasePrice,
            @PageUnitPrice,
            @CalculatedPrice,
            0,
            @Discount,
            @FinalPrice,
            0,
            NULL,
            NULL,
            @EstimatedShipDate,
            UTC_TIMESTAMP());
        """;

    private const string UpdateOrderItemSnapshotSql = """
        UPDATE order_order_item
        SET price_snapshot_id = @PriceSnapshotId,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @OrderItemId;
        """;

    private const string InsertPaymentSql = """
        INSERT INTO order_payment (
            payment_no,
            order_id,
            user_id,
            pay_channel,
            pay_amount,
            status,
            paid_at,
            expired_at)
        VALUES (
            @PaymentNo,
            @OrderId,
            @UserId,
            @PayChannel,
            @PayAmount,
            @Status,
            @PaidAt,
            @ExpiredAt);
        """;

    private const string UpdateProjectStatusSql = """
        UPDATE book_project
        SET status = 3,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @ProjectId
          AND status < 3;
        """;

    private const string CountOrdersSql = """
        SELECT COUNT(*)
        FROM order_order
        WHERE user_id = @UserId
          AND (@Status IS NULL OR status = @Status);
        """;

    private const string ListOrdersSql = """
        SELECT
            o.id AS OrderId,
            o.order_no AS OrderNo,
            o.status AS Status,
            o.item_count AS ItemCount,
            o.total_amount AS TotalAmount,
            o.pay_amount AS PayAmount,
            COALESCE(project.title, snapshot.spu_name) AS ProjectTitle,
            COALESCE(snapshot.spu_name, '') AS ProductName,
            COALESCE(snapshot.sku_code, '') AS SkuCode,
            COALESCE(first_item.quantity, 0) AS Quantity,
            o.created_at AS CreatedAt,
            o.paid_at AS PaidAt,
            latest_shipment.shipment_no AS ShipmentNo,
            latest_shipment.status AS ShipmentStatus
        FROM order_order o
        LEFT JOIN order_order_item first_item ON first_item.id = (
            SELECT item.id
            FROM order_order_item item
            WHERE item.order_id = o.id
            ORDER BY item.item_no, item.id
            LIMIT 1
        )
        LEFT JOIN book_project project ON project.id = first_item.project_id
        LEFT JOIN order_price_snapshot snapshot ON snapshot.order_item_id = first_item.id
        LEFT JOIN order_shipment latest_shipment ON latest_shipment.id = (
            SELECT shipment.id
            FROM order_shipment shipment
            WHERE shipment.order_id = o.id
            ORDER BY shipment.id DESC
            LIMIT 1
        )
        WHERE o.user_id = @UserId
          AND (@Status IS NULL OR o.status = @Status)
        ORDER BY o.created_at DESC, o.id DESC
        LIMIT @PageSize OFFSET @Offset;
        """;

    private const string FindOrderHeaderSql = """
        SELECT
            o.id AS OrderId,
            o.order_no AS OrderNo,
            o.user_id AS UserId,
            o.order_type AS OrderType,
            o.status AS Status,
            o.item_count AS ItemCount,
            o.total_amount AS TotalAmount,
            o.discount_amount AS DiscountAmount,
            o.freight_amount AS FreightAmount,
            o.pay_amount AS PayAmount,
            o.ship_receiver AS ReceiverName,
            o.ship_phone AS Phone,
            o.ship_province AS Province,
            o.ship_city AS City,
            o.ship_district AS District,
            o.ship_address AS Address,
            o.ship_postal AS PostalCode,
            o.remark AS Remark,
            o.created_at AS CreatedAt,
            o.paid_at AS PaidAt,
            o.closed_at AS ClosedAt,
            latest_payment.id AS PaymentId,
            latest_payment.payment_no AS PaymentNo,
            latest_payment.pay_channel AS PayChannel,
            latest_payment.pay_amount AS PaymentAmount,
            latest_payment.status AS PaymentStatus,
            latest_payment.paid_at AS PaymentPaidAt,
            latest_payment.expired_at AS PaymentExpiredAt,
            latest_shipment.id AS ShipmentId,
            latest_shipment.shipment_no AS ShipmentNo,
            latest_shipment.carrier_code AS CarrierCode,
            latest_shipment.carrier_name AS CarrierName,
            latest_shipment.status AS ShipmentStatus,
            latest_shipment.shipped_at AS ShippedAt,
            latest_shipment.delivered_at AS DeliveredAt
        FROM order_order o
        LEFT JOIN order_payment latest_payment ON latest_payment.id = (
            SELECT payment.id
            FROM order_payment payment
            WHERE payment.order_id = o.id
            ORDER BY payment.id DESC
            LIMIT 1
        )
        LEFT JOIN order_shipment latest_shipment ON latest_shipment.id = (
            SELECT shipment.id
            FROM order_shipment shipment
            WHERE shipment.order_id = o.id
            ORDER BY shipment.id DESC
            LIMIT 1
        )
        WHERE o.user_id = @UserId
          AND o.order_no = @OrderNo
        LIMIT 1;
        """;

    private const string FindOrderItemsSql = """
        SELECT
            item.item_no AS ItemNo,
            item.project_id AS ProjectId,
            item.project_version_id AS ProjectVersionId,
            item.sku_id AS SkuId,
            item.quantity AS Quantity,
            item.unit_price AS UnitPrice,
            item.subtotal AS Subtotal,
            item.status AS Status,
            project.title AS ProjectTitle,
            snapshot.spu_code AS SpuCode,
            snapshot.spu_name AS SpuName,
            snapshot.sku_code AS SnapshotSkuCode,
            snapshot.size_label AS SizeLabel,
            snapshot.binding_label AS BindingLabel,
            snapshot.layout_label AS LayoutLabel,
            snapshot.cover_desc AS CoverDescription,
            snapshot.page_count AS PageCount,
            snapshot.image_count AS ImageCount,
            snapshot.base_price AS BasePrice,
            snapshot.page_unit_price AS PageUnitPrice,
            snapshot.calculated_price AS CalculatedPrice,
            snapshot.agent_markup AS AgentMarkup,
            snapshot.discount AS Discount,
            snapshot.final_price AS FinalPrice,
            snapshot.is_bundle_item AS IsBundleItem,
            snapshot.bundle_code AS BundleCode,
            snapshot.bundle_name AS BundleName,
            snapshot.estimated_ship_date AS EstimatedShipDate
        FROM order_order_item item
        LEFT JOIN book_project project ON project.id = item.project_id
        LEFT JOIN order_price_snapshot snapshot ON snapshot.order_item_id = item.id
        WHERE item.order_id = @OrderId
        ORDER BY item.item_no, item.id;
        """;

    private const string FindShipmentEventsSql = """
        SELECT
            event_time AS EventTime,
            event_desc AS EventDescription,
            location AS Location
        FROM order_shipment_event
        WHERE shipment_id = @ShipmentId
        ORDER BY event_time DESC, id DESC;
        """;

    private const string GetLastInsertIdSql = """
        SELECT LAST_INSERT_ID();
        """;

    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Order database connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<IReadOnlyCollection<ShippingAddressQueryModel>> GetShippingAddressesAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ShippingAddressRow>(
            new CommandDefinition(ListShippingAddressesSql, new { UserId = userId }, cancellationToken: cancellationToken));

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
                new { command.UserId },
                transaction: transaction,
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
                    new { AddressId = command.AddressId.Value, command.UserId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (existing is null)
            {
                throw new ArgumentException("Shipping address does not exist.", nameof(command.AddressId));
            }
        }

        var otherAddressCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountOtherAddressesSql,
                new { command.UserId, command.AddressId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var otherDefaultCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountOtherDefaultAddressesSql,
                new { command.UserId, command.AddressId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var shouldSetDefault = command.IsDefault
            || otherAddressCount == 0
            || (existing is not null && existing.IsDefault && otherDefaultCount == 0);

        if (shouldSetDefault)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    ClearDefaultShippingAddressesSql,
                    new { command.UserId, command.AddressId },
                    transaction: transaction,
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
                        command.UserId,
                        command.ReceiverName,
                        command.Phone,
                        command.Province,
                        command.City,
                        District = NormalizeNullable(command.District),
                        command.AddressDetail,
                        PostalCode = NormalizeNullable(command.PostalCode),
                        IsDefault = shouldSetDefault
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            addressId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));
        }
        else
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    UpdateShippingAddressSql,
                    new
                    {
                        AddressId = existing.AddressId,
                        command.UserId,
                        command.ReceiverName,
                        command.Phone,
                        command.Province,
                        command.City,
                        District = NormalizeNullable(command.District),
                        command.AddressDetail,
                        PostalCode = NormalizeNullable(command.PostalCode),
                        IsDefault = shouldSetDefault
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            addressId = existing.AddressId;
        }

        var saved = await connection.QuerySingleAsync<ShippingAddressRow>(
            new CommandDefinition(
                FindShippingAddressSql,
                new { AddressId = addressId, command.UserId },
                transaction: transaction,
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
                new { command.ProjectId, command.UserId },
                transaction: transaction,
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
                new { command.SkuId },
                transaction: transaction,
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
                new { AddressId = command.ShippingAddressId, command.UserId },
                transaction: transaction,
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
                    OrderNo = orderNo,
                    command.UserId,
                    Status = orderStatus,
                    TotalAmount = totalAmount,
                    DiscountAmount = command.DiscountAmount,
                    FreightAmount = command.FreightAmount,
                    PayAmount = payAmount,
                    ShipReceiver = shippingAddress.ReceiverName,
                    ShipPhone = shippingAddress.Phone,
                    ShipProvince = shippingAddress.Province,
                    ShipCity = shippingAddress.City,
                    ShipDistrict = NormalizeNullable(shippingAddress.District),
                    ShipAddress = shippingAddress.AddressDetail,
                    ShipPostal = NormalizeNullable(shippingAddress.PostalCode),
                    Remark = NormalizeNullable(command.Remark),
                    EstimatedShipDate = estimatedShipDate,
                    PaidAt = paidAt
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var orderId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertOrderItemSql,
                new
                {
                    OrderId = orderId,
                    command.ProjectId,
                    ProjectVersionId = project.ProjectVersionId.Value,
                    command.SkuId,
                    command.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = discountedSubtotal
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var orderItemId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertPriceSnapshotSql,
                new
                {
                    OrderItemId = orderItemId,
                    sku.SpuCode,
                    sku.SpuName,
                    sku.SkuCode,
                    sku.SizeLabel,
                    sku.BindingLabel,
                    sku.LayoutLabel,
                    CoverDescription = BuildCoverDescription(project),
                    PageCount = pageCount,
                    ImageCount = imageCount,
                    BasePrice = sku.BasePrice,
                    PageUnitPrice = sku.PageUnitPrice,
                    CalculatedPrice = calculatedUnitPrice,
                    Discount = command.DiscountAmount,
                    FinalPrice = unitPrice,
                    EstimatedShipDate = estimatedShipDate
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var priceSnapshotId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateOrderItemSnapshotSql,
                new { PriceSnapshotId = priceSnapshotId, OrderItemId = orderItemId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertPaymentSql,
                new
                {
                    PaymentNo = paymentNo,
                    OrderId = orderId,
                    command.UserId,
                    PayChannel = command.PayChannel,
                    PayAmount = payAmount,
                    Status = paymentStatus,
                    PaidAt = paidAt,
                    ExpiredAt = expiredAt
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        var paymentId = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(GetLastInsertIdSql, transaction: transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                UpdateProjectStatusSql,
                new { command.ProjectId },
                transaction: transaction,
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
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountOrdersSql,
                new { filter.UserId, filter.Status },
                cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<OrderListRow>(
            new CommandDefinition(
                ListOrdersSql,
                new
                {
                    filter.UserId,
                    filter.Status,
                    filter.PageSize,
                    Offset = (filter.PageNumber - 1) * filter.PageSize
                },
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

    public async Task<OrderDetailQueryModel?> GetOrderDetailAsync(
        long userId,
        string orderNo,
        CancellationToken cancellationToken)
    {
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<OrderHeaderRow>(
            new CommandDefinition(
                FindOrderHeaderSql,
                new { UserId = userId, OrderNo = orderNo },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var itemRows = await connection.QueryAsync<OrderItemRow>(
            new CommandDefinition(
                FindOrderItemsSql,
                new { OrderId = header.OrderId },
                cancellationToken: cancellationToken));

        IReadOnlyCollection<OrderShipmentEventModel> shipmentEvents = Array.Empty<OrderShipmentEventModel>();
        if (header.ShipmentId.HasValue)
        {
            var eventRows = await connection.QueryAsync<ShipmentEventRow>(
                new CommandDefinition(
                    FindShipmentEventsSql,
                    new { ShipmentId = header.ShipmentId.Value },
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
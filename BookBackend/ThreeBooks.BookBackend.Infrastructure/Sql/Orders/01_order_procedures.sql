DROP PROCEDURE IF EXISTS usp_Order_FindUser;
DROP PROCEDURE IF EXISTS usp_Order_ListShippingAddresses;
DROP PROCEDURE IF EXISTS usp_Order_FindShippingAddress;
DROP PROCEDURE IF EXISTS usp_Order_CountOtherAddresses;
DROP PROCEDURE IF EXISTS usp_Order_CountOtherDefaultAddresses;
DROP PROCEDURE IF EXISTS usp_Order_ClearDefaultShippingAddresses;
DROP PROCEDURE IF EXISTS usp_Order_InsertShippingAddress;
DROP PROCEDURE IF EXISTS usp_Order_UpdateShippingAddress;
DROP PROCEDURE IF EXISTS usp_Order_FindProjectForOrder;
DROP PROCEDURE IF EXISTS usp_Order_FindSkuForOrder;
DROP PROCEDURE IF EXISTS usp_Order_InsertOrder;
DROP PROCEDURE IF EXISTS usp_Order_InsertOrderItem;
DROP PROCEDURE IF EXISTS usp_Order_InsertPriceSnapshot;
DROP PROCEDURE IF EXISTS usp_Order_UpdateOrderItemSnapshot;
DROP PROCEDURE IF EXISTS usp_Order_InsertPayment;
DROP PROCEDURE IF EXISTS usp_Order_UpdateProjectStatus;
DROP PROCEDURE IF EXISTS usp_Order_CountOrders;
DROP PROCEDURE IF EXISTS usp_Order_ListOrders;
DROP PROCEDURE IF EXISTS usp_Order_FindOrderHeader;
DROP PROCEDURE IF EXISTS usp_Order_FindOrderItems;
DROP PROCEDURE IF EXISTS usp_Order_FindShipmentEvents;
DROP PROCEDURE IF EXISTS usp_Order_UpdateAdminOrder;
DROP PROCEDURE IF EXISTS usp_Order_InsertShipment;
DROP PROCEDURE IF EXISTS usp_Order_UpdateShipment;
DROP PROCEDURE IF EXISTS usp_Order_InsertShipmentEvent;

DELIMITER $$

CREATE PROCEDURE usp_Order_FindUser(
    IN p_user_id BIGINT
)
BEGIN
    SELECT id
    FROM identity_user
    WHERE id = p_user_id
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Order_ListShippingAddresses(
    IN p_user_id BIGINT
)
BEGIN
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
    WHERE user_id = p_user_id
    ORDER BY is_default DESC, updated_at DESC, id DESC;
END $$

CREATE PROCEDURE usp_Order_FindShippingAddress(
    IN p_address_id BIGINT,
    IN p_user_id BIGINT
)
BEGIN
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
    WHERE id = p_address_id
      AND user_id = p_user_id
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Order_CountOtherAddresses(
    IN p_user_id BIGINT,
    IN p_address_id BIGINT
)
BEGIN
    SELECT COUNT(*)
    FROM order_shipping_address
    WHERE user_id = p_user_id
      AND (p_address_id IS NULL OR id <> p_address_id);
END $$

CREATE PROCEDURE usp_Order_CountOtherDefaultAddresses(
    IN p_user_id BIGINT,
    IN p_address_id BIGINT
)
BEGIN
    SELECT COUNT(*)
    FROM order_shipping_address
    WHERE user_id = p_user_id
      AND is_default = 1
      AND (p_address_id IS NULL OR id <> p_address_id);
END $$

CREATE PROCEDURE usp_Order_ClearDefaultShippingAddresses(
    IN p_user_id BIGINT,
    IN p_address_id BIGINT
)
BEGIN
    UPDATE order_shipping_address
    SET is_default = 0,
        updated_at = CURRENT_TIMESTAMP
    WHERE user_id = p_user_id
      AND (p_address_id IS NULL OR id <> p_address_id);
END $$

CREATE PROCEDURE usp_Order_InsertShippingAddress(
    IN p_user_id BIGINT,
    IN p_receiver_name VARCHAR(128),
    IN p_phone VARCHAR(32),
    IN p_province VARCHAR(64),
    IN p_city VARCHAR(64),
    IN p_district VARCHAR(64),
    IN p_address_detail VARCHAR(256),
    IN p_postal_code VARCHAR(32),
    IN p_is_default TINYINT
)
BEGIN
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
        p_user_id,
        p_receiver_name,
        p_phone,
        p_province,
        p_city,
        p_district,
        p_address_detail,
        p_postal_code,
        p_is_default);
END $$

CREATE PROCEDURE usp_Order_UpdateShippingAddress(
    IN p_address_id BIGINT,
    IN p_user_id BIGINT,
    IN p_receiver_name VARCHAR(128),
    IN p_phone VARCHAR(32),
    IN p_province VARCHAR(64),
    IN p_city VARCHAR(64),
    IN p_district VARCHAR(64),
    IN p_address_detail VARCHAR(256),
    IN p_postal_code VARCHAR(32),
    IN p_is_default TINYINT
)
BEGIN
    UPDATE order_shipping_address
    SET receiver_name = p_receiver_name,
        phone = p_phone,
        province = p_province,
        city = p_city,
        district = p_district,
        address_detail = p_address_detail,
        postal_code = p_postal_code,
        is_default = p_is_default,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_address_id
      AND user_id = p_user_id;
END $$

CREATE PROCEDURE usp_Order_FindProjectForOrder(
    IN p_project_id BIGINT,
    IN p_user_id BIGINT
)
BEGIN
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
    WHERE p.id = p_project_id
      AND p.user_id = p_user_id
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Order_FindSkuForOrder(
    IN p_sku_id BIGINT
)
BEGIN
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
    WHERE sku.id = p_sku_id
      AND sku.is_active = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Order_InsertOrder(
    IN p_order_no VARCHAR(64),
    IN p_user_id BIGINT,
    IN p_status INT,
    IN p_total_amount DECIMAL(18, 2),
    IN p_discount_amount DECIMAL(18, 2),
    IN p_freight_amount DECIMAL(18, 2),
    IN p_pay_amount DECIMAL(18, 2),
    IN p_ship_receiver VARCHAR(128),
    IN p_ship_phone VARCHAR(32),
    IN p_ship_province VARCHAR(64),
    IN p_ship_city VARCHAR(64),
    IN p_ship_district VARCHAR(64),
    IN p_ship_address VARCHAR(256),
    IN p_ship_postal VARCHAR(32),
    IN p_remark VARCHAR(512),
    IN p_estimated_ship_date DATETIME,
    IN p_paid_at DATETIME
)
BEGIN
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
        p_order_no,
        p_user_id,
        'normal',
        p_status,
        1,
        p_total_amount,
        p_discount_amount,
        p_freight_amount,
        p_pay_amount,
        p_ship_receiver,
        p_ship_phone,
        p_ship_province,
        p_ship_city,
        p_ship_district,
        p_ship_address,
        p_ship_postal,
        NULL,
        NULL,
        NULL,
        p_remark,
        p_estimated_ship_date,
        p_paid_at,
        NULL);
END $$

CREATE PROCEDURE usp_Order_InsertOrderItem(
    IN p_order_id BIGINT,
    IN p_project_id BIGINT,
    IN p_project_version_id BIGINT,
    IN p_sku_id BIGINT,
    IN p_quantity INT,
    IN p_unit_price DECIMAL(18, 2),
    IN p_subtotal DECIMAL(18, 2)
)
BEGIN
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
        p_order_id,
        1,
        p_project_id,
        p_project_version_id,
        p_sku_id,
        NULL,
        p_quantity,
        p_unit_price,
        p_subtotal,
        0);
END $$

CREATE PROCEDURE usp_Order_InsertPriceSnapshot(
    IN p_order_item_id BIGINT,
    IN p_spu_code VARCHAR(64),
    IN p_spu_name VARCHAR(256),
    IN p_sku_code VARCHAR(64),
    IN p_size_label VARCHAR(128),
    IN p_binding_label VARCHAR(128),
    IN p_layout_label VARCHAR(128),
    IN p_cover_description VARCHAR(512),
    IN p_page_count INT,
    IN p_image_count INT,
    IN p_base_price DECIMAL(18, 2),
    IN p_page_unit_price DECIMAL(18, 2),
    IN p_calculated_price DECIMAL(18, 2),
    IN p_discount DECIMAL(18, 2),
    IN p_final_price DECIMAL(18, 2),
    IN p_estimated_ship_date DATETIME
)
BEGIN
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
        p_order_item_id,
        p_spu_code,
        p_spu_name,
        p_sku_code,
        p_size_label,
        p_binding_label,
        p_layout_label,
        p_cover_description,
        p_page_count,
        p_image_count,
        p_base_price,
        p_page_unit_price,
        p_calculated_price,
        0,
        p_discount,
        p_final_price,
        0,
        NULL,
        NULL,
        p_estimated_ship_date,
        UTC_TIMESTAMP());
END $$

CREATE PROCEDURE usp_Order_UpdateOrderItemSnapshot(
    IN p_price_snapshot_id BIGINT,
    IN p_order_item_id BIGINT
)
BEGIN
    UPDATE order_order_item
    SET price_snapshot_id = p_price_snapshot_id,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_order_item_id;
END $$

CREATE PROCEDURE usp_Order_InsertPayment(
    IN p_payment_no VARCHAR(64),
    IN p_order_id BIGINT,
    IN p_user_id BIGINT,
    IN p_pay_channel VARCHAR(64),
    IN p_pay_amount DECIMAL(18, 2),
    IN p_status INT,
    IN p_paid_at DATETIME,
    IN p_expired_at DATETIME
)
BEGIN
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
        p_payment_no,
        p_order_id,
        p_user_id,
        p_pay_channel,
        p_pay_amount,
        p_status,
        p_paid_at,
        p_expired_at);
END $$

CREATE PROCEDURE usp_Order_UpdateProjectStatus(
    IN p_project_id BIGINT
)
BEGIN
    UPDATE book_project
    SET status = 3,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_project_id
      AND status < 3;
END $$

CREATE PROCEDURE usp_Order_CountOrders(
    IN p_user_id BIGINT,
    IN p_keyword VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_status INT
)
BEGIN
    SELECT COUNT(*)
    FROM order_order o
    LEFT JOIN order_shipment latest_shipment ON latest_shipment.id = (
        SELECT shipment.id
        FROM order_shipment shipment
        WHERE shipment.order_id = o.id
        ORDER BY shipment.id DESC
        LIMIT 1
    )
    WHERE (p_user_id IS NULL OR o.user_id = p_user_id)
      AND (p_keyword IS NULL
            OR o.order_no COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
            OR o.ship_receiver COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
            OR latest_shipment.shipment_no COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
      AND (p_status IS NULL OR o.status = p_status);
END $$

CREATE PROCEDURE usp_Order_ListOrders(
    IN p_user_id BIGINT,
    IN p_keyword VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_status INT,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
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
        WHERE (p_user_id IS NULL OR o.user_id = p_user_id)
            AND (p_keyword IS NULL
                        OR o.order_no COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                        OR o.ship_receiver COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                        OR latest_shipment.shipment_no COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
      AND (p_status IS NULL OR o.status = p_status)
    ORDER BY o.created_at DESC, o.id DESC
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_Order_FindOrderHeader(
    IN p_user_id BIGINT,
    IN p_order_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
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
        WHERE (p_user_id IS NULL OR o.user_id = p_user_id)
            AND o.order_no COLLATE utf8mb4_unicode_ci = p_order_no COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Order_FindOrderItems(
    IN p_order_id BIGINT
)
BEGIN
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
    WHERE item.order_id = p_order_id
    ORDER BY item.item_no, item.id;
END $$

CREATE PROCEDURE usp_Order_FindShipmentEvents(
    IN p_shipment_id BIGINT
)
BEGIN
    SELECT
        event_time AS EventTime,
        event_desc AS EventDescription,
        location AS Location
    FROM order_shipment_event
    WHERE shipment_id = p_shipment_id
    ORDER BY event_time DESC, id DESC;
END $$

CREATE PROCEDURE usp_Order_UpdateAdminOrder(
    IN p_order_id BIGINT,
    IN p_status INT,
    IN p_remark VARCHAR(512),
    IN p_paid_at DATETIME,
    IN p_closed_at DATETIME
)
BEGIN
    UPDATE order_order
    SET status = p_status,
        remark = p_remark,
        paid_at = p_paid_at,
        closed_at = p_closed_at,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_order_id;
END $$

CREATE PROCEDURE usp_Order_InsertShipment(
    IN p_order_id BIGINT,
    IN p_shipment_id BIGINT,
    IN p_shipment_no VARCHAR(64),
    IN p_carrier_code VARCHAR(64),
    IN p_carrier_name VARCHAR(128),
    IN p_status INT,
    IN p_shipped_at DATETIME,
    IN p_delivered_at DATETIME
)
BEGIN
    INSERT INTO order_shipment (
        order_id,
        shipment_no,
        carrier_code,
        carrier_name,
        status,
        shipped_at,
        delivered_at)
    VALUES (
        p_order_id,
        p_shipment_no,
        p_carrier_code,
        p_carrier_name,
        p_status,
        p_shipped_at,
        p_delivered_at);
END $$

CREATE PROCEDURE usp_Order_UpdateShipment(
    IN p_order_id BIGINT,
    IN p_shipment_id BIGINT,
    IN p_shipment_no VARCHAR(64),
    IN p_carrier_code VARCHAR(64),
    IN p_carrier_name VARCHAR(128),
    IN p_status INT,
    IN p_shipped_at DATETIME,
    IN p_delivered_at DATETIME
)
BEGIN
    UPDATE order_shipment
    SET shipment_no = p_shipment_no,
        carrier_code = p_carrier_code,
        carrier_name = p_carrier_name,
        status = p_status,
        shipped_at = p_shipped_at,
        delivered_at = p_delivered_at,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_shipment_id
      AND order_id = p_order_id;
END $$

CREATE PROCEDURE usp_Order_InsertShipmentEvent(
    IN p_shipment_id BIGINT,
    IN p_event_time DATETIME,
    IN p_event_desc VARCHAR(512),
    IN p_location VARCHAR(256)
)
BEGIN
    INSERT INTO order_shipment_event (
        shipment_id,
        event_time,
        event_desc,
        location,
        raw_data)
    VALUES (
        p_shipment_id,
        p_event_time,
        p_event_desc,
        p_location,
        NULL);
END $$

DELIMITER ;
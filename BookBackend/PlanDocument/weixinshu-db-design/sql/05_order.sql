-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 交易履约域 (order_*)
-- ============================================================

-- ----- 1. 收货地址 -----
CREATE TABLE order_shipping_address (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    receiver_name   VARCHAR(64)     NOT NULL                 COMMENT '收货人姓名',
    phone           VARCHAR(20)     NOT NULL                 COMMENT '收货人手机',
    province        VARCHAR(32)     NOT NULL,
    city            VARCHAR(32)     NOT NULL,
    district        VARCHAR(32)     NULL,
    address_detail  VARCHAR(256)    NOT NULL                 COMMENT '详细地址',
    postal_code     VARCHAR(10)     NULL,
    is_default      TINYINT(1)      NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    CONSTRAINT fk_addr_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='收货地址';

-- ----- 2. 订单主表 -----
CREATE TABLE order_order (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    order_no        VARCHAR(32)     NOT NULL                 COMMENT '业务订单号',
    user_id         BIGINT          NOT NULL,
    order_type      VARCHAR(16)     NOT NULL DEFAULT 'normal' COMMENT 'normal / bundle / reprint',
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待支付 1=已支付 2=生产中 3=待发货 4=已发货 5=已签收 6=已完成 7=已关闭 8=已取消',
    item_count      INT             NOT NULL DEFAULT 0,
    total_amount    DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '订单总额',
    discount_amount DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '优惠金额',
    freight_amount  DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '运费',
    pay_amount      DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '实付金额',
    -- 收货快照 (下单时冻结, 不被地址修改影响)
    ship_receiver   VARCHAR(64)     NOT NULL,
    ship_phone      VARCHAR(20)     NOT NULL,
    ship_province   VARCHAR(32)     NOT NULL,
    ship_city       VARCHAR(32)     NOT NULL,
    ship_district   VARCHAR(32)     NULL,
    ship_address    VARCHAR(256)    NOT NULL,
    ship_postal     VARCHAR(10)     NULL,
    -- 代理归因
    agent_account_id BIGINT         NULL                     COMMENT '-> agent_account.id (若通过代理下单)',
    agent_share_link_id BIGINT      NULL                     COMMENT '-> agent_share_link.id',
    -- 套餐
    bundle_id       BIGINT          NULL                     COMMENT '-> catalog_bundle.id (套餐订单)',
    remark          VARCHAR(500)    NULL                     COMMENT '用户备注',
    estimated_ship_date DATE        NULL                     COMMENT '预计发货日',
    paid_at         DATETIME        NULL,
    closed_at       DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_order_no (order_no),
    INDEX idx_user_status (user_id, status),
    INDEX idx_user_created (user_id, created_at DESC),
    CONSTRAINT fk_order_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='订单';

-- ----- 3. 订单项 -----
CREATE TABLE order_order_item (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    order_id        BIGINT          NOT NULL                 COMMENT '-> order_order.id',
    item_no         SMALLINT        NOT NULL                 COMMENT '项序号',
    project_id      BIGINT          NULL                     COMMENT '-> book_project.id',
    project_version_id BIGINT       NULL                     COMMENT '-> book_project_version.id (冻结版本)',
    sku_id          BIGINT          NULL                     COMMENT '-> catalog_product_sku.id (下单时)',
    price_snapshot_id BIGINT        NULL                     COMMENT '-> order_price_snapshot.id',
    quantity        INT             NOT NULL DEFAULT 1,
    unit_price      DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    subtotal        DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待处理 1=生产中 2=已完成 3=已取消',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_order_id (order_id),
    CONSTRAINT fk_item_order FOREIGN KEY (order_id) REFERENCES order_order (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='订单项';

-- ----- 4. 订单价格快照 (不可变) -----
CREATE TABLE order_price_snapshot (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    order_item_id   BIGINT          NOT NULL                 COMMENT '-> order_order_item.id',
    -- 商品快照
    spu_code        VARCHAR(32)     NOT NULL                 COMMENT '产品编码快照',
    spu_name        VARCHAR(128)    NOT NULL,
    sku_code        VARCHAR(64)     NOT NULL,
    size_label      VARCHAR(32)     NULL                     COMMENT '尺寸名称 如 A5 (14.8×21)',
    binding_label   VARCHAR(32)     NULL                     COMMENT '装帧名称 如 经济装',
    layout_label    VARCHAR(32)     NULL                     COMMENT '版式名称 如 瀑布流',
    cover_desc      VARCHAR(256)    NULL                     COMMENT '封面描述(模板名或自定义)',
    -- 数量与价格
    page_count      INT             NOT NULL,
    image_count     INT             NOT NULL DEFAULT 0,
    base_price      DECIMAL(10,2)   NOT NULL                 COMMENT '基础价格',
    page_unit_price DECIMAL(10,2)   NOT NULL                 COMMENT '每页单价',
    calculated_price DECIMAL(10,2)  NOT NULL                 COMMENT '公式计算价: base + page_unit * pages',
    agent_markup    DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '代理加价',
    discount        DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '折扣',
    final_price     DECIMAL(10,2)   NOT NULL                 COMMENT '最终单价',
    -- 套餐信息
    is_bundle_item  TINYINT(1)      NOT NULL DEFAULT 0,
    bundle_code     VARCHAR(32)     NULL,
    bundle_name     VARCHAR(128)    NULL,
    -- 发货
    estimated_ship_date DATE        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_order_item (order_item_id),
    CONSTRAINT fk_snapshot_item FOREIGN KEY (order_item_id) REFERENCES order_order_item (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='订单价格快照(不可变)';

-- ----- 5. 支付单 -----
CREATE TABLE order_payment (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    payment_no      VARCHAR(32)     NOT NULL                 COMMENT '支付单号',
    order_id        BIGINT          NOT NULL                 COMMENT '-> order_order.id',
    user_id         BIGINT          NOT NULL,
    pay_channel     VARCHAR(16)     NOT NULL                 COMMENT 'wechat_pay / alipay / agent_transfer',
    pay_amount      DECIMAL(10,2)   NOT NULL,
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待支付 1=支付中 2=已支付 3=支付失败 4=已退款 5=部分退款',
    paid_at         DATETIME        NULL,
    expired_at      DATETIME        NULL                     COMMENT '支付过期时间',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_payment_no (payment_no),
    INDEX idx_order_id (order_id),
    CONSTRAINT fk_payment_order FOREIGN KEY (order_id) REFERENCES order_order (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='支付单';

-- ----- 6. 支付交易明细 -----
CREATE TABLE order_payment_txn (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    payment_id      BIGINT          NOT NULL                 COMMENT '-> order_payment.id',
    txn_type        VARCHAR(16)     NOT NULL                 COMMENT 'pay / refund / chargeback',
    third_party_txn_id VARCHAR(128) NULL                     COMMENT '第三方交易号',
    amount          DECIMAL(10,2)   NOT NULL,
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=发起 1=成功 2=失败',
    idempotency_key VARCHAR(64)     NOT NULL                 COMMENT '幂等键',
    raw_callback    JSON            NULL                     COMMENT '第三方回调原始报文(脱敏)',
    notified_at     DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_idempotency (idempotency_key),
    INDEX idx_payment_id (payment_id),
    INDEX idx_third_party (third_party_txn_id),
    CONSTRAINT fk_txn_payment FOREIGN KEY (payment_id) REFERENCES order_payment (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='支付交易明细';

-- ----- 7. 物流单 -----
CREATE TABLE order_shipment (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    order_id        BIGINT          NOT NULL                 COMMENT '-> order_order.id',
    shipment_no     VARCHAR(32)     NOT NULL                 COMMENT '物流单号',
    carrier_code    VARCHAR(32)     NOT NULL                 COMMENT '快递公司代码',
    carrier_name    VARCHAR(64)     NULL,
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待发货 1=已发货 2=运输中 3=已签收 4=异常',
    shipped_at      DATETIME        NULL,
    delivered_at    DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_shipment_no (shipment_no),
    INDEX idx_order_id (order_id),
    CONSTRAINT fk_shipment_order FOREIGN KEY (order_id) REFERENCES order_order (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物流单';

-- ----- 8. 物流事件时间线 -----
CREATE TABLE order_shipment_event (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    shipment_id     BIGINT          NOT NULL                 COMMENT '-> order_shipment.id',
    event_time      DATETIME        NOT NULL                 COMMENT '事件发生时间',
    event_desc      VARCHAR(512)    NOT NULL                 COMMENT '事件描述',
    location        VARCHAR(256)    NULL,
    raw_data        JSON            NULL                     COMMENT '快递100等第三方原始数据',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_shipment_time (shipment_id, event_time),
    CONSTRAINT fk_event_shipment FOREIGN KEY (shipment_id) REFERENCES order_shipment (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物流事件时间线';

-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 代理分销域 (agent_*)
-- ============================================================

-- ----- 1. 代理申请 -----
CREATE TABLE agent_application (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    applicant_name  VARCHAR(64)     NOT NULL,
    contact_phone   VARCHAR(20)     NOT NULL,
    contact_wechat  VARCHAR(64)     NULL,
    channel_type    VARCHAR(32)     NULL                     COMMENT 'taobao / jd / wechat_shop / offline / other',
    shop_url        VARCHAR(512)    NULL                     COMMENT '店铺链接',
    reason          VARCHAR(500)    NULL                     COMMENT '申请理由',
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待审核 1=已通过 2=已拒绝 3=已注销',
    reviewed_at     DATETIME        NULL,
    reviewer_note   VARCHAR(500)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    INDEX idx_status (status),
    CONSTRAINT fk_app_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='代理申请';

-- ----- 2. 代理账号 -----
CREATE TABLE agent_account (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    application_id  BIGINT          NOT NULL                 COMMENT '-> agent_application.id',
    agent_code      VARCHAR(16)     NOT NULL                 COMMENT '代理编号',
    status          TINYINT         NOT NULL DEFAULT 1       COMMENT '1=正常 2=冻结 3=注销',
    balance         DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '可提现余额',
    frozen_balance  DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '冻结金额(提现中)',
    total_commission DECIMAL(12,2)  NOT NULL DEFAULT 0.00    COMMENT '累计佣金',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_user_id (user_id),
    UNIQUE KEY uk_agent_code (agent_code),
    CONSTRAINT fk_agent_user FOREIGN KEY (user_id) REFERENCES identity_user (id),
    CONSTRAINT fk_agent_app FOREIGN KEY (application_id) REFERENCES agent_application (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='代理账号';

-- ----- 3. 代理品牌 (自定义品牌名/Logo) -----
CREATE TABLE agent_brand (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    agent_id        BIGINT          NOT NULL                 COMMENT '-> agent_account.id',
    brand_name      VARCHAR(128)    NOT NULL,
    brand_logo_url  VARCHAR(1024)   NULL,
    brand_intro     VARCHAR(500)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_agent_id (agent_id),
    CONSTRAINT fk_brand_agent FOREIGN KEY (agent_id) REFERENCES agent_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='代理品牌';

-- ----- 4. 代理专属链接 -----
CREATE TABLE agent_share_link (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    agent_id        BIGINT          NOT NULL                 COMMENT '-> agent_account.id',
    link_code       VARCHAR(32)     NOT NULL                 COMMENT '短码, 唯一',
    link_url        VARCHAR(512)    NOT NULL                 COMMENT '完整推广链接',
    channel         VARCHAR(32)     NULL                     COMMENT 'wechat / taobao / general',
    click_count     BIGINT          NOT NULL DEFAULT 0,
    order_count     BIGINT          NOT NULL DEFAULT 0,
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_link_code (link_code),
    INDEX idx_agent_id (agent_id),
    CONSTRAINT fk_link_agent FOREIGN KEY (agent_id) REFERENCES agent_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='代理专属链接';

-- ----- 5. 代理自定义定价规则 -----
CREATE TABLE agent_pricing_rule (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    agent_id        BIGINT          NOT NULL                 COMMENT '-> agent_account.id',
    sku_id          BIGINT          NULL                     COMMENT '-> catalog_product_sku.id (NULL=全局)',
    spu_id          BIGINT          NULL                     COMMENT '-> catalog_product_spu.id (可选)',
    markup_type     VARCHAR(8)      NOT NULL DEFAULT 'fixed' COMMENT 'fixed / percent',
    markup_value    DECIMAL(10,2)   NOT NULL                 COMMENT '加价金额或百分比',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_agent_sku (agent_id, sku_id),
    CONSTRAINT fk_pricing_agent FOREIGN KEY (agent_id) REFERENCES agent_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='代理自定义定价规则';

-- ----- 6. 客户归因 (用户通过哪个代理链接注册/做书) -----
CREATE TABLE agent_customer_attribution (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id (被归因用户)',
    agent_id        BIGINT          NOT NULL                 COMMENT '-> agent_account.id',
    share_link_id   BIGINT          NULL                     COMMENT '-> agent_share_link.id',
    attribution_type VARCHAR(16)    NOT NULL DEFAULT 'first' COMMENT 'first / latest',
    attributed_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    INDEX idx_agent_id (agent_id),
    CONSTRAINT fk_attr_user FOREIGN KEY (user_id) REFERENCES identity_user (id),
    CONSTRAINT fk_attr_agent FOREIGN KEY (agent_id) REFERENCES agent_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户归因';

-- ----- 7. 佣金流水 -----
CREATE TABLE agent_commission_ledger (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    agent_id        BIGINT          NOT NULL                 COMMENT '-> agent_account.id',
    order_id        BIGINT          NULL                     COMMENT '-> order_order.id',
    order_item_id   BIGINT          NULL                     COMMENT '-> order_order_item.id',
    txn_type        VARCHAR(16)     NOT NULL                 COMMENT 'earn / reversal / freeze / unfreeze / withdraw',
    amount          DECIMAL(10,2)   NOT NULL                 COMMENT '金额(正=收入, 负=扣减)',
    balance_after   DECIMAL(10,2)   NOT NULL                 COMMENT '操作后余额',
    description     VARCHAR(256)    NULL,
    reference_no    VARCHAR(64)     NULL                     COMMENT '关联凭证号',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_agent_created (agent_id, created_at DESC),
    INDEX idx_order_id (order_id),
    CONSTRAINT fk_ledger_agent FOREIGN KEY (agent_id) REFERENCES agent_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='佣金流水';

-- ----- 8. 提现申请 -----
CREATE TABLE agent_withdraw_request (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    agent_id        BIGINT          NOT NULL                 COMMENT '-> agent_account.id',
    request_no      VARCHAR(32)     NOT NULL                 COMMENT '提现单号',
    amount          DECIMAL(10,2)   NOT NULL,
    withdraw_to     VARCHAR(32)     NOT NULL                 COMMENT 'wechat / alipay / bank',
    account_info    VARCHAR(256)    NOT NULL                 COMMENT '收款账号(脱敏保存)',
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待审核 1=处理中 2=已到账 3=已拒绝',
    reviewed_at     DATETIME        NULL,
    paid_at         DATETIME        NULL,
    reviewer_note   VARCHAR(256)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_request_no (request_no),
    INDEX idx_agent_status (agent_id, status),
    CONSTRAINT fk_withdraw_agent FOREIGN KEY (agent_id) REFERENCES agent_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='提现申请';

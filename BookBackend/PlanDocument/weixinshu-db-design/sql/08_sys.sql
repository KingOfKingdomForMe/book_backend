-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 合规审计域 (sys_*)
-- ============================================================

-- ----- 1. 隐私协议版本 -----
CREATE TABLE sys_privacy_policy_version (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    version_code    VARCHAR(16)     NOT NULL                 COMMENT '版本号 如 v2.0',
    title           VARCHAR(128)    NOT NULL,
    content_url     VARCHAR(1024)   NOT NULL                 COMMENT '协议全文地址',
    published_at    DATETIME        NOT NULL,
    is_current      TINYINT(1)      NOT NULL DEFAULT 0       COMMENT '是否当前生效版本',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_version_code (version_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='隐私协议版本';

-- ----- 2. 用户授权同意记录 -----
CREATE TABLE sys_consent_log (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    consent_type    VARCHAR(32)     NOT NULL                 COMMENT 'privacy_policy / wechat_auth / weibo_auth / content_import / address_collect',
    policy_version_id BIGINT        NULL                     COMMENT '-> sys_privacy_policy_version.id',
    action          VARCHAR(16)     NOT NULL                 COMMENT 'grant / revoke',
    ip_address      VARCHAR(45)     NULL,
    user_agent      VARCHAR(512)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_type (user_id, consent_type),
    INDEX idx_created (created_at),
    CONSTRAINT fk_consent_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户授权同意记录';

-- ----- 3. 审计日志 (关键操作) -----
CREATE TABLE sys_audit_log (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    actor_type      VARCHAR(16)     NOT NULL                 COMMENT 'user / admin / system',
    actor_id        BIGINT          NULL                     COMMENT '操作人ID',
    action          VARCHAR(64)     NOT NULL                 COMMENT '操作标识 如 user.delete / order.cancel',
    target_type     VARCHAR(32)     NULL                     COMMENT '目标实体类型',
    target_id       BIGINT          NULL                     COMMENT '目标实体ID',
    detail          JSON            NULL                     COMMENT '变更前后摘要',
    ip_address      VARCHAR(45)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_actor (actor_type, actor_id),
    INDEX idx_target (target_type, target_id),
    INDEX idx_created (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='审计日志';

-- ----- 4. 操作日志 (一般运营操作, 量较大) -----
CREATE TABLE sys_operation_log (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    operator_id     BIGINT          NULL,
    module          VARCHAR(32)     NOT NULL                 COMMENT '模块标识: order / book / agent / unboxing',
    operation       VARCHAR(64)     NOT NULL,
    target_type     VARCHAR(32)     NULL,
    target_id       BIGINT          NULL,
    summary         VARCHAR(512)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_module_created (module, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='操作日志';

-- ----- 5. 第三方共享记录 (与印刷/物流供应商共享用户数据) -----
CREATE TABLE sys_third_party_share_log (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    order_id        BIGINT          NULL                     COMMENT '-> order_order.id',
    third_party     VARCHAR(64)     NOT NULL                 COMMENT '第三方标识: print_vendor / logistics_sf / logistics_yd',
    shared_data_type VARCHAR(32)    NOT NULL                 COMMENT '共享数据类型: shipping_address / book_content / contact_info',
    purpose         VARCHAR(128)    NOT NULL                 COMMENT '共享目的',
    shared_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    INDEX idx_order_id (order_id),
    CONSTRAINT fk_share_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='第三方共享记录';

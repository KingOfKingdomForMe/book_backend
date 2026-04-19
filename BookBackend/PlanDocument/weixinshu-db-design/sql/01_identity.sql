-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 身份域 (identity_*)
-- 基于公开网页推断，非真实生产库还原
-- 目标: MySQL 8 + InnoDB, utf8mb4
-- ============================================================

-- ----- 1. 用户主账号 -----
CREATE TABLE identity_user (
    id              BIGINT          NOT NULL AUTO_INCREMENT  COMMENT '主键',
    user_no         VARCHAR(32)     NOT NULL                 COMMENT '业务用户编号(对外展示)',
    phone           VARCHAR(20)     NULL                     COMMENT '手机号(可选, 用于账密登录)',
    password_hash   VARCHAR(256)    NULL                     COMMENT '密码哈希(仅账密登录时存在)',
    status          TINYINT         NOT NULL DEFAULT 1       COMMENT '1=正常 2=冻结 3=注销',
    registered_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '注册时间',
    last_login_at   DATETIME        NULL                     COMMENT '最近登录时间',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_user_no (user_no),
    UNIQUE KEY uk_phone (phone)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户主账号';

-- ----- 2. 用户详情/画像 -----
CREATE TABLE identity_user_profile (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    nickname        VARCHAR(128)    NULL                     COMMENT '昵称',
    avatar_url      VARCHAR(512)    NULL                     COMMENT '头像 URL',
    gender          TINYINT         NULL                     COMMENT '0=未知 1=男 2=女',
    city            VARCHAR(64)     NULL                     COMMENT '所在城市',
    bio             VARCHAR(500)    NULL                     COMMENT '个人简介',
    extra           JSON            NULL                     COMMENT '扩展字段',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_user_id (user_id),
    CONSTRAINT fk_profile_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户画像';

-- ----- 3. 外部身份绑定 (微信/微博等) -----
CREATE TABLE identity_external_identity (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    platform        VARCHAR(32)     NOT NULL                 COMMENT '平台标识: wechat_mp / wechat_open / weibo / apple 等',
    external_uid    VARCHAR(128)    NOT NULL                 COMMENT '平台侧唯一用户ID (openid/unionid/uid)',
    external_name   VARCHAR(128)    NULL                     COMMENT '平台昵称快照',
    external_avatar VARCHAR(512)    NULL                     COMMENT '平台头像快照',
    access_token    VARCHAR(512)    NULL                     COMMENT '授权令牌(加密存储)',
    refresh_token   VARCHAR(512)    NULL                     COMMENT '刷新令牌(加密存储)',
    token_expires_at DATETIME       NULL                     COMMENT '令牌过期时间',
    auth_status     TINYINT         NOT NULL DEFAULT 1       COMMENT '1=已授权 2=已过期 3=已解绑',
    raw_auth_data   JSON            NULL                     COMMENT '平台原始授权返回(脱敏后保存)',
    bound_at        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '绑定时间',
    unbound_at      DATETIME        NULL                     COMMENT '解绑时间',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_platform_ext_uid (platform, external_uid),
    INDEX idx_user_id (user_id),
    CONSTRAINT fk_ext_identity_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='外部平台身份绑定';

-- ----- 4. 授权/登录会话 (扫码流程/OAuth 中间态) -----
CREATE TABLE identity_auth_session (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    session_key     VARCHAR(64)     NOT NULL                 COMMENT '会话唯一标识(UUID/随机码)',
    platform        VARCHAR(32)     NOT NULL                 COMMENT '发起平台',
    purpose         VARCHAR(32)     NOT NULL DEFAULT 'login' COMMENT 'login / bind / import_auth',
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待扫码 1=已扫码 2=已确认 3=已完成 4=已过期 5=已取消',
    user_id         BIGINT          NULL                     COMMENT '完成后关联的用户',
    external_identity_id BIGINT     NULL                     COMMENT '完成后关联的外部身份',
    ip_address      VARCHAR(45)     NULL                     COMMENT '发起端 IP',
    user_agent      VARCHAR(512)    NULL                     COMMENT '发起端 UA',
    expires_at      DATETIME        NOT NULL                 COMMENT '过期时间',
    completed_at    DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_session_key (session_key),
    INDEX idx_status_expires (status, expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='授权/登录会话';

-- ----- 5. 持久登录令牌 (Refresh Token) -----
CREATE TABLE identity_login_token (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL,
    token_hash      VARCHAR(128)    NOT NULL                 COMMENT 'refresh token 的 SHA-256 哈希',
    device_info     VARCHAR(256)    NULL                     COMMENT '设备标识',
    issued_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at      DATETIME        NOT NULL,
    revoked_at      DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_token_hash (token_hash),
    INDEX idx_user_expires (user_id, expires_at),
    CONSTRAINT fk_login_token_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='持久登录令牌';

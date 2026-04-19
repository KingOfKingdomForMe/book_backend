-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 社区晒单域 (community_*)
-- ============================================================

-- ----- 1. 晒单等级 -----
CREATE TABLE community_unboxing_level (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    level_code      VARCHAR(16)     NOT NULL                 COMMENT 'silver / gold / diamond',
    level_name      VARCHAR(32)     NOT NULL                 COMMENT '白银晒单 / 黄金晒单 / 钻石晒单',
    icon_url        VARCHAR(512)    NULL,
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_level_code (level_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='晒单等级';

-- ----- 2. 晒单帖子 -----
CREATE TABLE community_unboxing_post (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    post_no         VARCHAR(32)     NOT NULL                 COMMENT '晒单编号 (URL slug)',
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    author_name     VARCHAR(128)    NULL                     COMMENT '作者显示名',
    author_avatar   VARCHAR(512)    NULL                     COMMENT '作者头像快照',
    -- 关联信息
    order_item_id   BIGINT          NULL                     COMMENT '-> order_order_item.id (可选)',
    project_version_id BIGINT       NULL                     COMMENT '-> book_project_version.id (可选)',
    -- 产品标签快照
    product_label   VARCHAR(128)    NULL                     COMMENT '如 微信书 A5瀑布流 经济装',
    book_title      VARCHAR(256)    NULL                     COMMENT '作品名称快照',
    -- 等级
    level_id        BIGINT          NULL                     COMMENT '-> community_unboxing_level.id',
    -- 内容
    title           VARCHAR(256)    NULL                     COMMENT '晒单标题 (来自书名)',
    content_text    TEXT            NULL                     COMMENT '晒单正文',
    -- 状态与运营
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待审核 1=已发布 2=已隐藏 3=已删除',
    is_featured     TINYINT(1)      NOT NULL DEFAULT 0       COMMENT '是否精选(首页展示)',
    published_at    DATETIME        NULL                     COMMENT '发布时间',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_post_no (post_no),
    INDEX idx_user_id (user_id),
    INDEX idx_published (status, published_at DESC),
    INDEX idx_level (level_id, published_at DESC),
    CONSTRAINT fk_unbox_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='晒单帖子';

-- ----- 3. 晒单媒体 -----
CREATE TABLE community_unboxing_media (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    post_id         BIGINT          NOT NULL                 COMMENT '-> community_unboxing_post.id',
    media_type      VARCHAR(16)     NOT NULL                 COMMENT 'image / video',
    storage_url     VARCHAR(1024)   NOT NULL,
    thumbnail_url   VARCHAR(1024)   NULL,
    sort_order      SMALLINT        NOT NULL DEFAULT 0,
    width           INT             NULL,
    height          INT             NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_post_id (post_id),
    CONSTRAINT fk_umedia_post FOREIGN KEY (post_id) REFERENCES community_unboxing_post (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='晒单媒体';

-- ----- 4. 晒单标签 (产品类型/场景 等可运营标签) -----
CREATE TABLE community_unboxing_tag (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    post_id         BIGINT          NOT NULL,
    tag_name        VARCHAR(64)     NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_post_id (post_id),
    INDEX idx_tag_name (tag_name),
    CONSTRAINT fk_utag_post FOREIGN KEY (post_id) REFERENCES community_unboxing_post (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='晒单标签';

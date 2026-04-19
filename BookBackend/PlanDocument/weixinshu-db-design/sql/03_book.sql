-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 作品域 (book_*)
-- ============================================================

-- ----- 1. 作品项目 (一部书的根对象) -----
CREATE TABLE book_project (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '作品所有者 -> identity_user.id',
    book_type       VARCHAR(32)     NOT NULL                 COMMENT 'wxbook / wbbook / diary / blogbook / album / photo / frame 等',
    title           VARCHAR(256)    NOT NULL DEFAULT ''      COMMENT '书名',
    subtitle        VARCHAR(256)    NULL                     COMMENT '副标题',
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=草稿 1=编辑中 2=待下单 3=已下单 4=已归档',
    spu_id          BIGINT          NULL                     COMMENT '关联商品SPU -> catalog_product_spu.id',
    page_count      INT             NOT NULL DEFAULT 0       COMMENT '当前页数',
    image_count     INT             NOT NULL DEFAULT 0       COMMENT '当前图片数',
    is_gift         TINYINT(1)      NOT NULL DEFAULT 0       COMMENT '是否为好友代做(助手功能)',
    gift_target_name VARCHAR(128)   NULL                     COMMENT '代做目标昵称',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_status (user_id, status),
    INDEX idx_user_updated (user_id, updated_at DESC),
    CONSTRAINT fk_book_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品项目';

-- ----- 2. 作品内容条目 (排序后进入成书的内容单元) -----
CREATE TABLE book_project_entry (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    project_id      BIGINT          NOT NULL                 COMMENT '-> book_project.id',
    sort_order      INT             NOT NULL DEFAULT 0       COMMENT '在书内的排序',
    entry_type      VARCHAR(16)     NOT NULL                 COMMENT 'imported / manual_text / manual_photo / separator / chapter_title',
    content_post_id BIGINT          NULL                     COMMENT '-> content_post.id (引用导入内容时)',
    custom_text     TEXT            NULL                     COMMENT '手动补写文本',
    is_included     TINYINT(1)      NOT NULL DEFAULT 1       COMMENT '是否包含在当前版本中',
    page_number     INT             NULL                     COMMENT '排版后所在页码(排版引擎回写)',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_project_sort (project_id, sort_order),
    CONSTRAINT fk_entry_project FOREIGN KEY (project_id) REFERENCES book_project (id),
    CONSTRAINT fk_entry_post FOREIGN KEY (content_post_id) REFERENCES content_post (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品内容条目';

-- ----- 3. 作品素材 (条目级附件, 可覆盖原帖图片或添加自定义图) -----
CREATE TABLE book_project_asset (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    project_id      BIGINT          NOT NULL,
    entry_id        BIGINT          NULL                     COMMENT '-> book_project_entry.id (条目级素材)',
    asset_type      VARCHAR(16)     NOT NULL                 COMMENT 'image / video_cover',
    storage_url     VARCHAR(1024)   NOT NULL,
    thumbnail_url   VARCHAR(1024)   NULL,
    sort_order      SMALLINT        NOT NULL DEFAULT 0,
    width           INT             NULL,
    height          INT             NULL,
    file_size       BIGINT          NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_project_id (project_id),
    CONSTRAINT fk_asset_project FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品素材';

-- ----- 4. 作品封面配置 -----
CREATE TABLE book_project_cover (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    project_id      BIGINT          NOT NULL,
    cover_source    VARCHAR(16)     NOT NULL                 COMMENT 'template / custom_upload',
    template_id     BIGINT          NULL                     COMMENT '-> catalog_cover_template.id',
    custom_image_url VARCHAR(1024)  NULL                     COMMENT '自定义封面图片地址',
    cover_text      VARCHAR(256)    NULL                     COMMENT '封面文字',
    render_params   JSON            NULL                     COMMENT '封面渲染参数(颜色/字体/布局)',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_project_id (project_id),
    CONSTRAINT fk_cover_project FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品封面配置';

-- ----- 5. 作品序言/致谢 -----
CREATE TABLE book_project_preface (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    project_id      BIGINT          NOT NULL,
    section_type    VARCHAR(16)     NOT NULL                 COMMENT 'preface / acknowledgment',
    content_text    TEXT            NULL,
    author_name     VARCHAR(128)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_project_section (project_id, section_type),
    CONSTRAINT fk_preface_project FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品序言/致谢';

-- ----- 6. 作品版式/装帧选择 -----
CREATE TABLE book_project_layout (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    project_id      BIGINT          NOT NULL,
    size_code       VARCHAR(16)     NOT NULL                 COMMENT 'A5 / A4',
    layout_code     VARCHAR(32)     NOT NULL                 COMMENT '版式代码: waterfall / double_col / magazine / shiguang / single_page',
    binding_code    VARCHAR(32)     NOT NULL                 COMMENT '装帧: economy / literary / hardcover / collector',
    layout_template_id BIGINT       NULL                     COMMENT '-> catalog_layout_template.id',
    render_params   JSON            NULL                     COMMENT '排版渲染参数',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_project_id (project_id),
    CONSTRAINT fk_layout_project FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品版式/装帧选择';

-- ----- 7. 作品版本快照 (下单时冻结) -----
CREATE TABLE book_project_version (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    project_id      BIGINT          NOT NULL,
    version_no      INT             NOT NULL                 COMMENT '版本号, 自增',
    snapshot_data   JSON            NOT NULL                 COMMENT '完整排版快照: 标题/副标题/序言/封面/目录/内容排序/渲染参数/总页数',
    page_count      INT             NOT NULL,
    image_count     INT             NOT NULL DEFAULT 0,
    render_version  VARCHAR(32)     NULL                     COMMENT '排版引擎版本号',
    frozen_at       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '快照冻结时间',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_project_version (project_id, version_no),
    CONSTRAINT fk_version_project FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='作品版本快照';

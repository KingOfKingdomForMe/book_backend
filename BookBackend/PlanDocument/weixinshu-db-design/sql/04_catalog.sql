-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 商品目录域 (catalog_*)
-- ============================================================

-- ----- 1. 商品分类 -----
CREATE TABLE catalog_category (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    name            VARCHAR(64)     NOT NULL                 COMMENT '分类名称: 图文书 / 照片书 / 冲印 / 摆台',
    slug            VARCHAR(64)     NOT NULL                 COMMENT 'URL friendly 标识',
    sort_order      INT             NOT NULL DEFAULT 0,
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_slug (slug)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='商品分类';

-- ----- 2. 商品 SPU (标准产品单元) -----
CREATE TABLE catalog_product_spu (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    category_id     BIGINT          NOT NULL                 COMMENT '-> catalog_category.id',
    spu_code        VARCHAR(32)     NOT NULL                 COMMENT '产品编码: wxbook / wbbook / diary / blogbook / album / photo / frame 等',
    name            VARCHAR(128)    NOT NULL                 COMMENT '产品名称',
    subtitle        VARCHAR(256)    NULL,
    description     TEXT            NULL,
    cover_image_url VARCHAR(1024)   NULL,
    content_source  VARCHAR(32)     NOT NULL DEFAULT 'any'   COMMENT '适用内容源: wechat / weibo / diary / manual / any',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_spu_code (spu_code),
    INDEX idx_category (category_id),
    CONSTRAINT fk_spu_category FOREIGN KEY (category_id) REFERENCES catalog_category (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='商品SPU';

-- ----- 3. 规格定义 (维度) -----
CREATE TABLE catalog_spec_definition (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    spec_key        VARCHAR(32)     NOT NULL                 COMMENT '规格维度键: size / binding / layout',
    spec_label      VARCHAR(64)     NOT NULL                 COMMENT '显示名称: 尺寸 / 装订方式 / 版式',
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_spec_key (spec_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='规格维度定义';

-- ----- 4. 规格值 -----
CREATE TABLE catalog_spec_value (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    spec_id         BIGINT          NOT NULL                 COMMENT '-> catalog_spec_definition.id',
    value_code      VARCHAR(32)     NOT NULL                 COMMENT '值编码: A5 / A4 / economy / literary / hardcover / collector / waterfall / magazine',
    value_label     VARCHAR(64)     NOT NULL                 COMMENT '显示名: A5 (14.8×21) / 经济装 / 瀑布流',
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_spec_value (spec_id, value_code),
    CONSTRAINT fk_specval_def FOREIGN KEY (spec_id) REFERENCES catalog_spec_definition (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='规格值';

-- ----- 5. 商品 SKU -----
CREATE TABLE catalog_product_sku (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    spu_id          BIGINT          NOT NULL                 COMMENT '-> catalog_product_spu.id',
    sku_code        VARCHAR(64)     NOT NULL                 COMMENT '唯一SKU编码',
    size_value_id   BIGINT          NULL                     COMMENT '-> catalog_spec_value.id (尺寸)',
    binding_value_id BIGINT         NULL                     COMMENT '-> catalog_spec_value.id (装帧)',
    layout_value_id BIGINT          NULL                     COMMENT '-> catalog_spec_value.id (版式)',
    min_pages       INT             NOT NULL DEFAULT 60      COMMENT '最低页数',
    max_pages       INT             NULL                     COMMENT '最高页数(NULL=不限)',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_sku_code (sku_code),
    INDEX idx_spu_id (spu_id),
    CONSTRAINT fk_sku_spu FOREIGN KEY (spu_id) REFERENCES catalog_product_spu (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='商品SKU';

-- ----- 6. 价格规则 -----
CREATE TABLE catalog_price_rule (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    sku_id          BIGINT          NOT NULL                 COMMENT '-> catalog_product_sku.id',
    base_price      DECIMAL(10,2)   NOT NULL                 COMMENT '基础价格(元)',
    page_unit_price DECIMAL(10,2)   NOT NULL DEFAULT 0.00    COMMENT '每页单价(元)',
    effective_from  DATETIME        NOT NULL                 COMMENT '生效时间',
    effective_to    DATETIME        NULL                     COMMENT '失效时间(NULL=长期)',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_sku_active (sku_id, is_active, effective_from),
    CONSTRAINT fk_price_sku FOREIGN KEY (sku_id) REFERENCES catalog_product_sku (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='价格规则';

-- ----- 7. 版式模板 -----
CREATE TABLE catalog_layout_template (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    layout_code     VARCHAR(32)     NOT NULL                 COMMENT '版式代码',
    name            VARCHAR(64)     NOT NULL,
    description     VARCHAR(256)    NULL,
    preview_image   VARCHAR(1024)   NULL,
    applicable_sizes VARCHAR(64)    NOT NULL DEFAULT 'A5,A4' COMMENT '适用尺寸, 逗号分隔',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_layout_code (layout_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='版式模板';

-- ----- 8. 封面模板 -----
CREATE TABLE catalog_cover_template (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    template_code   VARCHAR(32)     NOT NULL,
    name            VARCHAR(64)     NOT NULL,
    category        VARCHAR(32)     NULL                     COMMENT '封面分类: animal_city / nostalgia / magazine / custom 等',
    preview_image   VARCHAR(1024)   NULL,
    applicable_spus VARCHAR(256)    NULL                     COMMENT '适用SPU编码列表, 逗号分隔',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_template_code (template_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='封面模板';

-- ----- 9. 套餐/组合包 -----
CREATE TABLE catalog_bundle (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    bundle_code     VARCHAR(32)     NOT NULL                 COMMENT '如 TNEORJ',
    name            VARCHAR(128)    NOT NULL                 COMMENT '轻奢杂志册6件套',
    description     TEXT            NULL,
    cover_image_url VARCHAR(1024)   NULL,
    bundle_price    DECIMAL(10,2)   NOT NULL                 COMMENT '套餐价',
    original_price  DECIMAL(10,2)   NULL                     COMMENT '原价(用于划线)',
    is_active       TINYINT(1)      NOT NULL DEFAULT 1,
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_bundle_code (bundle_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='套餐/组合包';

-- ----- 10. 套餐明细 -----
CREATE TABLE catalog_bundle_item (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    bundle_id       BIGINT          NOT NULL                 COMMENT '-> catalog_bundle.id',
    sku_id          BIGINT          NOT NULL                 COMMENT '-> catalog_product_sku.id',
    quantity        INT             NOT NULL DEFAULT 1,
    max_pages       INT             NULL                     COMMENT '套餐内该项上限页数(如24页)',
    max_images      INT             NULL                     COMMENT '套餐内该项上限图片数',
    sort_order      INT             NOT NULL DEFAULT 0,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_bundle (bundle_id),
    CONSTRAINT fk_bitem_bundle FOREIGN KEY (bundle_id) REFERENCES catalog_bundle (id),
    CONSTRAINT fk_bitem_sku FOREIGN KEY (sku_id) REFERENCES catalog_product_sku (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='套餐明细';

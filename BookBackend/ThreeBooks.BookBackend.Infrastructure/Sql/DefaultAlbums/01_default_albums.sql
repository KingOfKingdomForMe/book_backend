CREATE TABLE IF NOT EXISTS default_album (
    id BIGINT NOT NULL AUTO_INCREMENT,
    album_code VARCHAR(64) NOT NULL COMMENT '默认相册编码, 对外稳定标识',
    product_spu_id BIGINT NULL COMMENT '-> catalog_product_spu.id, 一对一绑定的产品',
    name VARCHAR(128) NOT NULL COMMENT '默认相册名称',
    description VARCHAR(512) NULL COMMENT '默认相册说明',
    extra_properties_json JSON NULL COMMENT '默认相册额外属性(JSON字符串数组)',
    book_type VARCHAR(32) NULL COMMENT '适用书册类型, NULL=通用',
    category VARCHAR(32) NULL COMMENT '默认相册分类',
    theme_code VARCHAR(64) NULL COMMENT '主题编码',
    preview_file_id BIGINT NULL COMMENT '-> storage_file_object.id, 默认相册预览图',
    created_by_user_id BIGINT NULL COMMENT '-> identity_user.id, 创建人',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否启用',
    sort_order INT NOT NULL DEFAULT 0 COMMENT '排序值, 越小越靠前',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_default_album_code (album_code),
    UNIQUE KEY uk_default_album_product_spu (product_spu_id),
    INDEX idx_default_album_lookup (is_active, book_type, category, sort_order, id),
    INDEX idx_default_album_product_lookup (product_spu_id, is_active, sort_order, id),
    INDEX idx_default_album_user (created_by_user_id, updated_at),
    CONSTRAINT fk_default_album_product_spu FOREIGN KEY (product_spu_id) REFERENCES catalog_product_spu (id),
    CONSTRAINT fk_default_album_preview_file FOREIGN KEY (preview_file_id) REFERENCES storage_file_object (id),
    CONSTRAINT fk_default_album_user FOREIGN KEY (created_by_user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='默认相册主表, 用于聚合多个模板内容';

CREATE TABLE IF NOT EXISTS default_album_template (
    id BIGINT NOT NULL AUTO_INCREMENT,
    default_album_id BIGINT NOT NULL COMMENT '-> default_album.id',
    template_id BIGINT NOT NULL COMMENT '-> album_content_template.id',
    sort_order INT NOT NULL DEFAULT 0 COMMENT '排序值, 越小越靠前',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_default_album_template_unique (default_album_id, template_id),
    INDEX idx_default_album_template_lookup (default_album_id, sort_order, id),
    CONSTRAINT fk_default_album_template_album FOREIGN KEY (default_album_id) REFERENCES default_album (id) ON DELETE CASCADE,
    CONSTRAINT fk_default_album_template_template FOREIGN KEY (template_id) REFERENCES album_content_template (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='默认相册与相册模板内容的关联表';
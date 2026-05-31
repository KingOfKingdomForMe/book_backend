-- Upgrade existing default_album tables to support one-to-one product mapping.

SET @default_album_has_product_spu_id = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'default_album'
      AND COLUMN_NAME = 'product_spu_id'
);

SET @default_album_add_product_spu_id_sql = IF(
    @default_album_has_product_spu_id = 0,
    'ALTER TABLE default_album ADD COLUMN product_spu_id BIGINT NULL COMMENT ''-> catalog_product_spu.id, 一对一绑定的产品'' AFTER album_code',
    'SELECT 1'
);
PREPARE default_album_add_product_spu_id_stmt FROM @default_album_add_product_spu_id_sql;
EXECUTE default_album_add_product_spu_id_stmt;
DEALLOCATE PREPARE default_album_add_product_spu_id_stmt;

SET @default_album_has_product_lookup_index = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'default_album'
      AND INDEX_NAME = 'idx_default_album_product_lookup'
);

SET @default_album_add_product_lookup_index_sql = IF(
    @default_album_has_product_lookup_index = 0,
    'ALTER TABLE default_album ADD INDEX idx_default_album_product_lookup (product_spu_id, is_active, sort_order, id)',
    'SELECT 1'
);
PREPARE default_album_add_product_lookup_index_stmt FROM @default_album_add_product_lookup_index_sql;
EXECUTE default_album_add_product_lookup_index_stmt;
DEALLOCATE PREPARE default_album_add_product_lookup_index_stmt;

SET @default_album_has_product_unique = (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'default_album'
      AND INDEX_NAME = 'uk_default_album_product_spu'
);

SET @default_album_add_product_unique_sql = IF(
    @default_album_has_product_unique = 0,
    'ALTER TABLE default_album ADD UNIQUE KEY uk_default_album_product_spu (product_spu_id)',
    'SELECT 1'
);
PREPARE default_album_add_product_unique_stmt FROM @default_album_add_product_unique_sql;
EXECUTE default_album_add_product_unique_stmt;
DEALLOCATE PREPARE default_album_add_product_unique_stmt;

SET @default_album_has_product_fk = (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'default_album'
      AND CONSTRAINT_NAME = 'fk_default_album_product_spu'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @default_album_add_product_fk_sql = IF(
    @default_album_has_product_fk = 0,
    'ALTER TABLE default_album ADD CONSTRAINT fk_default_album_product_spu FOREIGN KEY (product_spu_id) REFERENCES catalog_product_spu (id)',
    'SELECT 1'
);
PREPARE default_album_add_product_fk_stmt FROM @default_album_add_product_fk_sql;
EXECUTE default_album_add_product_fk_stmt;
DEALLOCATE PREPARE default_album_add_product_fk_stmt;
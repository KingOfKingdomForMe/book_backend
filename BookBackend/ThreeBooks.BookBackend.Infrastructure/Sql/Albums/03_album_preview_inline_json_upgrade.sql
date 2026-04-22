-- ============================================================
-- Upgrade existing album preview pages to inline JSON source storage
-- Safe to execute on environments that already applied 01_album_preview.sql
-- ============================================================

SET @has_json_source = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'book_project_version_page'
      AND COLUMN_NAME = 'json_source'
);

SET @add_json_source_sql = IF(
    @has_json_source = 0,
    'ALTER TABLE book_project_version_page ADD COLUMN json_source LONGTEXT NULL COMMENT ''Direct frontend JSON source text'' AFTER sort_order',
    'SELECT 1'
);

PREPARE add_json_source_stmt FROM @add_json_source_sql;
EXECUTE add_json_source_stmt;
DEALLOCATE PREPARE add_json_source_stmt;

ALTER TABLE book_project_version_page
    MODIFY COLUMN json_file_id BIGINT NULL COMMENT 'Optional legacy JSON file reference',
    MODIFY COLUMN json_bucket VARCHAR(128) NULL,
    MODIFY COLUMN json_object_key VARCHAR(512) NULL;
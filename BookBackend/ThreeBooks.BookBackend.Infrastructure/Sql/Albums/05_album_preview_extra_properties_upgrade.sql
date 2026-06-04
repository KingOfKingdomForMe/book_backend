-- ============================================================
-- Upgrade existing book_project rows to support album extra properties
-- Safe to execute on environments that already applied 01_album_preview.sql
-- ============================================================

SET @book_project_has_extra_properties_json = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'book_project'
      AND COLUMN_NAME = 'extra_properties_json'
);

SET @book_project_add_extra_properties_json_sql = IF(
    @book_project_has_extra_properties_json = 0,
    'ALTER TABLE book_project ADD COLUMN extra_properties_json JSON NULL COMMENT ''Album extra properties copied from default album'' AFTER share_count',
    'SELECT 1'
);

PREPARE book_project_add_extra_properties_json_stmt FROM @book_project_add_extra_properties_json_sql;
EXECUTE book_project_add_extra_properties_json_stmt;
DEALLOCATE PREPARE book_project_add_extra_properties_json_stmt;
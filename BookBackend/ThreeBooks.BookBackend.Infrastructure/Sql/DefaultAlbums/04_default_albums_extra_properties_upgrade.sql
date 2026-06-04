-- Upgrade existing default_album rows to support extra properties.

SET @default_album_has_extra_properties_json = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'default_album'
      AND COLUMN_NAME = 'extra_properties_json'
);

SET @default_album_add_extra_properties_json_sql = IF(
    @default_album_has_extra_properties_json = 0,
    'ALTER TABLE default_album ADD COLUMN extra_properties_json JSON NULL COMMENT ''默认相册额外属性(JSON字符串数组)'' AFTER description',
    'SELECT 1'
);

PREPARE default_album_add_extra_properties_json_stmt FROM @default_album_add_extra_properties_json_sql;
EXECUTE default_album_add_extra_properties_json_stmt;
DEALLOCATE PREPARE default_album_add_extra_properties_json_stmt;
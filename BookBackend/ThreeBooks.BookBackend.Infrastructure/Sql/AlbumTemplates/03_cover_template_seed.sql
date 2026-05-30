-- ============================================================
-- Seed data for generated cover template management
-- Preconditions:
--   1. Apply ThreeBooks.BookBackend.Infrastructure/Sql/Files/01_file_storage.sql
--   2. Apply ThreeBooks.BookBackend.Infrastructure/Sql/AlbumTemplates/01_album_content_template.sql
--   3. Upload raster background images to storage_file_object, for example:
--      bucket = bookbackend-dev
--      object_key = cover-templates/defaults/story-cover-bg.png
--      object_key = cover-templates/defaults/editorial-cover-bg.png
--   4. This seed is manual data only; startup SQL bootstrap does not auto-apply seed files.
-- ============================================================

SET @sample_bucket = 'bookbackend-dev';
SET @story_cover_bg_object_key = 'cover-templates/defaults/story-cover-bg.png';
SET @editorial_cover_bg_object_key = 'cover-templates/defaults/editorial-cover-bg.png';

INSERT INTO album_content_template (
    template_code,
    name,
    description,
    book_type,
    page_type,
    category,
    theme_code,
    schema_version,
    json_source,
    preview_file_id,
    created_by_user_id,
    is_built_in,
    is_active,
    sort_order)
VALUES
(
    'generated-cover-story-default',
    '默认故事封面',
    '适合书册封面标题、副标题和作者名叠加的默认封面模板。',
    NULL,
    'cover-template',
    'generated-cover',
    'cover-template-story',
    '1.0',
    JSON_OBJECT(
        'schemaVersion', '1.0',
        'fields', JSON_ARRAY(
            JSON_OBJECT(
                'fieldId', 'title',
                'displayName', '标题',
                'placeholder', '请输入封面标题',
                'x', 120,
                'y', 140,
                'width', 840,
                'height', 180,
                'fontFamily', 'Microsoft YaHei',
                'fontSize', 60,
                'fontColor', '#FFFFFF',
                'isRequired', TRUE,
                'sortOrder', 10,
                'defaultValue', '',
                'horizontalAlignment', 'center',
                'verticalAlignment', 'center',
                'maxLength', 40
            ),
            JSON_OBJECT(
                'fieldId', 'subtitle',
                'displayName', '副标题',
                'placeholder', '请输入封面副标题',
                'x', 180,
                'y', 350,
                'width', 720,
                'height', 96,
                'fontFamily', 'Microsoft YaHei',
                'fontSize', 28,
                'fontColor', '#F2F2F2',
                'isRequired', FALSE,
                'sortOrder', 20,
                'defaultValue', '',
                'horizontalAlignment', 'center',
                'verticalAlignment', 'center',
                'maxLength', 80
            ),
            JSON_OBJECT(
                'fieldId', 'author',
                'displayName', '作者名',
                'placeholder', '请输入作者名',
                'x', 260,
                'y', 560,
                'width', 560,
                'height', 64,
                'fontFamily', 'Microsoft YaHei',
                'fontSize', 22,
                'fontColor', '#FFFFFF',
                'isRequired', FALSE,
                'sortOrder', 30,
                'defaultValue', '',
                'horizontalAlignment', 'center',
                'verticalAlignment', 'center',
                'maxLength', 40
            )
        )
    ),
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @story_cover_bg_object_key LIMIT 1),
    NULL,
    1,
    1,
    100
),
(
    'generated-cover-editorial-default',
    '默认信息封面',
    '适合标题、期号和日期信息的简洁封面模板。',
    NULL,
    'cover-template',
    'generated-cover',
    'cover-template-editorial',
    '1.0',
    JSON_OBJECT(
        'schemaVersion', '1.0',
        'fields', JSON_ARRAY(
            JSON_OBJECT(
                'fieldId', 'issue',
                'displayName', '期号',
                'placeholder', 'VOL.01',
                'x', 96,
                'y', 100,
                'width', 220,
                'height', 56,
                'fontFamily', 'Segoe UI',
                'fontSize', 20,
                'fontColor', '#FFFFFF',
                'isRequired', FALSE,
                'sortOrder', 10,
                'defaultValue', '',
                'horizontalAlignment', 'left',
                'verticalAlignment', 'center',
                'maxLength', 20
            ),
            JSON_OBJECT(
                'fieldId', 'title',
                'displayName', '主标题',
                'placeholder', '请输入主标题',
                'x', 96,
                'y', 220,
                'width', 560,
                'height', 220,
                'fontFamily', 'Segoe UI',
                'fontSize', 54,
                'fontColor', '#FFFFFF',
                'isRequired', TRUE,
                'sortOrder', 20,
                'defaultValue', '',
                'horizontalAlignment', 'left',
                'verticalAlignment', 'top',
                'maxLength', 48
            ),
            JSON_OBJECT(
                'fieldId', 'publish_date',
                'displayName', '发布日期',
                'placeholder', '2026.05',
                'x', 96,
                'y', 620,
                'width', 300,
                'height', 52,
                'fontFamily', 'Segoe UI',
                'fontSize', 18,
                'fontColor', '#F3F3F3',
                'isRequired', FALSE,
                'sortOrder', 30,
                'defaultValue', '',
                'horizontalAlignment', 'left',
                'verticalAlignment', 'center',
                'maxLength', 32
            )
        )
    ),
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @editorial_cover_bg_object_key LIMIT 1),
    NULL,
    1,
    1,
    110
)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    description = VALUES(description),
    book_type = VALUES(book_type),
    page_type = VALUES(page_type),
    category = VALUES(category),
    theme_code = VALUES(theme_code),
    schema_version = VALUES(schema_version),
    json_source = VALUES(json_source),
    preview_file_id = VALUES(preview_file_id),
    created_by_user_id = VALUES(created_by_user_id),
    is_built_in = VALUES(is_built_in),
    is_active = VALUES(is_active),
    sort_order = VALUES(sort_order),
    updated_at = CURRENT_TIMESTAMP;
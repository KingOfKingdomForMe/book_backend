-- ============================================================
-- Seed data for product-mapped default albums
-- Preconditions:
--   1. Apply PlanDocument/weixinshu-db-design/sql/04_catalog.sql and 09_catalog_seed_data.sql
--   2. Apply ThreeBooks.BookBackend.Infrastructure/Sql/AlbumTemplates/01_album_content_template.sql
--   3. Apply ThreeBooks.BookBackend.Infrastructure/Sql/DefaultAlbums/01_default_albums.sql
--   4. Apply ThreeBooks.BookBackend.Infrastructure/Sql/DefaultAlbums/Procedures/01_default_albums_procedures.sql
--   5. This seed is manual data only; startup SQL bootstrap does not auto-apply seed files.
-- ============================================================

SET @default_cover_code = 'default-product-cover';
SET @default_story_code = 'default-product-story';
SET @default_gallery_code = 'default-product-gallery';

DELETE FROM default_album_template
WHERE default_album_id IN (
    SELECT id FROM (
        SELECT id
        FROM default_album
        WHERE album_code IN ('default-growth-memory', 'default-campus-archive')
    ) AS legacy_albums
);

DELETE FROM default_album
WHERE album_code IN ('default-growth-memory', 'default-campus-archive');

DELETE FROM album_content_template
WHERE template_code IN (
    'default-growth-cover',
    'default-growth-story',
    'default-growth-gallery',
    'default-campus-cover',
    'default-campus-story'
);

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
    @default_cover_code,
    '产品默认封面',
    '所有产品默认相册共享的封面模板。',
    'balbum',
    'cover',
    'product-default',
    'product-default',
    '2.0',
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'pageType', 'cover',
        'theme', 'product-default',
        'blocks', JSON_ARRAY(
            JSON_OBJECT('id', 'cover-title', 'type', 'text', 'text', '默认封面', 'style', JSON_OBJECT('fontSize', 42, 'fontWeight', 700)),
            JSON_OBJECT('id', 'cover-subtitle', 'type', 'text', 'text', '从产品默认相册快速开始', 'style', JSON_OBJECT('fontSize', 18, 'opacity', 0.75)),
            JSON_OBJECT('id', 'cover-image', 'type', 'image', 'src', '', 'alt', '默认封面图片', 'fit', 'cover')
        )
    ),
    NULL,
    NULL,
    1,
    1,
    10
),
(
    @default_story_code,
    '产品默认故事页',
    '所有产品默认相册共享的故事模板。',
    'balbum',
    'content',
    'product-default',
    'product-default',
    '2.0',
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'pageType', 'content',
        'theme', 'product-default',
        'layout', JSON_OBJECT('template', 'story-two-column'),
        'blocks', JSON_ARRAY(
            JSON_OBJECT('id', 'headline', 'type', 'text', 'text', '这一页写下你的故事', 'style', JSON_OBJECT('fontSize', 30, 'fontWeight', 700)),
            JSON_OBJECT('id', 'body', 'type', 'richText', 'content', JSON_ARRAY(
                JSON_OBJECT('type', 'paragraph', 'text', '默认相册会随产品创建一套初始页面，方便用户直接开始编辑。'),
                JSON_OBJECT('type', 'paragraph', 'text', '这段内容只是测试数据，可在编辑器里继续替换。')
            )),
            JSON_OBJECT('id', 'main-image', 'type', 'image', 'src', '', 'alt', '默认故事页主图', 'fit', 'cover')
        )
    ),
    NULL,
    NULL,
    1,
    1,
    20
),
(
    @default_gallery_code,
    '产品默认图片页',
    '所有产品默认相册共享的图片集合模板。',
    'balbum',
    'content',
    'product-default',
    'product-default',
    '2.0',
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'pageType', 'content',
        'theme', 'product-default',
        'layout', JSON_OBJECT('template', 'gallery-grid'),
        'blocks', JSON_ARRAY(
            JSON_OBJECT('id', 'title', 'type', 'text', 'text', '留出三张图的位置', 'style', JSON_OBJECT('fontSize', 26, 'fontWeight', 700)),
            JSON_OBJECT('id', 'gallery', 'type', 'gallery', 'items', JSON_ARRAY(
                JSON_OBJECT('slot', 1, 'src', ''),
                JSON_OBJECT('slot', 2, 'src', ''),
                JSON_OBJECT('slot', 3, 'src', '')
            )),
            JSON_OBJECT('id', 'caption', 'type', 'text', 'text', '这是给所有产品统一准备的默认图片页模板。', 'style', JSON_OBJECT('fontSize', 16, 'opacity', 0.72))
        )
    ),
    NULL,
    NULL,
    1,
    1,
    30
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

SET @default_cover_id = (SELECT id FROM album_content_template WHERE template_code = @default_cover_code LIMIT 1);
SET @default_story_id = (SELECT id FROM album_content_template WHERE template_code = @default_story_code LIMIT 1);
SET @default_gallery_id = (SELECT id FROM album_content_template WHERE template_code = @default_gallery_code LIMIT 1);

INSERT INTO default_album (
    album_code,
    product_spu_id,
    name,
    description,
    book_type,
    category,
    theme_code,
    preview_file_id,
    created_by_user_id,
    is_active,
    sort_order)
SELECT
    CONCAT('default-', spu.spu_code) AS album_code,
    spu.id AS product_spu_id,
    CONCAT(spu.name, '默认相册') AS name,
    CONCAT('为产品 ', spu.name, ' 准备的默认相册，用于初始化用户创建的第一本相册。') AS description,
    spu.spu_code AS book_type,
    'product-default' AS category,
    CONCAT('product-default-', spu.spu_code) AS theme_code,
    NULL AS preview_file_id,
    NULL AS created_by_user_id,
    1 AS is_active,
    spu.sort_order AS sort_order
FROM catalog_product_spu spu
WHERE spu.is_active = 1
ON DUPLICATE KEY UPDATE
    album_code = VALUES(album_code),
    product_spu_id = VALUES(product_spu_id),
    name = VALUES(name),
    description = VALUES(description),
    book_type = VALUES(book_type),
    category = VALUES(category),
    theme_code = VALUES(theme_code),
    preview_file_id = VALUES(preview_file_id),
    created_by_user_id = VALUES(created_by_user_id),
    is_active = VALUES(is_active),
    sort_order = VALUES(sort_order),
    updated_at = CURRENT_TIMESTAMP;

DELETE dat
FROM default_album_template dat
INNER JOIN default_album album ON album.id = dat.default_album_id
INNER JOIN catalog_product_spu spu ON spu.id = album.product_spu_id
WHERE spu.is_active = 1;

INSERT INTO default_album_template (
    default_album_id,
    template_id,
    sort_order)
SELECT album.id, @default_cover_id, 10
FROM default_album album
INNER JOIN catalog_product_spu spu ON spu.id = album.product_spu_id
WHERE spu.is_active = 1
UNION ALL
SELECT album.id, @default_story_id, 20
FROM default_album album
INNER JOIN catalog_product_spu spu ON spu.id = album.product_spu_id
WHERE spu.is_active = 1
UNION ALL
SELECT album.id, @default_gallery_id, 30
FROM default_album album
INNER JOIN catalog_product_spu spu ON spu.id = album.product_spu_id
WHERE spu.is_active = 1;
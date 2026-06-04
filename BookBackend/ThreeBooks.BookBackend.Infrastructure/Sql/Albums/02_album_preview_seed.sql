-- ============================================================
-- Seed data for public album preview sample
-- Preconditions:
--   1. Apply PlanDocument/weixinshu-db-design/sql/01_identity.sql
--   2. Apply PlanDocument/weixinshu-db-design/sql/03_book.sql
--   3. Apply ThreeBooks.BookBackend.Infrastructure/Sql/Files/01_file_storage.sql
--   4. Apply ThreeBooks.BookBackend.Infrastructure/Sql/Albums/01_album_preview.sql
--   5. For existing databases upgraded from the older preview schema, apply:
--      ThreeBooks.BookBackend.Infrastructure/Sql/Albums/03_album_preview_inline_json_upgrade.sql
--   6. Upload sample HTML and image files using:
--      ThreeBooks.BookBackend.Api/scripts/Upload-AlbumPreviewSampleFiles.ps1
-- ============================================================

SET @sample_user_id = 9001;
SET @sample_project_id = 9001;
SET @sample_version_id = 900101;
SET @sample_share_code = 'akqfpbdzr2goos';
SET @sample_list_public_project_id = 9002;
SET @sample_list_public_share_code = 'my-album-public-001';
SET @sample_list_private_project_id = 9003;
SET @sample_other_user_id = 9002;
SET @sample_other_user_project_id = 9011;
SET @sample_other_user_share_code = 'other-user-album-001';
SET @sample_bucket = 'bookbackend-dev';
SET @cover_hero_object_key = 'albums/akqfpbdzr2goos/assets/cover-hero.svg';
SET @content_hero_object_key = 'albums/akqfpbdzr2goos/assets/content-01.svg';
SET @cover_html_object_key = 'albums/akqfpbdzr2goos/html/page-001-cover.html';
SET @content_html_object_key = 'albums/akqfpbdzr2goos/html/page-004-content.html';
SET @cover_hero_url = CONCAT('/api/files/content/', @sample_bucket, '/', @cover_hero_object_key);
SET @content_hero_url = CONCAT('/api/files/content/', @sample_bucket, '/', @content_hero_object_key);

SET @page_001_json = CAST(JSON_OBJECT(
    'schemaVersion', '2.0',
    'pageNo', 1,
    'pageType', 'cover',
    'theme', 'album-inline-json',
    'meta', JSON_OBJECT(
        'title', '我们的纪念',
        'subtitle', '把时间折成书页，把日子存成风景',
        'background', 'linen-sand'
    ),
    'blocks', JSON_ARRAY(
        JSON_OBJECT(
            'id', 'cover-hero',
            'type', 'image',
            'src', @cover_hero_url,
            'fit', 'cover',
            'alt', '封面主图'
        ),
        JSON_OBJECT(
            'id', 'cover-title',
            'type', 'text',
            'text', '我们的纪念',
            'style', JSON_OBJECT('fontSize', 56, 'fontWeight', 700, 'letterSpacing', 6)
        ),
        JSON_OBJECT(
            'id', 'cover-subtitle',
            'type', 'text',
            'text', '把时间折成书页，把日子存成风景',
            'style', JSON_OBJECT('fontSize', 24, 'fontWeight', 400, 'opacity', 0.88)
        )
    )
) AS CHAR CHARACTER SET utf8mb4);

SET @page_002_json = CAST(JSON_OBJECT(
    'schemaVersion', '2.0',
    'pageNo', 2,
    'pageType', 'title',
    'theme', 'album-inline-json',
    'meta', JSON_OBJECT('align', 'center'),
    'blocks', JSON_ARRAY(
        JSON_OBJECT(
            'id', 'title-eyebrow',
            'type', 'text',
            'text', 'BALBUM PUBLIC PREVIEW',
            'style', JSON_OBJECT('fontSize', 14, 'letterSpacing', 8, 'opacity', 0.65)
        ),
        JSON_OBJECT(
            'id', 'title-main',
            'type', 'text',
            'text', '相册预览 · 内联 JSON 样例',
            'style', JSON_OBJECT('fontSize', 42, 'fontWeight', 700)
        ),
        JSON_OBJECT(
            'id', 'title-desc',
            'type', 'text',
            'text', '当前页的 jsonProxyUrl 不再是文件地址，而是可直接渲染的页面 JSON 字符串。',
            'style', JSON_OBJECT('fontSize', 18, 'lineHeight', 1.8)
        )
    )
) AS CHAR CHARACTER SET utf8mb4);

SET @page_003_json = CAST(JSON_OBJECT(
    'schemaVersion', '2.0',
    'pageNo', 3,
    'pageType', 'preface',
    'theme', 'album-inline-json',
    'blocks', JSON_ARRAY(
        JSON_OBJECT(
            'id', 'preface-title',
            'type', 'text',
            'text', '序言',
            'style', JSON_OBJECT('fontSize', 36, 'fontWeight', 600)
        ),
        JSON_OBJECT(
            'id', 'preface-p1',
            'type', 'paragraph',
            'text', '这份测试数据用于验证 GetPreviewAsync 在 pages 数组里直接返回前端 JSON 数据源，而不是再返回 JSON 文件代理地址。'
        ),
        JSON_OBJECT(
            'id', 'preface-p2',
            'type', 'paragraph',
            'text', '为了兼容旧数据，服务仍然允许从历史 json_file_id 回退读取；但新写入链路已经改成直接把 JSON 长文本存到 book_project_version_page.json_source。'
        ),
        JSON_OBJECT(
            'id', 'preface-p3',
            'type', 'paragraph',
            'text', '这样前台拿到 pages 后，无需额外发一次 JSON 文件请求，就可以直接进行页面结构解析和首屏渲染。'
        )
    )
) AS CHAR CHARACTER SET utf8mb4);

SET @page_004_json = CAST(JSON_OBJECT(
    'schemaVersion', '2.0',
    'pageNo', 4,
    'pageType', 'content',
    'theme', 'album-inline-json',
    'layout', JSON_OBJECT('template', 'image-left-story-right'),
    'blocks', JSON_ARRAY(
        JSON_OBJECT(
            'id', 'content-image',
            'type', 'image',
            'src', @content_hero_url,
            'fit', 'contain',
            'alt', '正文配图'
        ),
        JSON_OBJECT(
            'id', 'content-title',
            'type', 'text',
            'text', '湖边的黄昏',
            'style', JSON_OBJECT('fontSize', 30, 'fontWeight', 700)
        ),
        JSON_OBJECT(
            'id', 'content-body',
            'type', 'richText',
            'content', JSON_ARRAY(
                JSON_OBJECT('type', 'paragraph', 'text', '晚风把树影吹成了慢镜头，原本普通的一天，因为一句随手的感叹，忽然变得值得收藏。'),
                JSON_OBJECT('type', 'paragraph', 'text', '新的测试数据保留了图片文件走本地文件系统代理，但页面 JSON 本身改成了数据库内联长文本，便于预览接口一次性返回。'),
                JSON_OBJECT('type', 'quote', 'text', '把会忘记的日常，做成能翻阅的纪念。')
            )
        )
    )
) AS CHAR CHARACTER SET utf8mb4);

DELETE FROM book_project_view_log
WHERE project_id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id);

DELETE FROM book_project_share_log
WHERE project_id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id);

DELETE FROM book_project_version_page_asset
WHERE version_page_id IN (
    SELECT id FROM (
        SELECT page.id
        FROM book_project_version_page page
        INNER JOIN book_project_version version ON version.id = page.project_version_id
        WHERE version.project_id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id)
    ) AS page_ids
);

DELETE FROM book_project_version_page
WHERE project_version_id IN (
    SELECT id FROM (
        SELECT id
        FROM book_project_version
        WHERE project_id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id)
    ) AS version_ids
);

UPDATE book_project
SET shared_version_id = NULL
WHERE id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id);

DELETE FROM book_project_version
WHERE project_id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id);

DELETE FROM book_project
WHERE id IN (@sample_project_id, @sample_list_public_project_id, @sample_list_private_project_id, @sample_other_user_project_id);

INSERT INTO identity_user (
    id,
    user_no,
    phone,
    password_hash,
    status,
    registered_at,
    last_login_at,
    created_at,
    updated_at)
VALUES (
    @sample_user_id,
    'U9001',
    NULL,
    NULL,
    1,
    '2026-04-20 10:00:00',
    '2026-04-20 10:00:00',
    '2026-04-20 10:00:00',
    '2026-04-20 10:00:00'
) ON DUPLICATE KEY UPDATE
    user_no = VALUES(user_no),
    phone = VALUES(phone),
    password_hash = VALUES(password_hash),
    status = VALUES(status),
    registered_at = VALUES(registered_at),
    last_login_at = VALUES(last_login_at),
    updated_at = VALUES(updated_at);

INSERT INTO identity_user (
    id,
    user_no,
    phone,
    password_hash,
    status,
    registered_at,
    last_login_at,
    created_at,
    updated_at)
VALUES (
    @sample_other_user_id,
    'U9002',
    NULL,
    NULL,
    1,
    '2026-05-01 09:00:00',
    '2026-05-31 08:00:00',
    '2026-05-01 09:00:00',
    '2026-05-31 08:00:00'
) ON DUPLICATE KEY UPDATE
    user_no = VALUES(user_no),
    phone = VALUES(phone),
    password_hash = VALUES(password_hash),
    status = VALUES(status),
    registered_at = VALUES(registered_at),
    last_login_at = VALUES(last_login_at),
    updated_at = VALUES(updated_at);

INSERT INTO identity_user_profile (
    user_id,
    nickname,
    avatar_url,
    gender,
    city,
    bio,
    extra,
    created_at,
    updated_at)
VALUES (
    @sample_user_id,
    '相册样例用户',
    NULL,
    0,
    '杭州',
    'Public album preview sample owner',
    NULL,
    '2026-04-20 10:00:00',
    '2026-04-20 10:00:00'
) ON DUPLICATE KEY UPDATE
    nickname = VALUES(nickname),
    avatar_url = VALUES(avatar_url),
    gender = VALUES(gender),
    city = VALUES(city),
    bio = VALUES(bio),
    extra = VALUES(extra),
    updated_at = VALUES(updated_at);

INSERT INTO identity_user_profile (
    user_id,
    nickname,
    avatar_url,
    gender,
    city,
    bio,
    extra,
    created_at,
    updated_at)
VALUES (
    @sample_other_user_id,
    '列表隔离样例用户',
    NULL,
    0,
    '上海',
    'Used to verify album list ownership filtering',
    NULL,
    '2026-05-01 09:00:00',
    '2026-05-31 08:00:00'
) ON DUPLICATE KEY UPDATE
    nickname = VALUES(nickname),
    avatar_url = VALUES(avatar_url),
    gender = VALUES(gender),
    city = VALUES(city),
    bio = VALUES(bio),
    extra = VALUES(extra),
    updated_at = VALUES(updated_at);

INSERT INTO book_project (
    id,
    user_id,
    book_type,
    title,
    subtitle,
    status,
    spu_id,
    page_count,
    image_count,
    is_gift,
    gift_target_name,
    created_at,
    updated_at,
    share_code,
    is_public,
    shared_version_id,
    shared_at,
    view_count,
    share_count,
    extra_properties_json)
VALUES (
    @sample_project_id,
    @sample_user_id,
    'balbum',
    '我们的纪念',
    '内联 JSON 预览数据样例',
    4,
    (SELECT id FROM catalog_product_spu WHERE spu_code = 'balbum' AND is_active = 1 LIMIT 1),
    4,
    2,
    0,
    NULL,
    '2026-04-20 10:00:00',
    '2026-04-20 10:00:00',
    @sample_share_code,
    1,
    NULL,
    '2026-04-20 10:00:00',
    0,
    0,
    JSON_ARRAY('cover-required', 'story-starter', 'travel-memory'));

INSERT INTO book_project_version (
    id,
    project_id,
    version_no,
    snapshot_data,
    page_count,
    image_count,
    render_version,
    frozen_at,
    created_at)
VALUES (
    @sample_version_id,
    @sample_project_id,
    1,
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'shareCode', @sample_share_code,
        'pageCount', 4,
        'pages', JSON_ARRAY(
            JSON_OBJECT('pageNo', 1, 'pageLabel', '封面', 'pageType', 'cover'),
            JSON_OBJECT('pageNo', 2, 'pageLabel', '扉页', 'pageType', 'title'),
            JSON_OBJECT('pageNo', 3, 'pageLabel', '序言', 'pageType', 'preface'),
            JSON_OBJECT('pageNo', 4, 'pageLabel', '1', 'pageType', 'content')
        )
    ),
    4,
    2,
    'preview-sample-2.0',
    '2026-04-20 10:05:00',
    '2026-04-20 10:05:00'
);

UPDATE book_project
SET shared_version_id = @sample_version_id,
    updated_at = '2026-04-20 10:05:00'
WHERE id = @sample_project_id;

INSERT INTO book_project_version_page (
    id,
    project_version_id,
    page_no,
    page_label,
    page_type,
    sort_order,
    json_source,
    json_file_id,
    json_bucket,
    json_object_key,
    html_file_id,
    html_bucket,
    html_object_key,
    thumbnail_file_id,
    thumbnail_bucket,
    thumbnail_object_key,
    page_width,
    page_height,
    schema_version,
    created_at,
    updated_at)
VALUES (
    90010101,
    @sample_version_id,
    1,
    '封面',
    'cover',
    1,
    @page_001_json,
    NULL,
    NULL,
    NULL,
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @cover_html_object_key LIMIT 1),
    @sample_bucket,
    @cover_html_object_key,
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @cover_hero_object_key LIMIT 1),
    @sample_bucket,
    @cover_hero_object_key,
    1200,
    1600,
    '2.0',
    '2026-04-20 10:05:00',
    '2026-04-20 10:05:00'
);

INSERT INTO book_project_version_page (
    id,
    project_version_id,
    page_no,
    page_label,
    page_type,
    sort_order,
    json_source,
    json_file_id,
    json_bucket,
    json_object_key,
    html_file_id,
    html_bucket,
    html_object_key,
    thumbnail_file_id,
    thumbnail_bucket,
    thumbnail_object_key,
    page_width,
    page_height,
    schema_version,
    created_at,
    updated_at)
VALUES (
    90010102,
    @sample_version_id,
    2,
    '扉页',
    'title',
    2,
    @page_002_json,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    1200,
    1600,
    '2.0',
    '2026-04-20 10:05:00',
    '2026-04-20 10:05:00'
);

INSERT INTO book_project_version_page (
    id,
    project_version_id,
    page_no,
    page_label,
    page_type,
    sort_order,
    json_source,
    json_file_id,
    json_bucket,
    json_object_key,
    html_file_id,
    html_bucket,
    html_object_key,
    thumbnail_file_id,
    thumbnail_bucket,
    thumbnail_object_key,
    page_width,
    page_height,
    schema_version,
    created_at,
    updated_at)
VALUES (
    90010103,
    @sample_version_id,
    3,
    '序言',
    'preface',
    3,
    @page_003_json,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    1200,
    1600,
    '2.0',
    '2026-04-20 10:05:00',
    '2026-04-20 10:05:00'
);

INSERT INTO book_project_version_page (
    id,
    project_version_id,
    page_no,
    page_label,
    page_type,
    sort_order,
    json_source,
    json_file_id,
    json_bucket,
    json_object_key,
    html_file_id,
    html_bucket,
    html_object_key,
    thumbnail_file_id,
    thumbnail_bucket,
    thumbnail_object_key,
    page_width,
    page_height,
    schema_version,
    created_at,
    updated_at)
VALUES (
    90010104,
    @sample_version_id,
    4,
    '1',
    'content',
    4,
    @page_004_json,
    NULL,
    NULL,
    NULL,
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @content_html_object_key LIMIT 1),
    @sample_bucket,
    @content_html_object_key,
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @content_hero_object_key LIMIT 1),
    @sample_bucket,
    @content_hero_object_key,
    1200,
    1600,
    '2.0',
    '2026-04-20 10:05:00',
    '2026-04-20 10:05:00'
);

INSERT INTO book_project_version_page_asset (
    id,
    version_page_id,
    sort_order,
    role,
    file_id,
    bucket_name,
    object_key,
    width,
    height,
    alt_text,
    caption,
    crop_json,
    created_at)
VALUES (
    9001010401,
    90010101,
    1,
    'hero',
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @cover_hero_object_key LIMIT 1),
    @sample_bucket,
    @cover_hero_object_key,
    1200,
    1600,
    '封面主图',
    'Cover hero image',
    NULL,
    '2026-04-20 10:05:00'
);

INSERT INTO book_project_version_page_asset (
    id,
    version_page_id,
    sort_order,
    role,
    file_id,
    bucket_name,
    object_key,
    width,
    height,
    alt_text,
    caption,
    crop_json,
    created_at)
VALUES (
    9001010402,
    90010104,
    1,
    'hero',
    (SELECT id FROM storage_file_object WHERE bucket_name = @sample_bucket AND object_key = @content_hero_object_key LIMIT 1),
    @sample_bucket,
    @content_hero_object_key,
    1200,
    900,
    '正文配图',
    'A quiet evening by the lake',
    NULL,
    '2026-04-20 10:05:00'
);

INSERT INTO book_project_view_log (
    project_id,
    share_code,
    page_no,
    client_ip,
    client_user_agent,
    referrer,
    created_at)
VALUES
    (@sample_project_id, @sample_share_code, 1, '127.0.0.1', 'sample-seed', NULL, '2026-04-20 10:06:00'),
    (@sample_project_id, @sample_share_code, 4, '127.0.0.1', 'sample-seed', NULL, '2026-04-20 10:06:30');

INSERT INTO book_project_share_log (
    project_id,
    share_code,
    channel,
    client_ip,
    client_user_agent,
    created_at)
VALUES
    (@sample_project_id, @sample_share_code, 'link', '127.0.0.1', 'sample-seed', '2026-04-20 10:07:00');

UPDATE book_project
SET view_count = 2,
    share_count = 1,
    updated_at = '2026-04-20 10:07:00'
WHERE id = @sample_project_id;

-- Additional sample albums for GET /api/albums list self-test
INSERT INTO book_project (
    id,
    user_id,
    book_type,
    title,
    subtitle,
    status,
    spu_id,
    page_count,
    image_count,
    is_gift,
    gift_target_name,
    created_at,
    updated_at,
    share_code,
    is_public,
    shared_version_id,
    shared_at,
    view_count,
    share_count,
    extra_properties_json)
VALUES
(
    @sample_list_public_project_id,
    @sample_user_id,
    'balbum',
    '五月旅行手记',
    '公开列表样例',
    4,
    (SELECT id FROM catalog_product_spu WHERE spu_code = 'balbum' AND is_active = 1 LIMIT 1),
    18,
    26,
    0,
    NULL,
    '2026-05-21 14:00:00',
    '2026-05-31 09:30:00',
    @sample_list_public_share_code,
    1,
    NULL,
    '2026-05-31 09:30:00',
    35,
    6,
    JSON_ARRAY('travel-memory', 'public-showcase')
),
(
    @sample_list_private_project_id,
    @sample_user_id,
    'balbum',
    '未公开草稿集',
    '私有列表样例',
    2,
    (SELECT id FROM catalog_product_spu WHERE spu_code = 'balbum' AND is_active = 1 LIMIT 1),
    9,
    12,
    0,
    NULL,
    '2026-05-30 18:20:00',
    '2026-05-31 10:10:00',
    NULL,
    0,
    NULL,
    NULL,
    0,
    0,
    JSON_ARRAY('draft', 'needs-cover-photo')
),
(
    @sample_other_user_project_id,
    @sample_other_user_id,
    'balbum',
    '别人的相册',
    '用于校验用户隔离',
    4,
    (SELECT id FROM catalog_product_spu WHERE spu_code = 'balbum' AND is_active = 1 LIMIT 1),
    12,
    15,
    0,
    NULL,
    '2026-05-28 08:00:00',
    '2026-05-31 08:30:00',
    @sample_other_user_share_code,
    1,
    NULL,
    '2026-05-31 08:30:00',
    9,
    1,
    JSON_ARRAY('family-memory')
);
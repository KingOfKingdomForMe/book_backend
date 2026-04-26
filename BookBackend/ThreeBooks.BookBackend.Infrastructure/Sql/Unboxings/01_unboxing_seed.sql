-- Preconditions:
--   1. Apply PlanDocument/weixinshu-db-design/sql/01_identity.sql
--   2. Apply PlanDocument/weixinshu-db-design/sql/06_community.sql

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
VALUES
    (9201, 'U9201', NULL, NULL, 1, '2026-04-01 09:00:00', '2026-04-01 09:00:00', '2026-04-01 09:00:00', '2026-04-01 09:00:00')
ON DUPLICATE KEY UPDATE
    user_no = VALUES(user_no),
    status = VALUES(status),
    last_login_at = VALUES(last_login_at),
    updated_at = VALUES(updated_at);

INSERT INTO community_unboxing_level (level_code, level_name, icon_url, sort_order)
VALUES
    ('silver', '白银晒单', 'https://placehold.co/64x64/png?text=S', 10),
    ('gold', '黄金晒单', 'https://placehold.co/64x64/png?text=G', 20),
    ('diamond', '钻石晒单', 'https://placehold.co/64x64/png?text=D', 30)
ON DUPLICATE KEY UPDATE
    level_name = VALUES(level_name),
    icon_url = VALUES(icon_url),
    sort_order = VALUES(sort_order);

DELETE FROM community_unboxing_media
WHERE post_id IN (
    SELECT id FROM (
        SELECT id FROM community_unboxing_post WHERE post_no IN ('39415', '39411', '39408', '39400')
    ) AS post_ids
);

DELETE FROM community_unboxing_tag
WHERE post_id IN (
    SELECT id FROM (
        SELECT id FROM community_unboxing_post WHERE post_no IN ('39415', '39411', '39408', '39400')
    ) AS post_ids
);

INSERT INTO community_unboxing_post (
    post_no,
    user_id,
    author_name,
    author_avatar,
    order_item_id,
    project_version_id,
    product_label,
    book_title,
    level_id,
    title,
    content_text,
    status,
    is_featured,
    published_at)
VALUES
(
    '39415',
    9201,
    '示例书友A',
    'https://placehold.co/128x128/png?text=A',
    NULL,
    NULL,
    '微信书 A5瀑布流 文艺装',
    '春日心书',
    (SELECT id FROM community_unboxing_level WHERE level_code = 'diamond' LIMIT 1),
    '春日心书',
    '把一年里值得回望的生活片段整理成一本书，再次翻阅时，纸页会让普通日子重新变得有重量。',
    1,
    1,
    '2026-04-13 10:30:00'
),
(
    '39411',
    9201,
    '示例书友B',
    'https://placehold.co/128x128/png?text=B',
    NULL,
    NULL,
    '长文集 文艺装',
    '四月散文集',
    (SELECT id FROM community_unboxing_level WHERE level_code = 'gold' LIMIT 1),
    '四月散文集',
    '长文被装订成册之后，内容的节奏、停顿和留白都更清晰，适合沉浸式翻阅。',
    1,
    0,
    '2026-04-12 18:20:00'
),
(
    '39408',
    9201,
    '示例书友C',
    'https://placehold.co/128x128/png?text=C',
    NULL,
    NULL,
    '文绘集 A4精装',
    '旅行手记',
    (SELECT id FROM community_unboxing_level WHERE level_code = 'silver' LIMIT 1),
    '旅行手记',
    '照片、标题和简短注释被编排成一体之后，旅行记忆更像一本真正可以收藏的作品。',
    1,
    0,
    '2026-04-10 15:40:00'
),
(
    '39400',
    9201,
    '示例书友D',
    'https://placehold.co/128x128/png?text=D',
    NULL,
    NULL,
    '微信书 B6 轻装',
    '待发布样书',
    (SELECT id FROM community_unboxing_level WHERE level_code = 'silver' LIMIT 1),
    '待发布样书',
    '这是一条用于本地后台调试的待审核晒单，默认不会出现在公开列表。',
    0,
    0,
    NULL
)
ON DUPLICATE KEY UPDATE
    user_id = VALUES(user_id),
    author_name = VALUES(author_name),
    author_avatar = VALUES(author_avatar),
    product_label = VALUES(product_label),
    book_title = VALUES(book_title),
    level_id = VALUES(level_id),
    title = VALUES(title),
    content_text = VALUES(content_text),
    status = VALUES(status),
    is_featured = VALUES(is_featured),
    published_at = VALUES(published_at),
    updated_at = CURRENT_TIMESTAMP;

INSERT INTO community_unboxing_media (
    post_id,
    media_type,
    storage_url,
    thumbnail_url,
    sort_order,
    width,
    height)
VALUES
((SELECT id FROM community_unboxing_post WHERE post_no = '39415' LIMIT 1), 'image', 'https://placehold.co/960x720/png?text=Unboxing+39415-1', 'https://placehold.co/320x240/png?text=39415-1', 1, 960, 720),
((SELECT id FROM community_unboxing_post WHERE post_no = '39415' LIMIT 1), 'image', 'https://placehold.co/960x720/png?text=Unboxing+39415-2', 'https://placehold.co/320x240/png?text=39415-2', 2, 960, 720),
((SELECT id FROM community_unboxing_post WHERE post_no = '39411' LIMIT 1), 'image', 'https://placehold.co/960x720/png?text=Unboxing+39411', 'https://placehold.co/320x240/png?text=39411', 1, 960, 720),
((SELECT id FROM community_unboxing_post WHERE post_no = '39408' LIMIT 1), 'image', 'https://placehold.co/960x720/png?text=Unboxing+39408', 'https://placehold.co/320x240/png?text=39408', 1, 960, 720),
((SELECT id FROM community_unboxing_post WHERE post_no = '39400' LIMIT 1), 'image', 'https://placehold.co/960x720/png?text=Unboxing+39400', 'https://placehold.co/320x240/png?text=39400', 1, 960, 720);

INSERT INTO community_unboxing_tag (post_id, tag_name)
VALUES
((SELECT id FROM community_unboxing_post WHERE post_no = '39415' LIMIT 1), '微信书'),
((SELECT id FROM community_unboxing_post WHERE post_no = '39415' LIMIT 1), '生活记录'),
((SELECT id FROM community_unboxing_post WHERE post_no = '39411' LIMIT 1), '长文集'),
((SELECT id FROM community_unboxing_post WHERE post_no = '39411' LIMIT 1), '写作'),
((SELECT id FROM community_unboxing_post WHERE post_no = '39408' LIMIT 1), '文绘集'),
((SELECT id FROM community_unboxing_post WHERE post_no = '39408' LIMIT 1), '旅行'),
((SELECT id FROM community_unboxing_post WHERE post_no = '39400' LIMIT 1), '后台调试');
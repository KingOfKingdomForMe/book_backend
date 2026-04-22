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
    'balbum-content-minimal',
    '文绘集极简正文',
    '适合长文和少量配图的留白正文模板。',
    'balbum',
    'content',
    'minimal',
    'paper-journal',
    '2.0',
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'pageType', 'content',
        'theme', 'paper-journal',
        'layout', JSON_OBJECT('template', 'text-primary-image-secondary'),
        'blocks', JSON_ARRAY(
            JSON_OBJECT('id', 'title', 'type', 'text', 'text', '在这里输入标题', 'style', JSON_OBJECT('fontSize', 30, 'fontWeight', 700)),
            JSON_OBJECT('id', 'summary', 'type', 'text', 'text', '一句导语，概括这一页想表达的情绪。', 'style', JSON_OBJECT('fontSize', 18, 'opacity', 0.72)),
            JSON_OBJECT('id', 'body', 'type', 'richText', 'content', JSON_ARRAY(
                JSON_OBJECT('type', 'paragraph', 'text', '这是一段模板正文。前端获取到 jsonSource 后，可以直接替换为用户自己的内容。'),
                JSON_OBJECT('type', 'paragraph', 'text', '如果需要图片，可把 image block 的 src 改成用户上传后的文件地址。')
            )),
            JSON_OBJECT('id', 'image', 'type', 'image', 'src', '', 'alt', '示意图', 'fit', 'contain')
        )
    ),
    NULL,
    NULL,
    1,
    1,
    10
),
(
    'balbum-content-magazine',
    '文绘集杂志正文',
    '更偏向图文并排和标题强调的正文模板。',
    'balbum',
    'content',
    'magazine',
    'editorial-grid',
    '2.0',
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'pageType', 'content',
        'theme', 'editorial-grid',
        'layout', JSON_OBJECT('template', 'editorial-split'),
        'blocks', JSON_ARRAY(
            JSON_OBJECT('id', 'eyebrow', 'type', 'text', 'text', 'SECTION LABEL', 'style', JSON_OBJECT('fontSize', 12, 'letterSpacing', 6, 'opacity', 0.56)),
            JSON_OBJECT('id', 'headline', 'type', 'text', 'text', '在这里输入更有冲击力的标题', 'style', JSON_OBJECT('fontSize', 34, 'fontWeight', 800)),
            JSON_OBJECT('id', 'image', 'type', 'image', 'src', '', 'alt', '模板图片', 'fit', 'cover'),
            JSON_OBJECT('id', 'body', 'type', 'richText', 'content', JSON_ARRAY(
                JSON_OBJECT('type', 'paragraph', 'text', '适合需要更强版式感的内容页。'),
                JSON_OBJECT('type', 'quote', 'text', '把模板当起点，而不是当终点。')
            ))
        )
    ),
    NULL,
    NULL,
    1,
    1,
    20
),
(
    'balbum-cover-story',
    '文绘集故事封面',
    '适合作品首页的封面模板。',
    'balbum',
    'cover',
    'story',
    'story-cover',
    '2.0',
    JSON_OBJECT(
        'schemaVersion', '2.0',
        'pageType', 'cover',
        'theme', 'story-cover',
        'blocks', JSON_ARRAY(
            JSON_OBJECT('id', 'cover-image', 'type', 'image', 'src', '', 'alt', '封面图', 'fit', 'cover'),
            JSON_OBJECT('id', 'cover-title', 'type', 'text', 'text', '在这里输入相册标题', 'style', JSON_OBJECT('fontSize', 40, 'fontWeight', 700, 'letterSpacing', 4)),
            JSON_OBJECT('id', 'cover-subtitle', 'type', 'text', 'text', '在这里输入封面副标题', 'style', JSON_OBJECT('fontSize', 18, 'opacity', 0.8))
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
-- ============================================================
-- 心书 商品目录域 — 种子数据 (基于 weixinshu.com 公开页面推断)
-- ============================================================

-- ----- 1. 规格维度 -----
INSERT INTO catalog_spec_definition (id, spec_key, spec_label, sort_order) VALUES
(1, 'size',    '尺寸',     1),
(2, 'binding', '装订方式', 2),
(3, 'layout',  '版式',     3);

-- ----- 2. 规格值 -----
-- 尺寸
INSERT INTO catalog_spec_value (id, spec_id, value_code, value_label, sort_order) VALUES
(1,  1, 'A5', 'A5 (14.8×21)',  1),
(2,  1, 'A4', 'A4 (21×28.5)',  2);

-- 装帧
INSERT INTO catalog_spec_value (id, spec_id, value_code, value_label, sort_order) VALUES
(10, 2, 'economy',   '经济装', 1),
(11, 2, 'literary',  '文艺装', 2),
(12, 2, 'hardcover', '精装',   3),
(13, 2, 'collector', '典藏装', 4);

-- 版式
INSERT INTO catalog_spec_value (id, spec_id, value_code, value_label, sort_order) VALUES
(20, 3, 'waterfall',  'A5 瀑布流',  1),
(21, 3, 'double_col', 'A5 双栏',    2),
(22, 3, 'shiguang',   'A5 拾光',    3),
(23, 3, 'magazine',   'A4 杂志版',  4),
(24, 3, 'a4_waterfall','A4 瀑布流', 5),
(25, 3, 'single_page','A5 单页',    6);

-- ----- 3. 商品分类 -----
INSERT INTO catalog_category (id, name, slug, sort_order) VALUES
(1, '图文书',  'text-photo-book', 1),
(2, '照片书',  'photo-book',      2),
(3, '照片冲印','photo-print',     3),
(4, '摆台',    'frame',           4);

-- ----- 4. 商品 SPU -----
INSERT INTO catalog_product_spu (id, category_id, spu_code, name, subtitle, content_source, sort_order) VALUES
(1,  1, 'wxbook',       '微信书',           '朋友圈5分钟自动成书',                'wechat',  1),
(2,  1, 'wbbook',       '微博书',           '微博5分钟自动成书',                  'weibo',   2),
(3,  1, 'diary',        '日记书',           '用照片记录生活',                     'diary',   3),
(4,  1, 'blogbook',     '长文集',           '社交长文5分钟自动成书',              'manual',  4),
(5,  1, 'dalbum',       'B5布面书',         '专属图文定制书',                     'manual',  5),
(6,  2, 'album',        'A5精装纪念册',     '经典蝴蝶对裱装',                    'any',     6),
(7,  2, 'b5album',      '锁线精装照片书',   '首款锁线精装照片书',                'any',     7),
(8,  2, 'xalbum',       'A4精装纪念册',     '更大更精致的阅读体验',              'any',     8),
(9,  2, 'xcalbum',      'A4轻奢杂志册',     '大气杂志画册',                      'any',     9),
(10, 2, 'balbum',       '文绘集',           '图文最佳拍档',                      'any',    10),
(11, 2, 'square-album', '蝴蝶写真集',       '影楼级写真集',                      'any',    11),
(12, 2, 'fabric-album', '横款照片书',       '海量照片收纳能力',                  'any',    12),
(13, 3, 'photo',        '照片冲印',         '6寸照片冲印',                       'any',    13),
(14, 4, 'frame-12in',   '12寸创意摆台',     '12寸创意摆台',                      'any',    14),
(15, 4, 'frame-8in',    '8寸水晶摆台',      '8寸水晶摆台',                       'any',    15);

-- ----- 5. 版式模板 -----
INSERT INTO catalog_layout_template (id, layout_code, name, description, applicable_sizes, sort_order) VALUES
(1, 'a4_magazine',   'A4 杂志版',  '照片更大，放大你的美',          'A4',    1),
(2, 'a4_waterfall',  'A4 瀑布流',  '细节更多，图文排版堪比写真',    'A4',    2),
(3, 'a5_waterfall',  'A5 瀑布流',  '图文环绕，排版有呼吸',          'A5',    3),
(4, 'a5_double_col', 'A5 双栏',    '照片定宽，排版紧凑',            'A5',    4),
(5, 'a5_shiguang',   'A5 拾光',    '单独分页，灵活错落',            'A5',    5),
(6, 'blogbook_a5',   '长文集版式', '目录+正文图文版式',             'A5,A4', 6);

-- ----- 6. 封面模板 (根据晒单中出现的封面系列推断) -----
INSERT INTO catalog_cover_template (id, template_code, name, category, applicable_spus, sort_order) VALUES
(1,  'animal_city_a5',  'A5动物城',  'animal_city', 'wxbook,diary',           1),
(2,  'animal_city_a4',  'A4动物城',  'animal_city', 'wxbook,diary',           2),
(3,  'nostalgia_a5',    'A5怀旧',    'nostalgia',   'wxbook,wbbook,diary',    3),
(4,  'magazine_a4',     'A4杂志',    'magazine',    'wxbook,wbbook',          4),
(5,  'childlike_a5',    'A5童趣',    'childlike',   'wxbook,diary',           5),
(6,  'nezha_a5',        'A5哪吒',    'nezha',       'wxbook,diary',           6),
(7,  'waterfall_a5',    'A5瀑布流',  'waterfall',   'wxbook,wbbook,diary',    7),
(8,  'waterfall_a4',    'A4瀑布流',  'waterfall',   'wxbook,wbbook',          8),
(9,  'double_col_a5',   'A5双栏',    'double_col',  'wxbook,wbbook,blogbook', 9),
(10, 'double_col_a4',   'A4双栏',    'double_col',  'wxbook,wbbook',         10),
(11, 'literary_blogbook','长文集文艺','literary',    'blogbook',              11),
(12, 'custom_upload',   '自定义封面', 'custom',      NULL,                    99);

-- ----- 7. 商品 SKU (微信书主力 SKU) -----
-- 微信书: 2尺寸 × 4装帧 = 8 SKU
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages) VALUES
(1,  1, 'wxbook-a5-economy',   1, 10, NULL, 60),
(2,  1, 'wxbook-a5-literary',  1, 11, NULL, 60),
(3,  1, 'wxbook-a5-hardcover', 1, 12, NULL, 60),
(4,  1, 'wxbook-a5-collector', 1, 13, NULL, 60),
(5,  1, 'wxbook-a4-economy',   2, 10, NULL, 60),
(6,  1, 'wxbook-a4-literary',  2, 11, NULL, 60),
(7,  1, 'wxbook-a4-hardcover', 2, 12, NULL, 60),
(8,  1, 'wxbook-a4-collector', 2, 13, NULL, 60);

-- 微博书: 同结构
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages) VALUES
(11, 2, 'wbbook-a5-economy',   1, 10, NULL, 60),
(12, 2, 'wbbook-a5-literary',  1, 11, NULL, 60),
(13, 2, 'wbbook-a5-hardcover', 1, 12, NULL, 60),
(14, 2, 'wbbook-a5-collector', 1, 13, NULL, 60),
(15, 2, 'wbbook-a4-economy',   2, 10, NULL, 60),
(16, 2, 'wbbook-a4-literary',  2, 11, NULL, 60),
(17, 2, 'wbbook-a4-hardcover', 2, 12, NULL, 60),
(18, 2, 'wbbook-a4-collector', 2, 13, NULL, 60);

-- 日记书
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages) VALUES
(21, 3, 'diary-a5-economy',   1, 10, NULL, 60),
(22, 3, 'diary-a5-literary',  1, 11, NULL, 60),
(23, 3, 'diary-a5-hardcover', 1, 12, NULL, 60),
(24, 3, 'diary-a5-collector', 1, 13, NULL, 60),
(25, 3, 'diary-a4-economy',   2, 10, NULL, 60),
(26, 3, 'diary-a4-literary',  2, 11, NULL, 60),
(27, 3, 'diary-a4-hardcover', 2, 12, NULL, 60),
(28, 3, 'diary-a4-collector', 2, 13, NULL, 60);

-- 长文集
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages) VALUES
(31, 4, 'blogbook-a5-economy',   1, 10, NULL, 60),
(32, 4, 'blogbook-a5-literary',  1, 11, NULL, 60),
(33, 4, 'blogbook-a5-hardcover', 1, 12, NULL, 60),
(34, 4, 'blogbook-a5-collector', 1, 13, NULL, 60),
(35, 4, 'blogbook-a4-economy',   2, 10, NULL, 60),
(36, 4, 'blogbook-a4-literary',  2, 11, NULL, 60),
(37, 4, 'blogbook-a4-hardcover', 2, 12, NULL, 60),
(38, 4, 'blogbook-a4-collector', 2, 13, NULL, 60);

-- 照片书类 (纪念册等, 通常固定页数范围)
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages, max_pages) VALUES
(41, 6,  'album-a5-hardcover',     1, 12, NULL, 20, 150),
(42, 8,  'xalbum-a4-hardcover',    2, 12, NULL, 20, 150),
(43, 9,  'xcalbum-a4-softcover',   2, 10, NULL, 24,  48),
(44, 7,  'b5album-hardcover',      1, 12, NULL, 20, 120),
(45, 10, 'balbum-a4-hardcover',    2, 12, NULL, 24,  60),
(46, 11, 'square-album-hardcover', NULL, 12, NULL, 20, 60),
(47, 12, 'fabric-album-softcover', NULL, 10, NULL, 24, 120);

-- 照片冲印
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages) VALUES
(51, 13, 'photo-6in', NULL, NULL, NULL, 1);

-- 摆台
INSERT INTO catalog_product_sku (id, spu_id, sku_code, size_value_id, binding_value_id, layout_value_id, min_pages) VALUES
(61, 14, 'frame-12in-set', NULL, NULL, NULL, 1),
(62, 15, 'frame-8in-crystal', NULL, NULL, NULL, 1);

-- ----- 8. 价格规则 (基于产品页展示推断, 仅供参考) -----
-- 微信书 (所有尺寸/装帧统一展示: 基础价 ¥18 + ¥0.70/页)
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(1,  1,  18.00, 0.70, '2024-01-01', 1),
(2,  2,  18.00, 0.70, '2024-01-01', 1),
(3,  3,  18.00, 0.70, '2024-01-01', 1),
(4,  4,  18.00, 0.70, '2024-01-01', 1),
(5,  5,  18.00, 0.70, '2024-01-01', 1),
(6,  6,  18.00, 0.70, '2024-01-01', 1),
(7,  7,  18.00, 0.70, '2024-01-01', 1),
(8,  8,  18.00, 0.70, '2024-01-01', 1);

-- 微博书 (同微信书定价)
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(11, 11, 18.00, 0.70, '2024-01-01', 1),
(12, 12, 18.00, 0.70, '2024-01-01', 1),
(13, 13, 18.00, 0.70, '2024-01-01', 1),
(14, 14, 18.00, 0.70, '2024-01-01', 1),
(15, 15, 18.00, 0.70, '2024-01-01', 1),
(16, 16, 18.00, 0.70, '2024-01-01', 1),
(17, 17, 18.00, 0.70, '2024-01-01', 1),
(18, 18, 18.00, 0.70, '2024-01-01', 1);

-- 日记书 (同结构)
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(21, 21, 18.00, 0.70, '2024-01-01', 1),
(22, 22, 18.00, 0.70, '2024-01-01', 1),
(23, 23, 18.00, 0.70, '2024-01-01', 1),
(24, 24, 18.00, 0.70, '2024-01-01', 1),
(25, 25, 18.00, 0.70, '2024-01-01', 1),
(26, 26, 18.00, 0.70, '2024-01-01', 1),
(27, 27, 18.00, 0.70, '2024-01-01', 1),
(28, 28, 18.00, 0.70, '2024-01-01', 1);

-- 长文集 (同结构)
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(31, 31, 18.00, 0.70, '2024-01-01', 1),
(32, 32, 18.00, 0.70, '2024-01-01', 1),
(33, 33, 18.00, 0.70, '2024-01-01', 1),
(34, 34, 18.00, 0.70, '2024-01-01', 1),
(35, 35, 18.00, 0.70, '2024-01-01', 1),
(36, 36, 18.00, 0.70, '2024-01-01', 1),
(37, 37, 18.00, 0.70, '2024-01-01', 1),
(38, 38, 18.00, 0.70, '2024-01-01', 1);

-- 照片书类 (纪念册等, 起步价)
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(41, 41, 68.00,  0.00, '2024-01-01', 1),   -- A5精装纪念册 ¥68起
(42, 42, 98.00,  0.00, '2024-01-01', 1),   -- A4精装纪念册
(43, 43, 48.00,  0.00, '2024-01-01', 1),   -- A4轻奢杂志册
(44, 44, 88.00,  0.00, '2024-01-01', 1),   -- 锁线精装照片书
(45, 45, 68.00,  0.00, '2024-01-01', 1),   -- 文绘集
(46, 46, 78.00,  0.00, '2024-01-01', 1),   -- 蝴蝶写真集
(47, 47, 58.00,  0.00, '2024-01-01', 1);   -- 横款照片书

-- 照片冲印
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(51, 51, 16.90, 0.00, '2024-01-01', 1);    -- ¥16.9起

-- 摆台
INSERT INTO catalog_price_rule (id, sku_id, base_price, page_unit_price, effective_from, is_active) VALUES
(61, 61, 33.00, 0.00, '2024-01-01', 1),    -- 12寸创意摆台
(62, 62, 39.00, 0.00, '2024-01-01', 1);    -- 8寸水晶摆台

-- ----- 9. 套餐 -----
INSERT INTO catalog_bundle (id, bundle_code, name, description, bundle_price, original_price, sort_order) VALUES
(1, 'TNEORJ', '轻奢杂志册6件套',        '内含6本轻奢杂志，每本24页，最多可传图120张',           198.00, 288.00, 1),
(2, 'IXXMRL', '锁线照片书3件套',        '内含3本锁线照片书',                                   248.00, 384.00, 2),
(3, 'TETSWB', '心书皮面册6件套',        '内含6本心书皮面册',                                   198.00, 288.00, 3),
(4, 'QUHLMI', '12寸创意摆台6件套',      '内含6套12寸创意摆台',                                 198.00, 288.00, 4),
(5, 'A4ALBUM3','A4纪念册(20页)3件套',   '内含3本A4精装纪念册(20页)',                            228.00, 354.00, 5);

-- ----- 10. 套餐明细 -----
INSERT INTO catalog_bundle_item (id, bundle_id, sku_id, quantity, max_pages, max_images, sort_order) VALUES
(1, 1, 43, 6, 24, 120, 1),   -- 轻奢杂志册6件套 → A4轻奢杂志册 ×6
(2, 2, 44, 3, NULL, NULL, 1), -- 锁线照片书3件套 → 锁线照片书 ×3
(3, 4, 61, 6, NULL, NULL, 1), -- 12寸摆台6件套 → 12寸摆台 ×6
(4, 5, 42, 3, 20, NULL, 1);   -- A4纪念册3件套 → A4精装纪念册 ×3

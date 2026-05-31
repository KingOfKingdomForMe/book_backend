DROP PROCEDURE IF EXISTS usp_DefaultAlbums_Count;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_List;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_GetDetail;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_GetActiveByProductCode;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_GetTemplates;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_FindPreviewFile;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_FindCreator;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_FindProductId;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_FindTemplateIdsByCodes;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_Create;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_Update;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_AddTemplate;
DROP PROCEDURE IF EXISTS usp_DefaultAlbums_DeleteTemplates;

DELIMITER $$

CREATE PROCEDURE usp_DefaultAlbums_Count(
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_product_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_book_type VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_category VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_active TINYINT
)
BEGIN
    SELECT COUNT(*)
    FROM default_album a
    LEFT JOIN catalog_product_spu product ON product.id = a.product_spu_id
    WHERE (p_keyword IS NULL
                OR a.album_code COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR a.name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR a.description COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
            AND (p_product_code IS NULL OR product.spu_code COLLATE utf8mb4_unicode_ci = p_product_code COLLATE utf8mb4_unicode_ci)
            AND (p_book_type IS NULL OR a.book_type COLLATE utf8mb4_unicode_ci = p_book_type COLLATE utf8mb4_unicode_ci)
            AND (p_category IS NULL OR a.category COLLATE utf8mb4_unicode_ci = p_category COLLATE utf8mb4_unicode_ci)
            AND (p_is_active IS NULL OR a.is_active = p_is_active);
END $$

CREATE PROCEDURE usp_DefaultAlbums_List(
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_product_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_book_type VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_category VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_active TINYINT,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        a.id AS AlbumId,
        a.album_code AS AlbumCode,
        product.spu_code AS ProductCode,
        a.name AS Name,
        a.description AS Description,
        a.book_type AS BookType,
        a.category AS Category,
        a.theme_code AS ThemeCode,
        a.is_active AS IsActive,
        a.sort_order AS SortOrder,
        (
            SELECT COUNT(*)
            FROM default_album_template dat
            WHERE dat.default_album_id = a.id
        ) AS TemplateCount,
        a.updated_at AS UpdatedAtUtc,
        preview.bucket_name AS PreviewBucket,
        preview.object_key AS PreviewObjectKey
    FROM default_album a
    LEFT JOIN catalog_product_spu product ON product.id = a.product_spu_id
    LEFT JOIN storage_file_object preview ON preview.id = a.preview_file_id AND preview.storage_status = 1
    WHERE (p_keyword IS NULL
                OR a.album_code COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR a.name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR a.description COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
            AND (p_product_code IS NULL OR product.spu_code COLLATE utf8mb4_unicode_ci = p_product_code COLLATE utf8mb4_unicode_ci)
            AND (p_book_type IS NULL OR a.book_type COLLATE utf8mb4_unicode_ci = p_book_type COLLATE utf8mb4_unicode_ci)
            AND (p_category IS NULL OR a.category COLLATE utf8mb4_unicode_ci = p_category COLLATE utf8mb4_unicode_ci)
            AND (p_is_active IS NULL OR a.is_active = p_is_active)
    ORDER BY a.sort_order ASC, a.updated_at DESC, a.id DESC
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_DefaultAlbums_GetDetail(
    IN p_album_code VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        a.id AS AlbumId,
        a.album_code AS AlbumCode,
        product.spu_code AS ProductCode,
        a.name AS Name,
        a.description AS Description,
        a.book_type AS BookType,
        a.category AS Category,
        a.theme_code AS ThemeCode,
        a.preview_file_id AS PreviewFileId,
        a.created_by_user_id AS CreatedByUserId,
        a.is_active AS IsActive,
        a.sort_order AS SortOrder,
        (
            SELECT COUNT(*)
            FROM default_album_template dat
            WHERE dat.default_album_id = a.id
        ) AS TemplateCount,
        a.created_at AS CreatedAtUtc,
        a.updated_at AS UpdatedAtUtc,
        preview.bucket_name AS PreviewBucket,
        preview.object_key AS PreviewObjectKey
    FROM default_album a
    LEFT JOIN catalog_product_spu product ON product.id = a.product_spu_id
    LEFT JOIN storage_file_object preview ON preview.id = a.preview_file_id AND preview.storage_status = 1
    WHERE a.album_code COLLATE utf8mb4_unicode_ci = p_album_code COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_DefaultAlbums_GetActiveByProductCode(
    IN p_product_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        a.id AS AlbumId,
        a.album_code AS AlbumCode,
        product.spu_code AS ProductCode,
        a.name AS Name,
        a.description AS Description,
        a.book_type AS BookType,
        a.category AS Category,
        a.theme_code AS ThemeCode,
        a.preview_file_id AS PreviewFileId,
        a.created_by_user_id AS CreatedByUserId,
        a.is_active AS IsActive,
        a.sort_order AS SortOrder,
        (
            SELECT COUNT(*)
            FROM default_album_template dat
            WHERE dat.default_album_id = a.id
        ) AS TemplateCount,
        a.created_at AS CreatedAtUtc,
        a.updated_at AS UpdatedAtUtc,
        preview.bucket_name AS PreviewBucket,
        preview.object_key AS PreviewObjectKey
    FROM default_album a
    INNER JOIN catalog_product_spu product ON product.id = a.product_spu_id AND product.is_active = 1
    LEFT JOIN storage_file_object preview ON preview.id = a.preview_file_id AND preview.storage_status = 1
    WHERE product.spu_code COLLATE utf8mb4_unicode_ci = p_product_code COLLATE utf8mb4_unicode_ci
      AND a.is_active = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_DefaultAlbums_GetTemplates(
    IN p_default_album_id BIGINT
)
BEGIN
    SELECT
        dat.id AS ItemId,
        t.id AS TemplateId,
        t.template_code AS TemplateCode,
        t.name AS Name,
        t.description AS Description,
        t.page_type AS PageType,
        t.category AS Category,
        t.theme_code AS ThemeCode,
        t.schema_version AS SchemaVersion,
        t.json_source AS JsonSource,
        dat.sort_order AS SortOrder,
        preview.bucket_name AS PreviewBucket,
        preview.object_key AS PreviewObjectKey
    FROM default_album_template dat
    INNER JOIN album_content_template t ON t.id = dat.template_id
    LEFT JOIN storage_file_object preview ON preview.id = t.preview_file_id AND preview.storage_status = 1
    WHERE dat.default_album_id = p_default_album_id
    ORDER BY dat.sort_order ASC, dat.id ASC;
END $$

CREATE PROCEDURE usp_DefaultAlbums_FindPreviewFile(
    IN p_preview_file_id BIGINT
)
BEGIN
    SELECT id
    FROM storage_file_object
    WHERE id = p_preview_file_id
      AND storage_status = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_DefaultAlbums_FindCreator(
    IN p_created_by_user_id BIGINT
)
BEGIN
    SELECT id
    FROM identity_user
    WHERE id = p_created_by_user_id
    LIMIT 1;
END $$

CREATE PROCEDURE usp_DefaultAlbums_FindProductId(
    IN p_product_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT id
    FROM catalog_product_spu
    WHERE spu_code COLLATE utf8mb4_unicode_ci = p_product_code COLLATE utf8mb4_unicode_ci
      AND is_active = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_DefaultAlbums_FindTemplateIdsByCodes(
    IN p_template_codes LONGTEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        t.id AS TemplateId,
        t.template_code AS TemplateCode
    FROM album_content_template t
    WHERE FIND_IN_SET(t.template_code, p_template_codes) > 0;
END $$

CREATE PROCEDURE usp_DefaultAlbums_Create(
    IN p_album_code VARCHAR(128),
    IN p_product_spu_id BIGINT,
    IN p_name VARCHAR(256),
    IN p_description TEXT,
    IN p_book_type VARCHAR(64),
    IN p_category VARCHAR(64),
    IN p_theme_code VARCHAR(64),
    IN p_preview_file_id BIGINT,
    IN p_created_by_user_id BIGINT,
    IN p_is_active TINYINT,
    IN p_sort_order INT
)
BEGIN
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
    VALUES (
        p_album_code,
        p_product_spu_id,
        p_name,
        p_description,
        p_book_type,
        p_category,
        p_theme_code,
        p_preview_file_id,
        p_created_by_user_id,
        p_is_active,
        p_sort_order);
END $$

CREATE PROCEDURE usp_DefaultAlbums_Update(
    IN p_album_code VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_product_spu_id BIGINT,
    IN p_name VARCHAR(256),
    IN p_description TEXT,
    IN p_book_type VARCHAR(64),
    IN p_category VARCHAR(64),
    IN p_theme_code VARCHAR(64),
    IN p_preview_file_id BIGINT,
    IN p_created_by_user_id BIGINT,
    IN p_is_active TINYINT,
    IN p_sort_order INT
)
BEGIN
    UPDATE default_album
    SET product_spu_id = p_product_spu_id,
        name = p_name,
        description = p_description,
        book_type = p_book_type,
        category = p_category,
        theme_code = p_theme_code,
        preview_file_id = p_preview_file_id,
        created_by_user_id = p_created_by_user_id,
        is_active = p_is_active,
        sort_order = p_sort_order,
        updated_at = CURRENT_TIMESTAMP
    WHERE album_code COLLATE utf8mb4_unicode_ci = p_album_code COLLATE utf8mb4_unicode_ci;
END $$

CREATE PROCEDURE usp_DefaultAlbums_AddTemplate(
    IN p_default_album_id BIGINT,
    IN p_template_id BIGINT,
    IN p_sort_order INT
)
BEGIN
    INSERT INTO default_album_template (
        default_album_id,
        template_id,
        sort_order)
    VALUES (
        p_default_album_id,
        p_template_id,
        p_sort_order);
END $$

CREATE PROCEDURE usp_DefaultAlbums_DeleteTemplates(
    IN p_default_album_id BIGINT
)
BEGIN
    DELETE FROM default_album_template
    WHERE default_album_id = p_default_album_id;
END $$

DELIMITER ;
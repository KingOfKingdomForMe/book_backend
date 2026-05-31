DROP PROCEDURE IF EXISTS usp_AlbumTemplates_Count;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_List;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_GetDetail;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_FindPreviewFile;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_FindCreator;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_Create;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_Update;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_Delete;
DROP PROCEDURE IF EXISTS usp_AlbumTemplates_DeleteAll;

DELIMITER $$

CREATE PROCEDURE usp_AlbumTemplates_Count(
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_book_type VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_page_type VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_category VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_active TINYINT
)
BEGIN
    SELECT COUNT(*)
    FROM album_content_template t
    WHERE (p_keyword IS NULL
                OR t.template_code COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR t.name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR t.description COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
            AND (p_book_type IS NULL OR t.book_type COLLATE utf8mb4_unicode_ci = p_book_type COLLATE utf8mb4_unicode_ci)
            AND ((p_page_type IS NULL AND t.page_type COLLATE utf8mb4_unicode_ci <> 'cover-template')
                OR (p_page_type IS NOT NULL AND t.page_type COLLATE utf8mb4_unicode_ci = p_page_type COLLATE utf8mb4_unicode_ci))
            AND (p_category IS NULL OR t.category COLLATE utf8mb4_unicode_ci = p_category COLLATE utf8mb4_unicode_ci)
      AND (p_is_active IS NULL OR t.is_active = p_is_active);
END $$

CREATE PROCEDURE usp_AlbumTemplates_List(
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_book_type VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_page_type VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_category VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_active TINYINT,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        t.id AS TemplateId,
        t.template_code AS TemplateCode,
        t.name AS Name,
        t.description AS Description,
        t.book_type AS BookType,
        t.page_type AS PageType,
        t.category AS Category,
        t.theme_code AS ThemeCode,
        t.schema_version AS SchemaVersion,
        t.json_source AS JsonSource,
        t.is_built_in AS IsBuiltIn,
        t.is_active AS IsActive,
        t.sort_order AS SortOrder,
        t.updated_at AS UpdatedAtUtc,
        preview.bucket_name AS PreviewBucket,
        preview.object_key AS PreviewObjectKey
    FROM album_content_template t
    LEFT JOIN storage_file_object preview ON preview.id = t.preview_file_id AND preview.storage_status = 1
    WHERE (p_keyword IS NULL
                OR t.template_code COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR t.name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR t.description COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
            AND (p_book_type IS NULL OR t.book_type COLLATE utf8mb4_unicode_ci = p_book_type COLLATE utf8mb4_unicode_ci)
                        AND ((p_page_type IS NULL AND t.page_type COLLATE utf8mb4_unicode_ci <> 'cover-template')
                                OR (p_page_type IS NOT NULL AND t.page_type COLLATE utf8mb4_unicode_ci = p_page_type COLLATE utf8mb4_unicode_ci))
            AND (p_category IS NULL OR t.category COLLATE utf8mb4_unicode_ci = p_category COLLATE utf8mb4_unicode_ci)
      AND (p_is_active IS NULL OR t.is_active = p_is_active)
    ORDER BY t.sort_order ASC, t.updated_at DESC, t.id DESC
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_AlbumTemplates_GetDetail(
    IN p_template_code VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        t.id AS TemplateId,
        t.template_code AS TemplateCode,
        t.name AS Name,
        t.description AS Description,
        t.book_type AS BookType,
        t.page_type AS PageType,
        t.category AS Category,
        t.theme_code AS ThemeCode,
        t.schema_version AS SchemaVersion,
        t.json_source AS JsonSource,
        t.preview_file_id AS PreviewFileId,
        t.created_by_user_id AS CreatedByUserId,
        t.is_built_in AS IsBuiltIn,
        t.is_active AS IsActive,
        t.sort_order AS SortOrder,
        t.created_at AS CreatedAtUtc,
        t.updated_at AS UpdatedAtUtc,
        preview.bucket_name AS PreviewBucket,
        preview.object_key AS PreviewObjectKey
    FROM album_content_template t
    LEFT JOIN storage_file_object preview ON preview.id = t.preview_file_id AND preview.storage_status = 1
    WHERE t.template_code COLLATE utf8mb4_unicode_ci = p_template_code COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_AlbumTemplates_FindPreviewFile(
    IN p_preview_file_id BIGINT
)
BEGIN
    SELECT id
    FROM storage_file_object
    WHERE id = p_preview_file_id
      AND storage_status = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_AlbumTemplates_FindCreator(
    IN p_created_by_user_id BIGINT
)
BEGIN
    SELECT id
    FROM identity_user
    WHERE id = p_created_by_user_id
    LIMIT 1;
END $$

CREATE PROCEDURE usp_AlbumTemplates_Create(
    IN p_template_code VARCHAR(128),
    IN p_name VARCHAR(256),
    IN p_description TEXT,
    IN p_book_type VARCHAR(64),
    IN p_page_type VARCHAR(64),
    IN p_category VARCHAR(64),
    IN p_theme_code VARCHAR(64),
    IN p_schema_version VARCHAR(64),
    IN p_json_source LONGTEXT,
    IN p_preview_file_id BIGINT,
    IN p_created_by_user_id BIGINT,
    IN p_is_built_in TINYINT,
    IN p_is_active TINYINT,
    IN p_sort_order INT
)
BEGIN
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
    VALUES (
        p_template_code,
        p_name,
        p_description,
        p_book_type,
        p_page_type,
        p_category,
        p_theme_code,
        p_schema_version,
        p_json_source,
        p_preview_file_id,
        p_created_by_user_id,
        p_is_built_in,
        p_is_active,
        p_sort_order);
END $$

CREATE PROCEDURE usp_AlbumTemplates_Update(
    IN p_template_code VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_name VARCHAR(256),
    IN p_description TEXT,
    IN p_book_type VARCHAR(64),
    IN p_page_type VARCHAR(64),
    IN p_category VARCHAR(64),
    IN p_theme_code VARCHAR(64),
    IN p_schema_version VARCHAR(64),
    IN p_json_source LONGTEXT,
    IN p_preview_file_id BIGINT,
    IN p_created_by_user_id BIGINT,
    IN p_is_built_in TINYINT,
    IN p_is_active TINYINT,
    IN p_sort_order INT
)
BEGIN
    UPDATE album_content_template
    SET name = p_name,
        description = p_description,
        book_type = p_book_type,
        page_type = p_page_type,
        category = p_category,
        theme_code = p_theme_code,
        schema_version = p_schema_version,
        json_source = p_json_source,
        preview_file_id = p_preview_file_id,
        created_by_user_id = p_created_by_user_id,
        is_built_in = p_is_built_in,
        is_active = p_is_active,
        sort_order = p_sort_order,
        updated_at = CURRENT_TIMESTAMP
    WHERE template_code COLLATE utf8mb4_unicode_ci = p_template_code COLLATE utf8mb4_unicode_ci;
END $$

CREATE PROCEDURE usp_AlbumTemplates_Delete(
        IN p_template_code VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
        DELETE dat
        FROM default_album_template dat
        INNER JOIN album_content_template t ON t.id = dat.template_id
        WHERE t.template_code COLLATE utf8mb4_unicode_ci = p_template_code COLLATE utf8mb4_unicode_ci
            AND t.page_type COLLATE utf8mb4_unicode_ci <> 'cover-template';

        DELETE a
        FROM default_album a
        LEFT JOIN default_album_template dat ON dat.default_album_id = a.id
        WHERE dat.default_album_id IS NULL;

        DELETE FROM album_content_template
        WHERE template_code COLLATE utf8mb4_unicode_ci = p_template_code COLLATE utf8mb4_unicode_ci
            AND page_type COLLATE utf8mb4_unicode_ci <> 'cover-template';

        DELETE a
        FROM default_album a
        LEFT JOIN default_album_template dat ON dat.default_album_id = a.id
        WHERE dat.default_album_id IS NULL;

        SELECT ROW_COUNT() AS AffectedRows;
END $$

CREATE PROCEDURE usp_AlbumTemplates_DeleteAll()
BEGIN
        DELETE dat
        FROM default_album_template dat
        INNER JOIN album_content_template t ON t.id = dat.template_id
        WHERE t.page_type COLLATE utf8mb4_unicode_ci <> 'cover-template';

    DELETE a
    FROM default_album a
    LEFT JOIN default_album_template dat ON dat.default_album_id = a.id
    WHERE dat.default_album_id IS NULL;

        DELETE FROM album_content_template
        WHERE page_type COLLATE utf8mb4_unicode_ci <> 'cover-template';

    DELETE a
    FROM default_album a
    LEFT JOIN default_album_template dat ON dat.default_album_id = a.id
    WHERE dat.default_album_id IS NULL;

        SELECT ROW_COUNT() AS AffectedRows;
END $$

DELIMITER ;
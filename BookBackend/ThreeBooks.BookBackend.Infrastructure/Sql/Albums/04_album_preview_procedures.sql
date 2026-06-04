DROP PROCEDURE IF EXISTS usp_Album_FindProductId;
DROP PROCEDURE IF EXISTS usp_Album_FindProjectByShareCode;
DROP PROCEDURE IF EXISTS usp_Album_CountProjectsByUser;
DROP PROCEDURE IF EXISTS usp_Album_ListProjectsByUser;
DROP PROCEDURE IF EXISTS usp_Album_InsertProject;
DROP PROCEDURE IF EXISTS usp_Album_InsertVersion;
DROP PROCEDURE IF EXISTS usp_Album_UpdateProjectSharedVersion;
DROP PROCEDURE IF EXISTS usp_Album_FindProjectPreview;
DROP PROCEDURE IF EXISTS usp_Album_FindPagesByVersion;
DROP PROCEDURE IF EXISTS usp_Album_FindPageByShareCodeAndPageNo;
DROP PROCEDURE IF EXISTS usp_Album_FindPageAssetsByVersionPage;
DROP PROCEDURE IF EXISTS usp_Album_FindProjectForMutation;
DROP PROCEDURE IF EXISTS usp_Album_FindProjectForWrite;
DROP PROCEDURE IF EXISTS usp_Album_FindActiveFilesByIds;
DROP PROCEDURE IF EXISTS usp_Album_InsertVersionPage;
DROP PROCEDURE IF EXISTS usp_Album_InsertVersionPageAsset;
DROP PROCEDURE IF EXISTS usp_Album_DeleteVersionPageAssets;
DROP PROCEDURE IF EXISTS usp_Album_DeleteVersionPages;
DROP PROCEDURE IF EXISTS usp_Album_FindVersionStats;
DROP PROCEDURE IF EXISTS usp_Album_FindSnapshotPages;
DROP PROCEDURE IF EXISTS usp_Album_UpdateProjectVersionSnapshot;
DROP PROCEDURE IF EXISTS usp_Album_UpdateProjectCounts;
DROP PROCEDURE IF EXISTS usp_Album_InsertViewLog;
DROP PROCEDURE IF EXISTS usp_Album_InsertShareLog;
DROP PROCEDURE IF EXISTS usp_Album_IncrementViewCount;
DROP PROCEDURE IF EXISTS usp_Album_IncrementShareCount;

DELIMITER $$

CREATE PROCEDURE usp_Album_FindProductId(
    IN p_product_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT id
    FROM catalog_product_spu
        WHERE spu_code COLLATE utf8mb4_unicode_ci = p_product_code COLLATE utf8mb4_unicode_ci
      AND is_active = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Album_FindProjectByShareCode(
    IN p_share_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT id
    FROM book_project
    WHERE share_code COLLATE utf8mb4_unicode_ci = p_share_code COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Album_CountProjectsByUser(
    IN p_user_id BIGINT
)
BEGIN
    SELECT COUNT(*)
    FROM book_project p
    WHERE p.user_id = p_user_id;
END $$

CREATE PROCEDURE usp_Album_ListProjectsByUser(
    IN p_user_id BIGINT,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        p.id AS ProjectId,
        p.share_code AS ShareCode,
        p.title AS Title,
        p.subtitle AS Subtitle,
        p.book_type AS BookType,
        COALESCE(spu.spu_code, p.book_type) AS ProductCode,
        p.is_public AS IsPublic,
        p.page_count AS PageCount,
        p.image_count AS ImageCount,
        p.extra_properties_json AS ExtraPropertiesJson,
        p.view_count AS ViewCount,
        p.share_count AS ShareCount,
        p.created_at AS CreatedAtUtc,
        p.updated_at AS UpdatedAtUtc
    FROM book_project p
    LEFT JOIN catalog_product_spu spu ON spu.id = p.spu_id
    WHERE p.user_id = p_user_id
    ORDER BY p.updated_at DESC, p.id DESC
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_Album_InsertProject(
    IN p_user_id BIGINT,
    IN p_book_type VARCHAR(64),
    IN p_title VARCHAR(256),
    IN p_subtitle VARCHAR(256),
    IN p_status INT,
    IN p_spu_id BIGINT,
    IN p_share_code VARCHAR(64),
    IN p_is_public TINYINT,
    IN p_shared_at DATETIME,
    IN p_extra_properties_json LONGTEXT
)
BEGIN
    INSERT INTO book_project (
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
        share_code,
        is_public,
        shared_version_id,
        shared_at,
        view_count,
        share_count,
        extra_properties_json)
    VALUES (
        p_user_id,
        p_book_type,
        p_title,
        p_subtitle,
        p_status,
        p_spu_id,
        0,
        0,
        0,
        NULL,
        p_share_code,
        p_is_public,
        NULL,
        p_shared_at,
        0,
        0,
        p_extra_properties_json);
END $$

CREATE PROCEDURE usp_Album_InsertVersion(
    IN p_project_id BIGINT,
    IN p_version_no INT,
    IN p_snapshot_data LONGTEXT,
    IN p_render_version VARCHAR(64)
)
BEGIN
    INSERT INTO book_project_version (
        project_id,
        version_no,
        snapshot_data,
        page_count,
        image_count,
        render_version,
        frozen_at)
    VALUES (
        p_project_id,
        p_version_no,
        p_snapshot_data,
        0,
        0,
        p_render_version,
        UTC_TIMESTAMP());
END $$

CREATE PROCEDURE usp_Album_UpdateProjectSharedVersion(
    IN p_project_id BIGINT,
    IN p_version_id BIGINT,
    IN p_shared_at DATETIME
)
BEGIN
    UPDATE book_project
    SET shared_version_id = p_version_id,
        shared_at = p_shared_at,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_project_id;
END $$

CREATE PROCEDURE usp_Album_FindProjectPreview(
    IN p_share_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        p.id AS ProjectId,
        p.share_code AS ShareCode,
        p.title AS Title,
        p.subtitle AS Subtitle,
        p.book_type AS BookType,
        spu.spu_code AS ProductCode,
        p.page_count AS PageCount,
        p.image_count AS ImageCount,
        p.view_count AS ViewCount,
        p.share_count AS ShareCount,
        p.extra_properties_json AS ExtraPropertiesJson,
        p.shared_version_id AS SharedVersionId
    FROM book_project p
    LEFT JOIN catalog_product_spu spu ON spu.id = p.spu_id AND spu.is_active = 1
        WHERE p.share_code COLLATE utf8mb4_unicode_ci = p_share_code COLLATE utf8mb4_unicode_ci
      AND p.is_public = 1
      AND p.shared_version_id IS NOT NULL
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Album_FindPagesByVersion(
    IN p_project_version_id BIGINT
)
BEGIN
    SELECT
        vp.page_no AS PageNo,
        vp.page_label AS PageLabel,
        vp.page_type AS PageType,
        vp.json_source AS JsonSource,
        vp.json_file_id AS JsonFileId,
        vp.json_bucket AS JsonBucket,
        vp.json_object_key AS JsonObjectKey,
        vp.html_file_id AS HtmlFileId,
        vp.html_bucket AS HtmlBucket,
        vp.html_object_key AS HtmlObjectKey,
        vp.thumbnail_file_id AS ThumbnailFileId,
        vp.thumbnail_bucket AS ThumbnailBucket,
        vp.thumbnail_object_key AS ThumbnailObjectKey,
        CASE WHEN image_stats.asset_count > 0 THEN TRUE ELSE FALSE END AS HasImages
    FROM book_project_version_page vp
    LEFT JOIN (
        SELECT
            version_page_id,
            COUNT(*) AS asset_count
        FROM book_project_version_page_asset
        GROUP BY version_page_id
    ) image_stats ON image_stats.version_page_id = vp.id
    WHERE vp.project_version_id = p_project_version_id
    ORDER BY vp.sort_order, vp.page_no;
END $$

CREATE PROCEDURE usp_Album_FindPageByShareCodeAndPageNo(
    IN p_share_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_page_no INT
)
BEGIN
    SELECT
        vp.id AS VersionPageId,
        vp.page_no AS PageNo,
        vp.page_label AS PageLabel,
        vp.page_type AS PageType,
        vp.schema_version AS SchemaVersion,
        vp.json_source AS JsonSource,
        vp.json_file_id AS JsonFileId,
        vp.json_bucket AS JsonBucket,
        vp.json_object_key AS JsonObjectKey,
        vp.html_file_id AS HtmlFileId,
        vp.html_bucket AS HtmlBucket,
        vp.html_object_key AS HtmlObjectKey
    FROM book_project p
    INNER JOIN book_project_version_page vp ON vp.project_version_id = p.shared_version_id
        WHERE p.share_code COLLATE utf8mb4_unicode_ci = p_share_code COLLATE utf8mb4_unicode_ci
      AND p.is_public = 1
      AND vp.page_no = p_page_no
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Album_FindPageAssetsByVersionPage(
    IN p_version_page_id BIGINT
)
BEGIN
    SELECT
        a.sort_order AS SortOrder,
        a.role AS Role,
        a.file_id AS FileId,
        a.bucket_name AS Bucket,
        a.object_key AS ObjectKey,
        a.width AS Width,
        a.height AS Height,
        a.alt_text AS AltText,
        a.caption AS Caption
    FROM book_project_version_page_asset a
    WHERE a.version_page_id = p_version_page_id
    ORDER BY a.sort_order, a.id;
END $$

CREATE PROCEDURE usp_Album_FindProjectForMutation(
    IN p_share_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT id AS ProjectId
    FROM book_project
        WHERE share_code COLLATE utf8mb4_unicode_ci = p_share_code COLLATE utf8mb4_unicode_ci
      AND is_public = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Album_FindProjectForWrite(
    IN p_share_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        p.id AS ProjectId,
        p.share_code AS ShareCode,
        p.shared_version_id AS SharedVersionId,
        p.is_public AS IsPublic
    FROM book_project p
    WHERE p.share_code COLLATE utf8mb4_unicode_ci = p_share_code COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Album_FindActiveFilesByIds(
    IN p_file_ids TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        id AS FileId,
        bucket_name AS Bucket,
        object_key AS ObjectKey
    FROM storage_file_object
        WHERE FIND_IN_SET(
            CAST(id AS CHAR) COLLATE utf8mb4_unicode_ci,
            p_file_ids COLLATE utf8mb4_unicode_ci) > 0
      AND storage_status = 1;
END $$

CREATE PROCEDURE usp_Album_InsertVersionPage(
    IN p_project_version_id BIGINT,
    IN p_page_no INT,
    IN p_page_label VARCHAR(128),
    IN p_page_type VARCHAR(64),
    IN p_sort_order INT,
    IN p_json_source LONGTEXT,
    IN p_json_file_id BIGINT,
    IN p_json_bucket VARCHAR(128),
    IN p_json_object_key VARCHAR(512),
    IN p_html_file_id BIGINT,
    IN p_html_bucket VARCHAR(128),
    IN p_html_object_key VARCHAR(512),
    IN p_thumbnail_file_id BIGINT,
    IN p_thumbnail_bucket VARCHAR(128),
    IN p_thumbnail_object_key VARCHAR(512),
    IN p_page_width INT,
    IN p_page_height INT,
    IN p_schema_version VARCHAR(64)
)
BEGIN
    INSERT INTO book_project_version_page (
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
        schema_version)
    VALUES (
        p_project_version_id,
        p_page_no,
        p_page_label,
        p_page_type,
        p_sort_order,
        p_json_source,
        p_json_file_id,
        p_json_bucket,
        p_json_object_key,
        p_html_file_id,
        p_html_bucket,
        p_html_object_key,
        p_thumbnail_file_id,
        p_thumbnail_bucket,
        p_thumbnail_object_key,
        p_page_width,
        p_page_height,
        p_schema_version);
END $$

CREATE PROCEDURE usp_Album_InsertVersionPageAsset(
    IN p_version_page_id BIGINT,
    IN p_sort_order INT,
    IN p_role VARCHAR(64),
    IN p_file_id BIGINT,
    IN p_bucket_name VARCHAR(128),
    IN p_object_key VARCHAR(512),
    IN p_width INT,
    IN p_height INT,
    IN p_alt_text VARCHAR(512),
    IN p_caption VARCHAR(512),
    IN p_crop_json LONGTEXT
)
BEGIN
    INSERT INTO book_project_version_page_asset (
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
        p_version_page_id,
        p_sort_order,
        p_role,
        p_file_id,
        p_bucket_name,
        p_object_key,
        p_width,
        p_height,
        p_alt_text,
        p_caption,
        p_crop_json,
        UTC_TIMESTAMP());
END $$

CREATE PROCEDURE usp_Album_DeleteVersionPageAssets(
    IN p_project_version_id BIGINT
)
BEGIN
    DELETE asset
    FROM book_project_version_page_asset asset
    INNER JOIN book_project_version_page page ON page.id = asset.version_page_id
    WHERE page.project_version_id = p_project_version_id;
END $$

CREATE PROCEDURE usp_Album_DeleteVersionPages(
    IN p_project_version_id BIGINT
)
BEGIN
    DELETE FROM book_project_version_page
    WHERE project_version_id = p_project_version_id;
END $$

CREATE PROCEDURE usp_Album_FindVersionStats(
    IN p_project_version_id BIGINT
)
BEGIN
    SELECT
        (SELECT COUNT(*)
         FROM book_project_version_page
         WHERE project_version_id = p_project_version_id) AS PageCount,
        (SELECT COUNT(*)
         FROM book_project_version_page_asset asset
         INNER JOIN book_project_version_page page ON page.id = asset.version_page_id
         WHERE page.project_version_id = p_project_version_id) AS ImageCount;
END $$

CREATE PROCEDURE usp_Album_FindSnapshotPages(
    IN p_project_version_id BIGINT
)
BEGIN
    SELECT
        page_no AS PageNo,
        page_label AS PageLabel,
        page_type AS PageType
    FROM book_project_version_page
    WHERE project_version_id = p_project_version_id
    ORDER BY sort_order, page_no;
END $$

CREATE PROCEDURE usp_Album_UpdateProjectVersionSnapshot(
    IN p_project_version_id BIGINT,
    IN p_snapshot_data LONGTEXT,
    IN p_page_count INT,
    IN p_image_count INT
)
BEGIN
    UPDATE book_project_version
    SET snapshot_data = p_snapshot_data,
        page_count = p_page_count,
        image_count = p_image_count
    WHERE id = p_project_version_id;
END $$

CREATE PROCEDURE usp_Album_UpdateProjectCounts(
    IN p_project_id BIGINT,
    IN p_page_count INT,
    IN p_image_count INT
)
BEGIN
    UPDATE book_project
    SET page_count = p_page_count,
        image_count = p_image_count,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_project_id;
END $$

CREATE PROCEDURE usp_Album_InsertViewLog(
    IN p_project_id BIGINT,
    IN p_share_code VARCHAR(64),
    IN p_page_no INT,
    IN p_client_ip VARCHAR(64),
    IN p_client_user_agent VARCHAR(512)
)
BEGIN
    INSERT INTO book_project_view_log (
        project_id,
        share_code,
        page_no,
        client_ip,
        client_user_agent,
        created_at)
    VALUES (
        p_project_id,
        p_share_code,
        p_page_no,
        p_client_ip,
        p_client_user_agent,
        UTC_TIMESTAMP());
END $$

CREATE PROCEDURE usp_Album_InsertShareLog(
    IN p_project_id BIGINT,
    IN p_share_code VARCHAR(64),
    IN p_channel VARCHAR(64),
    IN p_client_ip VARCHAR(64),
    IN p_client_user_agent VARCHAR(512)
)
BEGIN
    INSERT INTO book_project_share_log (
        project_id,
        share_code,
        channel,
        client_ip,
        client_user_agent,
        created_at)
    VALUES (
        p_project_id,
        p_share_code,
        p_channel,
        p_client_ip,
        p_client_user_agent,
        UTC_TIMESTAMP());
END $$

CREATE PROCEDURE usp_Album_IncrementViewCount(
    IN p_project_id BIGINT
)
BEGIN
    UPDATE book_project
    SET view_count = view_count + 1,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_project_id;
END $$

CREATE PROCEDURE usp_Album_IncrementShareCount(
    IN p_project_id BIGINT
)
BEGIN
    UPDATE book_project
    SET share_count = share_count + 1,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_project_id;
END $$

DELIMITER ;
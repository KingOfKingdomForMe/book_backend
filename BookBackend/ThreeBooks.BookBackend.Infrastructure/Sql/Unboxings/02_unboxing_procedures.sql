DROP PROCEDURE IF EXISTS usp_Unboxing_UserExists;
DROP PROCEDURE IF EXISTS usp_Unboxing_PostNoExists;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetCurrentMaxNumericPostNo;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetLevelByCode;
DROP PROCEDURE IF EXISTS usp_Unboxing_FindExistingPost;
DROP PROCEDURE IF EXISTS usp_Unboxing_InsertPost;
DROP PROCEDURE IF EXISTS usp_Unboxing_UpdatePost;
DROP PROCEDURE IF EXISTS usp_Unboxing_DeleteMedia;
DROP PROCEDURE IF EXISTS usp_Unboxing_DeleteTags;
DROP PROCEDURE IF EXISTS usp_Unboxing_InsertMedia;
DROP PROCEDURE IF EXISTS usp_Unboxing_InsertTag;
DROP PROCEDURE IF EXISTS usp_Unboxing_Count;
DROP PROCEDURE IF EXISTS usp_Unboxing_List;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetDetail;
DROP PROCEDURE IF EXISTS usp_Unboxing_AdminCount;
DROP PROCEDURE IF EXISTS usp_Unboxing_AdminList;
DROP PROCEDURE IF EXISTS usp_Unboxing_AdminGetDetail;
DROP PROCEDURE IF EXISTS usp_Unboxing_DeletePost;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetMedia;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetTagsByPostIds;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetTagsByPostId;
DROP PROCEDURE IF EXISTS usp_Unboxing_GetLevels;

DELIMITER $$

CREATE PROCEDURE usp_Unboxing_UserExists(
    IN p_user_id BIGINT
)
BEGIN
    SELECT COUNT(*)
    FROM identity_user
    WHERE id = p_user_id;
END $$

CREATE PROCEDURE usp_Unboxing_PostNoExists(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT COUNT(*)
    FROM community_unboxing_post
    WHERE post_no COLLATE utf8mb4_unicode_ci = p_post_no COLLATE utf8mb4_unicode_ci;
END $$

CREATE PROCEDURE usp_Unboxing_GetCurrentMaxNumericPostNo()
BEGIN
    SELECT COALESCE(MAX(CAST(post_no AS UNSIGNED)), 40000)
    FROM community_unboxing_post
    WHERE post_no REGEXP '^[0-9]+$';
END $$

CREATE PROCEDURE usp_Unboxing_GetLevelByCode(
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        level_code AS LevelCode,
        level_name AS LevelName,
        icon_url AS IconUrl
    FROM community_unboxing_level
    WHERE level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Unboxing_FindExistingPost(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        id AS PostId,
        user_id AS UserId
    FROM community_unboxing_post
    WHERE post_no COLLATE utf8mb4_unicode_ci = p_post_no COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Unboxing_InsertPost(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_user_id BIGINT,
    IN p_author_name VARCHAR(128),
    IN p_author_avatar_url VARCHAR(512),
    IN p_order_item_id BIGINT,
    IN p_project_version_id BIGINT,
    IN p_product_label VARCHAR(256),
    IN p_book_title VARCHAR(256),
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_title VARCHAR(256),
    IN p_content_text LONGTEXT,
    IN p_status INT,
    IN p_is_featured TINYINT,
    IN p_published_at_utc DATETIME
)
BEGIN
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
    VALUES (
        p_post_no,
        p_user_id,
        p_author_name,
        p_author_avatar_url,
        p_order_item_id,
        p_project_version_id,
        p_product_label,
        p_book_title,
        (SELECT id FROM community_unboxing_level WHERE level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci LIMIT 1),
        p_title,
        p_content_text,
        p_status,
        p_is_featured,
        p_published_at_utc);
END $$

CREATE PROCEDURE usp_Unboxing_UpdatePost(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_author_name VARCHAR(128),
    IN p_author_avatar_url VARCHAR(512),
    IN p_order_item_id BIGINT,
    IN p_project_version_id BIGINT,
    IN p_product_label VARCHAR(256),
    IN p_book_title VARCHAR(256),
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_title VARCHAR(256),
    IN p_content_text LONGTEXT,
    IN p_status INT,
    IN p_is_featured TINYINT,
    IN p_published_at_utc DATETIME
)
BEGIN
    UPDATE community_unboxing_post
    SET author_name = p_author_name,
        author_avatar = p_author_avatar_url,
        order_item_id = p_order_item_id,
        project_version_id = p_project_version_id,
        product_label = p_product_label,
        book_title = p_book_title,
        level_id = (SELECT id FROM community_unboxing_level WHERE level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci LIMIT 1),
        title = p_title,
        content_text = p_content_text,
        status = p_status,
        is_featured = p_is_featured,
        published_at = p_published_at_utc,
        updated_at = CURRENT_TIMESTAMP
    WHERE post_no COLLATE utf8mb4_unicode_ci = p_post_no COLLATE utf8mb4_unicode_ci;
END $$

CREATE PROCEDURE usp_Unboxing_DeleteMedia(
    IN p_post_id BIGINT
)
BEGIN
    DELETE FROM community_unboxing_media
    WHERE post_id = p_post_id;
END $$

CREATE PROCEDURE usp_Unboxing_DeleteTags(
    IN p_post_id BIGINT
)
BEGIN
    DELETE FROM community_unboxing_tag
    WHERE post_id = p_post_id;
END $$

CREATE PROCEDURE usp_Unboxing_InsertMedia(
    IN p_post_id BIGINT,
    IN p_media_type VARCHAR(32),
    IN p_storage_url VARCHAR(512),
    IN p_thumbnail_url VARCHAR(512),
    IN p_sort_order INT,
    IN p_width INT,
    IN p_height INT
)
BEGIN
    INSERT INTO community_unboxing_media (
        post_id,
        media_type,
        storage_url,
        thumbnail_url,
        sort_order,
        width,
        height)
    VALUES (
        p_post_id,
        p_media_type,
        p_storage_url,
        p_thumbnail_url,
        p_sort_order,
        p_width,
        p_height);
END $$

CREATE PROCEDURE usp_Unboxing_InsertTag(
    IN p_post_id BIGINT,
    IN p_tag_name VARCHAR(128)
)
BEGIN
    INSERT INTO community_unboxing_tag (
        post_id,
        tag_name)
    VALUES (
        p_post_id,
        p_tag_name);
END $$

CREATE PROCEDURE usp_Unboxing_Count(
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_featured TINYINT
)
BEGIN
    SELECT COUNT(*)
    FROM community_unboxing_post post
    LEFT JOIN community_unboxing_level level ON level.id = post.level_id
    WHERE post.status = 1
      AND post.published_at IS NOT NULL
            AND (p_level_code IS NULL OR level.level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci)
      AND (p_is_featured IS NULL OR post.is_featured = p_is_featured);
END $$

CREATE PROCEDURE usp_Unboxing_List(
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_featured TINYINT,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        post.id AS PostId,
        post.post_no AS PostNo,
        post.author_name AS AuthorName,
        post.author_avatar AS AuthorAvatarUrl,
        post.title AS Title,
        post.book_title AS BookTitle,
        post.content_text AS ContentText,
        post.product_label AS ProductLabel,
        post.is_featured AS IsFeatured,
        post.published_at AS PublishedAtUtc,
        level.level_code AS LevelCode,
        level.level_name AS LevelName,
        level.icon_url AS LevelIconUrl,
        cover.storage_url AS CoverImageUrl,
        cover.thumbnail_url AS CoverThumbnailUrl
    FROM community_unboxing_post post
    LEFT JOIN community_unboxing_level level ON level.id = post.level_id
    LEFT JOIN community_unboxing_media cover ON cover.id = (
        SELECT media.id
        FROM community_unboxing_media media
        WHERE media.post_id = post.id
        ORDER BY CASE WHEN media.media_type = 'image' THEN 0 ELSE 1 END, media.sort_order, media.id
        LIMIT 1
    )
    WHERE post.status = 1
      AND post.published_at IS NOT NULL
            AND (p_level_code IS NULL OR level.level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci)
      AND (p_is_featured IS NULL OR post.is_featured = p_is_featured)
    ORDER BY post.published_at DESC, post.id DESC
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_Unboxing_GetDetail(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        post.id AS PostId,
        post.post_no AS PostNo,
        post.author_name AS AuthorName,
        post.author_avatar AS AuthorAvatarUrl,
        post.title AS Title,
        post.book_title AS BookTitle,
        post.content_text AS ContentText,
        post.product_label AS ProductLabel,
        post.is_featured AS IsFeatured,
        post.published_at AS PublishedAtUtc,
        level.level_code AS LevelCode,
        level.level_name AS LevelName,
        level.icon_url AS LevelIconUrl
    FROM community_unboxing_post post
    LEFT JOIN community_unboxing_level level ON level.id = post.level_id
        WHERE post.post_no COLLATE utf8mb4_unicode_ci = p_post_no COLLATE utf8mb4_unicode_ci
      AND post.status = 1
      AND post.published_at IS NOT NULL
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Unboxing_AdminCount(
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_featured TINYINT,
    IN p_status INT
)
BEGIN
    SELECT COUNT(*)
    FROM community_unboxing_post post
    LEFT JOIN community_unboxing_level level ON level.id = post.level_id
    WHERE (p_keyword IS NULL
                OR post.post_no COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR post.title COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR post.book_title COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR post.author_name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
      AND (p_level_code IS NULL OR level.level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci)
      AND (p_is_featured IS NULL OR post.is_featured = p_is_featured)
      AND (p_status IS NULL OR post.status = p_status);
END $$

CREATE PROCEDURE usp_Unboxing_AdminList(
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_level_code VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_is_featured TINYINT,
    IN p_status INT,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        post.id AS PostId,
        post.post_no AS PostNo,
        post.user_id AS UserId,
        post.author_name AS AuthorName,
        post.author_avatar AS AuthorAvatarUrl,
        post.title AS Title,
        post.book_title AS BookTitle,
        post.product_label AS ProductLabel,
        post.status AS Status,
        post.is_featured AS IsFeatured,
        post.published_at AS PublishedAtUtc,
        post.created_at AS CreatedAtUtc,
        post.updated_at AS UpdatedAtUtc,
        level.level_code AS LevelCode,
        level.level_name AS LevelName,
        level.icon_url AS LevelIconUrl,
        cover.storage_url AS CoverImageUrl,
        cover.thumbnail_url AS CoverThumbnailUrl
    FROM community_unboxing_post post
    LEFT JOIN community_unboxing_level level ON level.id = post.level_id
    LEFT JOIN community_unboxing_media cover ON cover.id = (
        SELECT media.id
        FROM community_unboxing_media media
        WHERE media.post_id = post.id
        ORDER BY CASE WHEN media.media_type = 'image' THEN 0 ELSE 1 END, media.sort_order, media.id
        LIMIT 1
    )
    WHERE (p_keyword IS NULL
                OR post.post_no COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR post.title COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR post.book_title COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
                OR post.author_name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci)
      AND (p_level_code IS NULL OR level.level_code COLLATE utf8mb4_unicode_ci = p_level_code COLLATE utf8mb4_unicode_ci)
      AND (p_is_featured IS NULL OR post.is_featured = p_is_featured)
      AND (p_status IS NULL OR post.status = p_status)
    ORDER BY post.updated_at DESC, post.id DESC
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_Unboxing_AdminGetDetail(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        post.id AS PostId,
        post.post_no AS PostNo,
        post.user_id AS UserId,
        post.author_name AS AuthorName,
        post.author_avatar AS AuthorAvatarUrl,
        post.title AS Title,
        post.book_title AS BookTitle,
        post.content_text AS ContentText,
        post.product_label AS ProductLabel,
        post.status AS Status,
        post.is_featured AS IsFeatured,
        post.published_at AS PublishedAtUtc,
        post.created_at AS CreatedAtUtc,
        post.updated_at AS UpdatedAtUtc,
        level.level_code AS LevelCode,
        level.level_name AS LevelName,
        level.icon_url AS LevelIconUrl
    FROM community_unboxing_post post
    LEFT JOIN community_unboxing_level level ON level.id = post.level_id
    WHERE post.post_no COLLATE utf8mb4_unicode_ci = p_post_no COLLATE utf8mb4_unicode_ci
    LIMIT 1;
END $$

CREATE PROCEDURE usp_Unboxing_DeletePost(
    IN p_post_no VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    UPDATE community_unboxing_post
    SET status = 3,
        is_featured = 0,
        published_at = NULL,
        updated_at = CURRENT_TIMESTAMP
    WHERE post_no COLLATE utf8mb4_unicode_ci = p_post_no COLLATE utf8mb4_unicode_ci;
END $$

CREATE PROCEDURE usp_Unboxing_GetMedia(
    IN p_post_id BIGINT
)
BEGIN
    SELECT
        media_type AS MediaType,
        storage_url AS StorageUrl,
        thumbnail_url AS ThumbnailUrl,
        sort_order AS SortOrder,
        width AS Width,
        height AS Height
    FROM community_unboxing_media
    WHERE post_id = p_post_id
    ORDER BY sort_order, id;
END $$

CREATE PROCEDURE usp_Unboxing_GetTagsByPostIds(
    IN p_post_ids TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        post_id AS PostId,
        tag_name AS TagName
    FROM community_unboxing_tag
    WHERE FIND_IN_SET(
            CAST(post_id AS CHAR) COLLATE utf8mb4_unicode_ci,
            p_post_ids COLLATE utf8mb4_unicode_ci) > 0
    ORDER BY id;
END $$

CREATE PROCEDURE usp_Unboxing_GetTagsByPostId(
    IN p_post_id BIGINT
)
BEGIN
    SELECT tag_name AS TagName
    FROM community_unboxing_tag
    WHERE post_id = p_post_id
    ORDER BY id;
END $$

CREATE PROCEDURE usp_Unboxing_GetLevels()
BEGIN
    SELECT
        level_code AS LevelCode,
        level_name AS LevelName,
        icon_url AS IconUrl
    FROM community_unboxing_level
    ORDER BY sort_order, id;
END $$

DELIMITER ;
DROP PROCEDURE IF EXISTS usp_UserGallery_UserExists;
DROP PROCEDURE IF EXISTS usp_UserGallery_CountImages;
DROP PROCEDURE IF EXISTS usp_UserGallery_ListImages;
DROP PROCEDURE IF EXISTS usp_UserGallery_GetImageById;
DROP PROCEDURE IF EXISTS usp_UserGallery_GetImageByObject;

DELIMITER $$

CREATE PROCEDURE usp_UserGallery_UserExists(
    IN p_user_id BIGINT
)
BEGIN
    SELECT COUNT(1)
    FROM identity_user
    WHERE id = p_user_id;
END $$

CREATE PROCEDURE usp_UserGallery_CountImages(
    IN p_directory_prefix VARCHAR(512)
)
BEGIN
    SELECT COUNT(1)
    FROM storage_file_object
    WHERE storage_status = 1
      AND (
            directory_path COLLATE utf8mb4_unicode_ci = p_directory_prefix COLLATE utf8mb4_unicode_ci
            OR directory_path COLLATE utf8mb4_unicode_ci LIKE CONCAT(p_directory_prefix COLLATE utf8mb4_unicode_ci, '/%')
          )
      AND (
            content_type LIKE 'image/%'
            OR LOWER(COALESCE(file_extension, '')) IN ('jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg', 'heic', 'heif', 'avif')
          );
END $$

CREATE PROCEDURE usp_UserGallery_ListImages(
    IN p_directory_prefix VARCHAR(512),
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        id AS FileId,
        bucket_name AS Bucket,
        object_key AS ObjectKey,
        file_name AS FileName,
        original_file_name AS OriginalFileName,
        content_type AS ContentType,
        content_length AS ContentLength,
        created_at AS UploadedAtUtc
    FROM storage_file_object
    WHERE storage_status = 1
      AND (
            directory_path COLLATE utf8mb4_unicode_ci = p_directory_prefix COLLATE utf8mb4_unicode_ci
            OR directory_path COLLATE utf8mb4_unicode_ci LIKE CONCAT(p_directory_prefix COLLATE utf8mb4_unicode_ci, '/%')
          )
      AND (
            content_type LIKE 'image/%'
            OR LOWER(COALESCE(file_extension, '')) IN ('jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg', 'heic', 'heif', 'avif')
          )
    ORDER BY created_at DESC, id DESC
    LIMIT p_offset, p_page_size;
END $$

CREATE PROCEDURE usp_UserGallery_GetImageById(
    IN p_file_id BIGINT,
    IN p_directory_prefix VARCHAR(512)
)
BEGIN
    SELECT
        id AS FileId,
        bucket_name AS Bucket,
        object_key AS ObjectKey,
        file_name AS FileName,
        original_file_name AS OriginalFileName,
        content_type AS ContentType,
        content_length AS ContentLength,
        created_at AS UploadedAtUtc
    FROM storage_file_object
    WHERE id = p_file_id
      AND storage_status = 1
      AND (
            directory_path COLLATE utf8mb4_unicode_ci = p_directory_prefix COLLATE utf8mb4_unicode_ci
            OR directory_path COLLATE utf8mb4_unicode_ci LIKE CONCAT(p_directory_prefix COLLATE utf8mb4_unicode_ci, '/%')
          )
      AND (
            content_type LIKE 'image/%'
            OR LOWER(COALESCE(file_extension, '')) IN ('jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg', 'heic', 'heif', 'avif')
          )
    LIMIT 1;
END $$

CREATE PROCEDURE usp_UserGallery_GetImageByObject(
    IN p_bucket_name VARCHAR(128),
    IN p_object_key VARCHAR(512),
    IN p_directory_prefix VARCHAR(512)
)
BEGIN
    SELECT
        id AS FileId,
        bucket_name AS Bucket,
        object_key AS ObjectKey,
        file_name AS FileName,
        original_file_name AS OriginalFileName,
        content_type AS ContentType,
        content_length AS ContentLength,
        created_at AS UploadedAtUtc
    FROM storage_file_object
    WHERE bucket_name COLLATE utf8mb4_unicode_ci = p_bucket_name COLLATE utf8mb4_unicode_ci
      AND object_key COLLATE utf8mb4_unicode_ci = p_object_key COLLATE utf8mb4_unicode_ci
      AND storage_status = 1
      AND (
            directory_path COLLATE utf8mb4_unicode_ci = p_directory_prefix COLLATE utf8mb4_unicode_ci
            OR directory_path COLLATE utf8mb4_unicode_ci LIKE CONCAT(p_directory_prefix COLLATE utf8mb4_unicode_ci, '/%')
          )
      AND (
            content_type LIKE 'image/%'
            OR LOWER(COALESCE(file_extension, '')) IN ('jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg', 'heic', 'heif', 'avif')
          )
    LIMIT 1;
END $$

DELIMITER ;
CREATE TABLE IF NOT EXISTS storage_file_object (
    id BIGINT NOT NULL AUTO_INCREMENT,
    bucket_name VARCHAR(128) NOT NULL COMMENT '对象存储桶',
    object_key VARCHAR(512) NOT NULL COMMENT '对象存储键',
    directory_path VARCHAR(512) NULL COMMENT '对象目录',
    file_name VARCHAR(256) NOT NULL COMMENT '当前文件名',
    original_file_name VARCHAR(256) NULL COMMENT '客户端原始文件名',
    file_extension VARCHAR(32) NULL COMMENT '文件扩展名',
    content_type VARCHAR(128) NULL COMMENT 'MIME 类型',
    content_length BIGINT NOT NULL COMMENT '文件大小(字节)',
    storage_provider VARCHAR(32) NOT NULL DEFAULT 'seaweedfs-s3' COMMENT '存储提供方',
    storage_status TINYINT NOT NULL DEFAULT 1 COMMENT '1=active 2=deleted',
    access_count INT NOT NULL DEFAULT 0 COMMENT '访问链接生成次数',
    last_accessed_at DATETIME NULL COMMENT '最近一次生成访问链接时间',
    last_access_expires_at DATETIME NULL COMMENT '最近一次访问链接过期时间',
    deleted_at DATETIME NULL COMMENT '逻辑删除时间',
    created_by_ip VARCHAR(64) NULL COMMENT '上传来源 IP',
    created_by_user_agent VARCHAR(512) NULL COMMENT '上传来源 UA',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_bucket_object_key (bucket_name, object_key),
    KEY idx_status_created_at (storage_status, created_at),
    KEY idx_bucket_directory (bucket_name, directory_path)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='文件对象元数据';

CREATE TABLE IF NOT EXISTS storage_file_operation_log (
    id BIGINT NOT NULL AUTO_INCREMENT,
    file_id BIGINT NULL COMMENT '关联文件主键',
    bucket_name VARCHAR(128) NOT NULL COMMENT '对象存储桶',
    object_key VARCHAR(512) NOT NULL COMMENT '对象存储键',
    operation_type VARCHAR(32) NOT NULL COMMENT 'upload/presigned_read/delete',
    operation_status TINYINT NOT NULL DEFAULT 1 COMMENT '1=success 2=not_found',
    access_url_expires_at DATETIME NULL COMMENT '访问链接过期时间',
    client_ip VARCHAR(64) NULL COMMENT '请求来源 IP',
    client_user_agent VARCHAR(512) NULL COMMENT '请求来源 UA',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_file_operation (file_id, operation_type, created_at),
    KEY idx_bucket_object_created (bucket_name, object_key, created_at),
    CONSTRAINT fk_storage_file_operation_log_file_id
        FOREIGN KEY (file_id) REFERENCES storage_file_object (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='文件对象操作日志';

DROP PROCEDURE IF EXISTS usp_FileObject_GetMetadata;
DROP PROCEDURE IF EXISTS usp_FileObject_SaveUpload;
DROP PROCEDURE IF EXISTS usp_FileObject_RecordAccess;
DROP PROCEDURE IF EXISTS usp_FileObject_MarkDeleted;

DELIMITER $$

CREATE PROCEDURE usp_FileObject_GetMetadata(
    IN p_bucket_name VARCHAR(128),
    IN p_object_key VARCHAR(512)
)
BEGIN
    SELECT
        original_file_name AS OriginalFileName,
        file_name AS FileName,
        file_extension AS FileExtension,
        content_type AS ContentType,
        content_length AS ContentLength
    FROM storage_file_object
    WHERE BINARY bucket_name = BINARY p_bucket_name
      AND BINARY object_key = BINARY p_object_key
      AND storage_status = 1
    LIMIT 1;
END $$

CREATE PROCEDURE usp_FileObject_SaveUpload(
    IN p_bucket_name VARCHAR(128),
    IN p_object_key VARCHAR(512),
    IN p_directory_path VARCHAR(512),
    IN p_file_name VARCHAR(256),
    IN p_original_file_name VARCHAR(256),
    IN p_file_extension VARCHAR(32),
    IN p_content_type VARCHAR(128),
    IN p_content_length BIGINT,
    IN p_storage_provider VARCHAR(32),
    IN p_client_ip VARCHAR(64),
    IN p_client_user_agent VARCHAR(512)
)
BEGIN
    DECLARE v_file_id BIGINT;

    INSERT INTO storage_file_object (
        bucket_name,
        object_key,
        directory_path,
        file_name,
        original_file_name,
        file_extension,
        content_type,
        content_length,
        storage_provider,
        storage_status,
        deleted_at,
        created_by_ip,
        created_by_user_agent
    ) VALUES (
        p_bucket_name,
        p_object_key,
        p_directory_path,
        p_file_name,
        p_original_file_name,
        p_file_extension,
        p_content_type,
        p_content_length,
        COALESCE(NULLIF(p_storage_provider, ''), 'seaweedfs-s3'),
        1,
        NULL,
        p_client_ip,
        p_client_user_agent
    )
    ON DUPLICATE KEY UPDATE
        id = LAST_INSERT_ID(id),
        directory_path = VALUES(directory_path),
        file_name = VALUES(file_name),
        original_file_name = COALESCE(VALUES(original_file_name), original_file_name),
        file_extension = VALUES(file_extension),
        content_type = VALUES(content_type),
        content_length = VALUES(content_length),
        storage_provider = VALUES(storage_provider),
        storage_status = 1,
        deleted_at = NULL,
        updated_at = CURRENT_TIMESTAMP,
        created_by_ip = COALESCE(VALUES(created_by_ip), created_by_ip),
        created_by_user_agent = COALESCE(VALUES(created_by_user_agent), created_by_user_agent);

    SET v_file_id = LAST_INSERT_ID();

    INSERT INTO storage_file_operation_log (
        file_id,
        bucket_name,
        object_key,
        operation_type,
        operation_status,
        client_ip,
        client_user_agent
    ) VALUES (
        v_file_id,
        p_bucket_name,
        p_object_key,
        'upload',
        1,
        p_client_ip,
        p_client_user_agent
    );

    SELECT v_file_id AS FileId, 1 AS Affected;
END $$

CREATE PROCEDURE usp_FileObject_RecordAccess(
    IN p_bucket_name VARCHAR(128),
    IN p_object_key VARCHAR(512),
    IN p_operation_type VARCHAR(32),
    IN p_access_url_expires_at DATETIME,
    IN p_client_ip VARCHAR(64),
    IN p_client_user_agent VARCHAR(512)
)
BEGIN
    DECLARE v_file_id BIGINT DEFAULT NULL;

    SELECT id
      INTO v_file_id
      FROM storage_file_object
         WHERE bucket_name COLLATE utf8mb4_unicode_ci = p_bucket_name COLLATE utf8mb4_unicode_ci
             AND object_key COLLATE utf8mb4_unicode_ci = p_object_key COLLATE utf8mb4_unicode_ci
       AND storage_status = 1
     LIMIT 1;

    IF v_file_id IS NOT NULL THEN
        UPDATE storage_file_object
           SET access_count = access_count + 1,
               last_accessed_at = CURRENT_TIMESTAMP,
               last_access_expires_at = p_access_url_expires_at,
               updated_at = CURRENT_TIMESTAMP
         WHERE id = v_file_id;
    END IF;

    INSERT INTO storage_file_operation_log (
        file_id,
        bucket_name,
        object_key,
        operation_type,
        operation_status,
        access_url_expires_at,
        client_ip,
        client_user_agent
    ) VALUES (
        v_file_id,
        p_bucket_name,
        p_object_key,
        COALESCE(NULLIF(p_operation_type, ''), 'presigned_read'),
        IF(v_file_id IS NULL, 2, 1),
        p_access_url_expires_at,
        p_client_ip,
        p_client_user_agent
    );

    SELECT COALESCE(v_file_id, 0) AS FileId, IF(v_file_id IS NULL, 0, 1) AS FileExists;
END $$

CREATE PROCEDURE usp_FileObject_MarkDeleted(
    IN p_bucket_name VARCHAR(128),
    IN p_object_key VARCHAR(512),
    IN p_client_ip VARCHAR(64),
    IN p_client_user_agent VARCHAR(512)
)
BEGIN
    DECLARE v_file_id BIGINT DEFAULT NULL;

    SELECT id
      INTO v_file_id
      FROM storage_file_object
         WHERE bucket_name COLLATE utf8mb4_unicode_ci = p_bucket_name COLLATE utf8mb4_unicode_ci
             AND object_key COLLATE utf8mb4_unicode_ci = p_object_key COLLATE utf8mb4_unicode_ci
     LIMIT 1;

    IF v_file_id IS NOT NULL THEN
        UPDATE storage_file_object
           SET storage_status = 2,
               deleted_at = CURRENT_TIMESTAMP,
               updated_at = CURRENT_TIMESTAMP
         WHERE id = v_file_id;
    END IF;

    INSERT INTO storage_file_operation_log (
        file_id,
        bucket_name,
        object_key,
        operation_type,
        operation_status,
        client_ip,
        client_user_agent
    ) VALUES (
        v_file_id,
        p_bucket_name,
        p_object_key,
        'delete',
        IF(v_file_id IS NULL, 2, 1),
        p_client_ip,
        p_client_user_agent
    );

    SELECT COALESCE(v_file_id, 0) AS FileId, IF(v_file_id IS NULL, 0, 1) AS Affected;
END $$

DELIMITER ;
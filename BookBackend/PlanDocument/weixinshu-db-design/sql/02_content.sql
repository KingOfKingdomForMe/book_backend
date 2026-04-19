-- ============================================================
-- 心书 (weixinshu.com) 数据库设计 — 内容导入域 (content_*)
-- ============================================================

-- ----- 1. 内容来源账号 (用户在外部平台的内容账号) -----
CREATE TABLE content_source_account (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL                 COMMENT '-> identity_user.id',
    platform        VARCHAR(32)     NOT NULL                 COMMENT 'wechat_moments / weibo / manual 等',
    external_identity_id BIGINT     NULL                     COMMENT '-> identity_external_identity.id (外部导入时)',
    account_name    VARCHAR(128)    NULL                     COMMENT '来源账号名称/昵称',
    status          TINYINT         NOT NULL DEFAULT 1       COMMENT '1=可用 2=授权过期 3=解绑',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_id (user_id),
    CONSTRAINT fk_source_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='内容来源账号';

-- ----- 2. 内容导入任务 -----
CREATE TABLE content_import_job (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL,
    source_account_id BIGINT        NOT NULL                 COMMENT '-> content_source_account.id',
    platform        VARCHAR(32)     NOT NULL,
    status          TINYINT         NOT NULL DEFAULT 0       COMMENT '0=待处理 1=导入中 2=已完成 3=失败 4=已取消',
    date_range_start DATE           NULL                     COMMENT '导入起始日期(可选)',
    date_range_end  DATE            NULL                     COMMENT '导入截止日期(可选)',
    total_posts     INT             NULL                     COMMENT '导入帖子总数',
    total_media     INT             NULL                     COMMENT '导入媒体总数',
    error_message   VARCHAR(1000)   NULL,
    started_at      DATETIME        NULL,
    completed_at    DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_status (user_id, status),
    CONSTRAINT fk_import_user FOREIGN KEY (user_id) REFERENCES identity_user (id),
    CONSTRAINT fk_import_source FOREIGN KEY (source_account_id) REFERENCES content_source_account (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='内容导入任务';

-- ----- 3. 导入/创建的图文条目 -----
CREATE TABLE content_post (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL,
    import_job_id   BIGINT          NULL                     COMMENT '-> content_import_job.id (外部导入时非 NULL)',
    source_platform VARCHAR(32)     NOT NULL                 COMMENT 'wechat_moments / weibo / manual_text / manual_photo',
    source_post_id  VARCHAR(128)    NULL                     COMMENT '外部平台原始帖子ID(用于去重)',
    content_text    TEXT            NULL                     COMMENT '文字内容',
    posted_at       DATETIME        NULL                     COMMENT '原始发布时间',
    visibility      VARCHAR(16)     NOT NULL DEFAULT 'public' COMMENT 'public / friends / private',
    location        VARCHAR(256)    NULL                     COMMENT '地理位置文本',
    media_count     SMALLINT        NOT NULL DEFAULT 0       COMMENT '附带媒体数量',
    extra_metadata  JSON            NULL                     COMMENT '扩展元数据(平台特有字段)',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_source_dedup (user_id, source_platform, source_post_id),
    INDEX idx_user_posted (user_id, posted_at DESC),
    CONSTRAINT fk_post_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='标准化图文条目';

-- ----- 4. 帖子媒体 (图片/视频) -----
CREATE TABLE content_post_media (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    post_id         BIGINT          NOT NULL                 COMMENT '-> content_post.id',
    sort_order      SMALLINT        NOT NULL DEFAULT 0       COMMENT '排序序号',
    media_type      VARCHAR(16)     NOT NULL                 COMMENT 'image / video',
    storage_url     VARCHAR(1024)   NOT NULL                 COMMENT '对象存储地址',
    thumbnail_url   VARCHAR(1024)   NULL                     COMMENT '缩略图地址',
    width           INT             NULL,
    height          INT             NULL,
    file_size       BIGINT          NULL                     COMMENT '字节数',
    file_hash       VARCHAR(64)     NULL                     COMMENT 'SHA-256 哈希(去重)',
    original_url    VARCHAR(1024)   NULL                     COMMENT '外部平台原始 URL',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_post_id (post_id),
    CONSTRAINT fk_media_post FOREIGN KEY (post_id) REFERENCES content_post (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='帖子媒体';

-- ----- 5. 帖子标签 -----
CREATE TABLE content_post_tag (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    post_id         BIGINT          NOT NULL,
    tag_name        VARCHAR(64)     NOT NULL                 COMMENT '标签文本',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_post_id (post_id),
    INDEX idx_tag_name (tag_name),
    CONSTRAINT fk_tag_post FOREIGN KEY (post_id) REFERENCES content_post (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='帖子标签';

-- ----- 6. 站内手动上传素材 -----
CREATE TABLE content_user_upload (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    user_id         BIGINT          NOT NULL,
    media_type      VARCHAR(16)     NOT NULL                 COMMENT 'image / video / document',
    storage_url     VARCHAR(1024)   NOT NULL,
    thumbnail_url   VARCHAR(1024)   NULL,
    file_name       VARCHAR(256)    NULL,
    file_size       BIGINT          NULL,
    file_hash       VARCHAR(64)     NULL,
    width           INT             NULL,
    height          INT             NULL,
    purpose         VARCHAR(32)     NOT NULL DEFAULT 'book_content' COMMENT 'book_content / cover_custom / avatar',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_user_purpose (user_id, purpose),
    CONSTRAINT fk_upload_user FOREIGN KEY (user_id) REFERENCES identity_user (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='站内手动上传素材';

-- ============================================================
-- Public album preview extension for book_* tables
-- Depends on:
--   1. PlanDocument/weixinshu-db-design/sql/03_book.sql
--   2. ThreeBooks.BookBackend.Infrastructure/Sql/Files/01_file_storage.sql
-- ============================================================

ALTER TABLE book_project
    ADD COLUMN share_code VARCHAR(32) NULL COMMENT 'Public share code',
    ADD COLUMN is_public TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether preview is public',
    ADD COLUMN shared_version_id BIGINT NULL COMMENT 'Frozen version used for public preview',
    ADD COLUMN shared_at DATETIME NULL COMMENT 'When preview was published',
    ADD COLUMN view_count BIGINT NOT NULL DEFAULT 0 COMMENT 'Aggregated preview views',
    ADD COLUMN share_count BIGINT NOT NULL DEFAULT 0 COMMENT 'Aggregated preview shares',
    ADD UNIQUE KEY uk_book_project_share_code (share_code),
    ADD KEY idx_book_project_public_share (is_public, share_code);

ALTER TABLE book_project
    ADD CONSTRAINT fk_book_project_shared_version
        FOREIGN KEY (shared_version_id) REFERENCES book_project_version (id);

CREATE TABLE book_project_version_page (
    id                  BIGINT          NOT NULL AUTO_INCREMENT,
    project_version_id  BIGINT          NOT NULL COMMENT 'FK to book_project_version.id',
    page_no             INT             NOT NULL COMMENT 'Physical page number in shared version',
    page_label          VARCHAR(64)     NOT NULL COMMENT 'Display label such as cover/preface/1',
    page_type           VARCHAR(32)     NOT NULL COMMENT 'cover/title/acknowledgment/preface/content/ending',
    sort_order          INT             NOT NULL DEFAULT 0,
    json_source         LONGTEXT        NULL COMMENT 'Direct frontend JSON source text',
    json_file_id        BIGINT          NULL COMMENT 'Optional legacy JSON file reference',
    json_bucket         VARCHAR(128)    NULL,
    json_object_key     VARCHAR(512)    NULL,
    html_file_id        BIGINT          NULL COMMENT 'Optional rendered HTML file',
    html_bucket         VARCHAR(128)    NULL,
    html_object_key     VARCHAR(512)    NULL,
    thumbnail_file_id   BIGINT          NULL COMMENT 'Optional page thumbnail file',
    thumbnail_bucket    VARCHAR(128)    NULL,
    thumbnail_object_key VARCHAR(512)   NULL,
    page_width          INT             NULL,
    page_height         INT             NULL,
    schema_version      VARCHAR(16)     NOT NULL DEFAULT '1.0',
    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_book_project_version_page (project_version_id, page_no),
    KEY idx_book_project_version_page_sort (project_version_id, sort_order),
    CONSTRAINT fk_book_project_version_page_version
        FOREIGN KEY (project_version_id) REFERENCES book_project_version (id),
    CONSTRAINT fk_book_project_version_page_json_file
        FOREIGN KEY (json_file_id) REFERENCES storage_file_object (id),
    CONSTRAINT fk_book_project_version_page_html_file
        FOREIGN KEY (html_file_id) REFERENCES storage_file_object (id),
    CONSTRAINT fk_book_project_version_page_thumbnail_file
        FOREIGN KEY (thumbnail_file_id) REFERENCES storage_file_object (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Frozen preview page content and related file references';

CREATE TABLE book_project_version_page_asset (
    id              BIGINT          NOT NULL AUTO_INCREMENT,
    version_page_id BIGINT          NOT NULL COMMENT 'FK to book_project_version_page.id',
    sort_order      INT             NOT NULL DEFAULT 0,
    role            VARCHAR(32)     NOT NULL COMMENT 'hero/gallery/background/inline/etc.',
    file_id         BIGINT          NOT NULL COMMENT 'FK to storage_file_object.id',
    bucket_name     VARCHAR(128)    NOT NULL,
    object_key      VARCHAR(512)    NOT NULL,
    width           INT             NULL,
    height          INT             NULL,
    alt_text        VARCHAR(256)    NULL,
    caption         VARCHAR(256)    NULL,
    crop_json       JSON            NULL,
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_book_project_version_page_asset_sort (version_page_id, sort_order),
    CONSTRAINT fk_book_project_version_page_asset_page
        FOREIGN KEY (version_page_id) REFERENCES book_project_version_page (id),
    CONSTRAINT fk_book_project_version_page_asset_file
        FOREIGN KEY (file_id) REFERENCES storage_file_object (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Frozen preview page image references';

CREATE TABLE book_project_view_log (
    id                  BIGINT          NOT NULL AUTO_INCREMENT,
    project_id          BIGINT          NOT NULL,
    share_code          VARCHAR(32)     NOT NULL,
    page_no             INT             NULL,
    client_ip           VARCHAR(64)     NULL,
    client_user_agent   VARCHAR(512)    NULL,
    referrer            VARCHAR(1024)   NULL,
    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_book_project_view_log_project_created (project_id, created_at),
    KEY idx_book_project_view_log_share_created (share_code, created_at),
    CONSTRAINT fk_book_project_view_log_project
        FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Public preview view log';

CREATE TABLE book_project_share_log (
    id                  BIGINT          NOT NULL AUTO_INCREMENT,
    project_id          BIGINT          NOT NULL,
    share_code          VARCHAR(32)     NOT NULL,
    channel             VARCHAR(32)     NOT NULL,
    client_ip           VARCHAR(64)     NULL,
    client_user_agent   VARCHAR(512)    NULL,
    created_at          DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_book_project_share_log_project_created (project_id, created_at),
    KEY idx_book_project_share_log_share_channel_created (share_code, channel, created_at),
    CONSTRAINT fk_book_project_share_log_project
        FOREIGN KEY (project_id) REFERENCES book_project (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Public preview share log';
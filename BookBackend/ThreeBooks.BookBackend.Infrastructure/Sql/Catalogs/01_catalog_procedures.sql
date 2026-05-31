DROP PROCEDURE IF EXISTS usp_Catalog_GetCategories_Count;
DROP PROCEDURE IF EXISTS usp_Catalog_GetCategories_List;
DROP PROCEDURE IF EXISTS usp_Catalog_GetCategoryProducts_ByCategoryIds;
DROP PROCEDURE IF EXISTS usp_Catalog_GetProducts_Count;
DROP PROCEDURE IF EXISTS usp_Catalog_GetProducts_List;
DROP PROCEDURE IF EXISTS usp_Catalog_GetProductDetail_Header;
DROP PROCEDURE IF EXISTS usp_Catalog_GetProductDetail_Skus;
DROP PROCEDURE IF EXISTS usp_Catalog_GetBundles_Count;
DROP PROCEDURE IF EXISTS usp_Catalog_GetBundles_List;

DELIMITER $$

CREATE PROCEDURE usp_Catalog_GetCategories_Count()
BEGIN
    SELECT COUNT(*)
    FROM catalog_category c
    WHERE c.is_active = 1;
END $$

CREATE PROCEDURE usp_Catalog_GetCategories_List(
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        c.id AS Id,
        c.name AS Name,
        c.slug AS Slug,
        c.sort_order AS SortOrder,
        COUNT(spu.id) AS ProductCount
    FROM catalog_category c
    LEFT JOIN catalog_product_spu spu ON spu.category_id = c.id AND spu.is_active = 1
    WHERE c.is_active = 1
    GROUP BY c.id, c.name, c.slug, c.sort_order
    ORDER BY c.sort_order, c.id
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_Catalog_GetCategoryProducts_ByCategoryIds(
    IN p_category_ids TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT
        spu.id AS Id,
        spu.category_id AS CategoryId,
        category.name AS CategoryName,
        category.slug AS CategorySlug,
        spu.spu_code AS SpuCode,
        spu.name AS Name,
        spu.subtitle AS Subtitle,
        spu.content_source AS ContentSource,
        COALESCE(price.starting_price, 0) AS StartingPrice,
        spu.sort_order AS SortOrder
    FROM catalog_product_spu spu
    INNER JOIN catalog_category category ON category.id = spu.category_id AND category.is_active = 1
    LEFT JOIN (
        SELECT
            sku.spu_id,
            MIN(pr.base_price + (pr.page_unit_price * sku.min_pages)) AS starting_price
        FROM catalog_product_sku sku
        INNER JOIN catalog_price_rule pr ON pr.sku_id = sku.id
            AND pr.is_active = 1
            AND pr.effective_from <= UTC_TIMESTAMP()
            AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
        WHERE sku.is_active = 1
        GROUP BY sku.spu_id
    ) price ON price.spu_id = spu.id
    WHERE spu.is_active = 1
    AND FIND_IN_SET(
        CAST(spu.category_id AS CHAR) COLLATE utf8mb4_unicode_ci,
        p_category_ids COLLATE utf8mb4_unicode_ci) > 0
    ORDER BY category.sort_order, spu.sort_order, spu.id;
END $$

CREATE PROCEDURE usp_Catalog_GetProducts_Count(
    IN p_category_id BIGINT,
    IN p_category_slug VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
)
BEGIN
    SELECT COUNT(*)
    FROM catalog_product_spu spu
    INNER JOIN catalog_category category ON category.id = spu.category_id AND category.is_active = 1
    WHERE spu.is_active = 1
        AND (p_category_id IS NULL OR spu.category_id = p_category_id)
        AND (p_category_slug IS NULL OR category.slug COLLATE utf8mb4_unicode_ci = p_category_slug COLLATE utf8mb4_unicode_ci)
      AND (
            p_keyword IS NULL
            OR spu.spu_code COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
            OR spu.name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
            OR spu.subtitle COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
          );
END $$

CREATE PROCEDURE usp_Catalog_GetProducts_List(
    IN p_category_id BIGINT,
    IN p_category_slug VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_keyword VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        spu.id AS Id,
        spu.category_id AS CategoryId,
        category.name AS CategoryName,
        category.slug AS CategorySlug,
        spu.spu_code AS SpuCode,
        default_album.album_code AS DefaultAlbumCode,
        spu.name AS Name,
        spu.subtitle AS Subtitle,
        spu.content_source AS ContentSource,
        COALESCE(price.starting_price, 0) AS StartingPrice,
        COALESCE(image_limits.upload_image_count, 0) AS UploadImageCount,
        spu.sort_order AS SortOrder
    FROM catalog_product_spu spu
    INNER JOIN catalog_category category ON category.id = spu.category_id AND category.is_active = 1
    LEFT JOIN (
        SELECT
            sku.spu_id,
            MIN(pr.base_price + (pr.page_unit_price * sku.min_pages)) AS starting_price
        FROM catalog_product_sku sku
        INNER JOIN catalog_price_rule pr ON pr.sku_id = sku.id
            AND pr.is_active = 1
            AND pr.effective_from <= UTC_TIMESTAMP()
            AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
        WHERE sku.is_active = 1
        GROUP BY sku.spu_id
    ) price ON price.spu_id = spu.id
    LEFT JOIN (
        SELECT
            sku.spu_id,
            MAX(COALESCE(sku.max_pages, sku.min_pages, 0)) AS upload_image_count
        FROM catalog_product_sku sku
        WHERE sku.is_active = 1
        GROUP BY sku.spu_id
    ) image_limits ON image_limits.spu_id = spu.id
        LEFT JOIN default_album default_album ON default_album.product_spu_id = spu.id AND default_album.is_active = 1
    WHERE spu.is_active = 1
        AND (p_category_id IS NULL OR spu.category_id = p_category_id)
        AND (p_category_slug IS NULL OR category.slug COLLATE utf8mb4_unicode_ci = p_category_slug COLLATE utf8mb4_unicode_ci)
      AND (
            p_keyword IS NULL
            OR spu.spu_code COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
            OR spu.name COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
            OR spu.subtitle COLLATE utf8mb4_unicode_ci LIKE CONCAT('%', p_keyword, '%') COLLATE utf8mb4_unicode_ci
          )
    ORDER BY category.sort_order, spu.sort_order, spu.id
    LIMIT p_page_size OFFSET p_offset;
END $$

CREATE PROCEDURE usp_Catalog_GetProductDetail_Header(
    IN p_spu_id BIGINT
)
BEGIN
    SELECT
        spu.id AS Id,
        spu.category_id AS CategoryId,
        spu.spu_code AS SpuCode,
        default_album.album_code AS DefaultAlbumCode,
        spu.name AS Name,
        spu.subtitle AS Subtitle,
        spu.content_source AS ContentSource
    FROM catalog_product_spu spu
    LEFT JOIN default_album default_album ON default_album.product_spu_id = spu.id AND default_album.is_active = 1
    WHERE spu.id = p_spu_id
      AND spu.is_active = 1;
END $$

CREATE PROCEDURE usp_Catalog_GetProductDetail_Skus(
    IN p_spu_id BIGINT
)
BEGIN
    SELECT
        sku.id AS Id,
        sku.sku_code AS SkuCode,
        size_value.value_label AS SizeLabel,
        binding_value.value_label AS BindingLabel,
        layout_value.value_label AS LayoutLabel,
        sku.min_pages AS MinPages,
        sku.max_pages AS MaxPages,
        COALESCE(price.base_price, 0) AS BasePrice,
        COALESCE(price.page_unit_price, 0) AS PageUnitPrice
    FROM catalog_product_sku sku
    LEFT JOIN catalog_spec_value size_value ON size_value.id = sku.size_value_id
    LEFT JOIN catalog_spec_value binding_value ON binding_value.id = sku.binding_value_id
    LEFT JOIN catalog_spec_value layout_value ON layout_value.id = sku.layout_value_id
    LEFT JOIN catalog_price_rule price ON price.id = (
        SELECT pr.id
        FROM catalog_price_rule pr
        WHERE pr.sku_id = sku.id
          AND pr.is_active = 1
          AND pr.effective_from <= UTC_TIMESTAMP()
          AND (pr.effective_to IS NULL OR pr.effective_to > UTC_TIMESTAMP())
        ORDER BY pr.effective_from DESC, pr.id DESC
        LIMIT 1
    )
    WHERE sku.spu_id = p_spu_id
      AND sku.is_active = 1
    ORDER BY sku.id;
END $$

CREATE PROCEDURE usp_Catalog_GetBundles_Count()
BEGIN
    SELECT COUNT(*)
    FROM catalog_bundle
    WHERE is_active = 1;
END $$

CREATE PROCEDURE usp_Catalog_GetBundles_List(
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    SELECT
        id AS Id,
        bundle_code AS BundleCode,
        name AS Name,
        description AS Description,
        bundle_price AS BundlePrice,
        COALESCE(original_price, bundle_price) AS OriginalPrice,
        sort_order AS SortOrder
    FROM catalog_bundle
    WHERE is_active = 1
    ORDER BY sort_order, id
    LIMIT p_page_size OFFSET p_offset;
END $$

DELIMITER ;
-- =============================================================================
-- Dapper Best Practice (.NET 4.6 + MySQL) — 資料庫 Schema
-- 請先建立資料庫後再執行此腳本：CREATE DATABASE your_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
--
-- MySQL 版本說明：
--   • MySQL 5.5.3+：支援 utf8mb4 字元集。
--   • 所有時間欄位統一使用 DATETIME（非 TIMESTAMP），避免 2038 年溢位及
--     時區自動轉換問題，確保製造業本地事件時間的一致性。
--   • MySQL 5.7+：可直接使用 BOOLEAN (TINYINT(1) 別名)，行為一致。
-- =============================================================================
-- ★ 執行順序說明
-- =============================================================================
-- 本檔案僅包含本專案 Repository 直接管理的 9 張核心資料表。
-- 既有系統整合資料表（lots_info 等）的 DDL 請參閱 schema-legacy.sql。
--
-- 執行前請確認外部依賴 lots_info 已存在於資料庫中：
--   若為全新環境，請先執行 schema-legacy.sql 建立整合資料表。
--
-- =============================================================================
-- ★ 本專案核心資料表（Repository 直接管理，共 9 張）
-- =============================================================================
--   1. detection_methods            — 偵測方法主表（TINYINT PK，固定 4 筆種子資料）
--   2. anomaly_lots                 — 異常批號主表（FK → lots_info, detection_methods）
--   3. anomaly_test_items           — 異常測項明細表（FK → anomaly_lots）
--   4. anomaly_units                — 異常 Unit 明細表（FK → anomaly_test_items）
--   5. anomaly_lot_process_mapping  — 批號 Process Mapping 表（FK → anomaly_lots）
--   6. anomaly_unit_process_mapping — Unit Process Mapping 表（FK → anomaly_units）
--   7. detection_specs              — Spec 規格表（FK → detection_methods）
--   8. site_test_statistics         — Site 測項統計值表（FK → lots_info）
--   9. good_lots                    — 好批批號記錄表（FK → lots_info, detection_methods）
--
-- =============================================================================
-- ★ 外部依賴資料表（需由既有系統提供，本專案不負責建立）
-- =============================================================================
--   ◆ 關鍵 FK 依賴（執行本檔前必須先存在）：
--      • lots_info                   — 批號主資料（anomaly_lots / site_test_statistics /
--                                      good_lots 均以其 id 為外鍵）
--
--   ◆ 既有系統整合資料表的完整 DDL 請參閱 schema-legacy.sql。
-- =============================================================================

-- 1. 偵測方法主表
CREATE TABLE detection_methods (
    id TINYINT PRIMARY KEY AUTO_INCREMENT,
    method_code VARCHAR(20) UNIQUE NOT NULL,
    method_name VARCHAR(50) NOT NULL,
    has_test_item BOOLEAN DEFAULT FALSE,
    has_unit_level BOOLEAN DEFAULT FALSE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO detection_methods (method_code, method_name, has_test_item, has_unit_level) VALUES
('YIELD',     '良率偵測',       FALSE, FALSE),
('STD',       '標準差偵測',     TRUE,  FALSE),
('MEAN',      '平均值偵測',     FALSE, TRUE),
('SITE_MEAN', 'Site平均值偵測', TRUE,  FALSE);

-- =============================================================================
-- ★ 以下為本專案核心資料表（Repository 直接管理，共 8 張）
-- =============================================================================
-- 注意：下列資料表的外鍵依賴 lots_info(id)，須先確認 lots_info 已存在於您的系統中。
-- =============================================================================

-- 2. 異常批號主表
--    注意：lots_info(id) 須已存在於您的系統中
CREATE TABLE anomaly_lots (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    lots_info_id INT(11) NOT NULL,
    detection_method_id TINYINT NOT NULL,
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NULL,
    spec_calc_end_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_created_at (created_at),
    UNIQUE INDEX unq_lot_method (lots_info_id, detection_method_id),
    CONSTRAINT fk_anomaly_lots_info
        FOREIGN KEY (lots_info_id) REFERENCES lots_info(id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_anomaly_lots_detection_method
        FOREIGN KEY (detection_method_id)
        REFERENCES detection_methods(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. 異常測項明細表
CREATE TABLE anomaly_test_items (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    anomaly_lot_id BIGINT NOT NULL,
    test_item_name VARCHAR(100) NOT NULL,
    detection_value DECIMAL(18,9),
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NULL,
    spec_calc_end_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_test_item_name (test_item_name),
    UNIQUE INDEX unq_lot_item (anomaly_lot_id, test_item_name),
    CONSTRAINT fk_test_items_anomaly_lot
        FOREIGN KEY (anomaly_lot_id)
        REFERENCES anomaly_lots(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. 異常 Unit 明細表
CREATE TABLE anomaly_units (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    anomaly_test_item_id BIGINT NOT NULL,
    unit_id VARCHAR(50) NOT NULL,
    detection_value DECIMAL(18,9),
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NULL,
    spec_calc_end_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_unit (unit_id),
    UNIQUE INDEX unq_item_unit (anomaly_test_item_id, unit_id),
    CONSTRAINT fk_units_test_item
        FOREIGN KEY (anomaly_test_item_id)
        REFERENCES anomaly_test_items(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. 批號 Process Mapping（站點 & 機台）
CREATE TABLE anomaly_lot_process_mapping (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    anomaly_lot_id BIGINT NOT NULL,
    station_name VARCHAR(100) NOT NULL,
    equipment_id VARCHAR(50) NOT NULL,
    process_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_lot (anomaly_lot_id),
    INDEX idx_equipment (equipment_id),
    INDEX idx_station_equipment (station_name, equipment_id),
    CONSTRAINT fk_lot_process_anomaly_lot
        FOREIGN KEY (anomaly_lot_id)
        REFERENCES anomaly_lots(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Unit Process Mapping（Boat ID & XY 座標）
CREATE TABLE anomaly_unit_process_mapping (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    anomaly_unit_id BIGINT NOT NULL,
    boat_id VARCHAR(50) NOT NULL,
    position_x SMALLINT NOT NULL,
    position_y SMALLINT NOT NULL,
    process_time DATETIME NULL,
    station_name VARCHAR(100),
    equipment_id VARCHAR(50),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_unit (anomaly_unit_id),
    INDEX idx_boat_position (boat_id, position_x, position_y),
    INDEX idx_station_equipment (station_name, equipment_id),
    CONSTRAINT fk_unit_process_anomaly_unit
        FOREIGN KEY (anomaly_unit_id)
        REFERENCES anomaly_units(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. Spec 規格表
CREATE TABLE detection_specs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    program VARCHAR(100) NOT NULL,
    test_item_name VARCHAR(100),
    site_id INT(11) UNSIGNED NOT NULL,
    detection_method_id TINYINT NOT NULL,
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NOT NULL,
    spec_calc_end_time DATETIME NOT NULL,
    spec_calc_mean       DECIMAL(18,9) NULL,
    spec_calc_std        DECIMAL(18,9) NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_program_method (program, detection_method_id),
    INDEX idx_program_item_method (program, test_item_name, detection_method_id),
    INDEX idx_calc_end_time (spec_calc_end_time),
    INDEX idx_calc_time (spec_calc_start_time, spec_calc_end_time),
    INDEX idx_site_id (site_id),
    CONSTRAINT fk_specs_detection_method
        FOREIGN KEY (detection_method_id)
        REFERENCES detection_methods(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 8. Site 測項統計值表
CREATE TABLE site_test_statistics (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    lots_info_id INT NOT NULL,
    program VARCHAR(100) NOT NULL,
    site_id INT(11) UNSIGNED NOT NULL,
    test_item_name VARCHAR(100) NOT NULL,
    mean_value DECIMAL(18,9),
    max_value DECIMAL(18,9),
    min_value DECIMAL(18,9),
    std_value DECIMAL(18,9),
    cp_value DECIMAL(18,9),
    cpk_value DECIMAL(18,9),
    tester_id VARCHAR(50),
    start_time DATETIME NULL,
    end_time   DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_site (site_id),
    INDEX idx_program_item (program, test_item_name),
    INDEX idx_start_time (start_time),
    INDEX idx_program_site_item_time (program, site_id, test_item_name, start_time),
    UNIQUE INDEX unq_lot_site_item (lots_info_id, site_id, test_item_name),
    CONSTRAINT fk_site_test_statistics_lots_info
        FOREIGN KEY (lots_info_id)
        REFERENCES lots_info(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 9. 好批批號記錄表（數值偏差偵測模組）
CREATE TABLE good_lots (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    lots_info_id INT NOT NULL,
    detection_method_id TINYINT NOT NULL,
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NULL,
    spec_calc_end_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_method (detection_method_id),
    UNIQUE INDEX unq_lot_method (lots_info_id, detection_method_id),
    CONSTRAINT fk_good_lots_info
        FOREIGN KEY (lots_info_id)
        REFERENCES lots_info(id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_good_lots_detection_method
        FOREIGN KEY (detection_method_id)
        REFERENCES detection_methods(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================================
-- 增量索引（若資料庫已建立，可單獨執行以下 ALTER TABLE 補上新索引）
-- =============================================================================
-- ALTER TABLE site_test_statistics ADD INDEX idx_program_site_item_time (program, site_id, test_item_name, start_time);
-- ALTER TABLE detection_specs ADD INDEX idx_site_id (site_id);

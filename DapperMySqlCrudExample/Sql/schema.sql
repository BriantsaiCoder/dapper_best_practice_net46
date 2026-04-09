-- =============================================================================
-- Dapper Best Practice (.NET 4.6 + MySQL) — 資料庫 Schema
-- =============================================================================
-- 請先建立資料庫：CREATE DATABASE your_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
--
-- ★ 執行順序：若為全新環境，請先執行 schema-legacy.sql 建立 lots_info 等外鍵依賴資料表。
--
-- ★ 本檔包含 9 張核心資料表，由 Repository 直接管理：
--    1. detection_methods            — 偵測方法主表
--    2. anomaly_lots                 — 異常批號主表
--    3. anomaly_test_items           — 異常測項明細
--    4. anomaly_units                — 異常 Unit 明細
--    5. anomaly_lot_process_mapping  — 批號 Process Mapping
--    6. anomaly_unit_process_mapping — Unit Process Mapping
--    7. detection_specs              — Spec 規格表
--    8. site_test_statistics         — Site 測項統計值表
--    9. good_lots                    — 好批批號記錄表
-- =============================================================================

-- 1. 偵測方法主表
CREATE TABLE detection_methods (
    id TINYINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    method_key VARCHAR(20) UNIQUE NOT NULL,
    method_name VARCHAR(50) NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO detection_methods (method_key, method_name) VALUES
('YIELD',     '良率偵測'),
('SITE_STD',  '標準差偵測'),
('MEAN',      '平均值偵測'),
('SITE_MEAN', 'Site平均值偵測');

-- 2. 異常批號主表
CREATE TABLE anomaly_lots (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    lots_info_id INT NOT NULL,
    detection_method_id TINYINT UNSIGNED NOT NULL,
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NULL,
    spec_calc_end_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
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
    site_id INT UNSIGNED NOT NULL,
    detection_value DECIMAL(18,9),
    spec_upper_limit DECIMAL(18,9),
    spec_lower_limit DECIMAL(18,9),
    spec_calc_start_time DATETIME NULL,
    spec_calc_end_time DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
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
    UNIQUE INDEX unq_item_unit (anomaly_test_item_id, unit_id),
    CONSTRAINT fk_units_test_item
        FOREIGN KEY (anomaly_test_item_id)
        REFERENCES anomaly_test_items(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. 批號 Process Mapping（廠區、站點、機台、人員）
CREATE TABLE anomaly_lot_process_mapping (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    anomaly_lot_id BIGINT NOT NULL,
    plant_name VARCHAR(100),
    station_name VARCHAR(100),
    machine_id VARCHAR(50),
    trackin_user VARCHAR(50),
    trackout_user VARCHAR(50),
    recipe VARCHAR(50),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_lot_process_anomaly_lot
        FOREIGN KEY (anomaly_lot_id)
        REFERENCES anomaly_lots(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Unit Process Mapping（Boat / Wafer / Substrate 座標追溯）
CREATE TABLE anomaly_unit_process_mapping (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    anomaly_unit_id BIGINT NOT NULL,
    boat_id VARCHAR(50) NOT NULL,
    boat_x SMALLINT NOT NULL,
    boat_y SMALLINT NOT NULL,
    wafer_barcode VARCHAR(50) NOT NULL,
    wafer_id VARCHAR(50) NOT NULL,
    wafer_x SMALLINT NOT NULL,
    wafer_y SMALLINT NOT NULL,
    substrate_id VARCHAR(50) NOT NULL,
    substrate_x SMALLINT NOT NULL,
    substrate_y SMALLINT NOT NULL,
    wafer_max_x SMALLINT NOT NULL,
    wafer_max_y SMALLINT NOT NULL,
    boat_max_x SMALLINT NOT NULL,
    boat_max_y SMALLINT NOT NULL,
    txn_time DATETIME NULL,
    station_name VARCHAR(100),
    equipment_id VARCHAR(50),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_unit_process_anomaly_unit
        FOREIGN KEY (anomaly_unit_id)
        REFERENCES anomaly_units(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 7. Spec 規格表
CREATE TABLE detection_specs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    program VARCHAR(100) NOT NULL,
    -- NULL when detection method does not use test items (e.g. YIELD)
    test_item_name VARCHAR(100),
    site_id INT UNSIGNED NOT NULL,
    detection_method_id TINYINT UNSIGNED NOT NULL,
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
    site_id INT UNSIGNED NOT NULL,
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
    detection_method_id TINYINT UNSIGNED NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
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
-- 常見擴充索引（新增對應 Repository 查詢方法時再加入）
-- =============================================================================
-- anomaly_lots:
--   ALTER TABLE anomaly_lots ADD INDEX idx_created_at (created_at);
--
-- anomaly_test_items:
--   ALTER TABLE anomaly_test_items ADD INDEX idx_test_item_name (test_item_name);
--
-- anomaly_units:
--   ALTER TABLE anomaly_units ADD INDEX idx_unit (unit_id);
--
-- anomaly_lot_process_mapping:
--   ALTER TABLE anomaly_lot_process_mapping ADD INDEX idx_machine (machine_id);
--   ALTER TABLE anomaly_lot_process_mapping ADD INDEX idx_plant_station (plant_name, station_name);
--
-- anomaly_unit_process_mapping:
--   ALTER TABLE anomaly_unit_process_mapping ADD INDEX idx_boat_position (boat_id, boat_x, boat_y);
--   ALTER TABLE anomaly_unit_process_mapping ADD INDEX idx_station_equipment (station_name, equipment_id);
--
-- =============================================================================
-- 欄位遷移（若既有環境仍使用舊欄位名稱）
-- =============================================================================
-- ALTER TABLE detection_methods
--   CHANGE COLUMN method_code method_key VARCHAR(20) NOT NULL;

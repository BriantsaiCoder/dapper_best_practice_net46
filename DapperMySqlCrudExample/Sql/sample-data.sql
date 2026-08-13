-- =============================================================================
-- Dapper Best Practice (.NET 4.6 + MySQL) — 半導體封測業範例資料
-- 請於 schema-legacy.sql 與 schema.sql 建立資料表後再執行本檔
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. 清空資料庫內容與重置計數器 (TRUNCATE ALL TABLES)
-- -----------------------------------------------------------------------------
SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE `good_lots`;
TRUNCATE TABLE `anomaly_unit_process_mapping`;
TRUNCATE TABLE `anomaly_units`;
TRUNCATE TABLE `anomaly_lot_process_mapping`;
TRUNCATE TABLE `anomaly_lots`;
TRUNCATE TABLE `site_test_statistics`;
TRUNCATE TABLE `detection_specs`;
TRUNCATE TABLE `lots_info`;
TRUNCATE TABLE `detection_methods`;

SET FOREIGN_KEY_CHECKS = 1;

-- -----------------------------------------------------------------------------
-- 1. 確保 detection_methods 種子資料（冪等寫入）
-- -----------------------------------------------------------------------------
INSERT INTO detection_methods (method_key, method_name)
VALUES
    ('SITE_STD', 'Site標準差偵測'),
    ('SITE_MEAN', 'Site平均值偵測'),
    ('OPEN_PPM', 'OPEN PPM偵測'),
    ('SHORT_PPM', 'SHORT PPM偵測')
ON DUPLICATE KEY UPDATE
    method_name = VALUES(method_name),
    updated_at = CURRENT_TIMESTAMP;

-- -----------------------------------------------------------------------------
-- 2. 根表：lots_info
-- -----------------------------------------------------------------------------
INSERT INTO lots_info
    (version, mac_address, db_key, customer, package, bonding_diagram, program, device,
     control_lot, ao_lot, os_machine_id, os_test_board_id, user_id, schedule_lot, file_name,
     yield, total, pass, open_pin_fail, short_pin_fail, leakage_pin_fail, nvtep_pin_fail,
     total_ppm, open_pin_fail_ppm, short_pin_fail_ppm, leakage_pin_fail_ppm, nvtep_pin_fail_ppm,
     total_test_items, average_test_time, clear_count, start, stop,
     pass_without_ocr, `open`, open_without_ocr, short_others,
     pass_without_ocr_ppm, open_ppm, open_without_ocr_ppm, short_others_ppm)
VALUES
-- 批號 1：QFN48 — MediaTek MT6985，正常批
('V3.2.1', '00:1A:2B:3C:4D:01', 'QFN48-20260401-001', 'MediaTek', 'QFN48',
 'BD-QFN48-A01', 'QFN48_PROD_V3', 'MT6985',
 'CL-2026040101', 'AO-2026040101', 'T5381-01', 'TB-QFN48-003', 'OP-KH-012', 'SL-20260401-A',
 'QFN48_PROD_V3_20260401_093015.stdf',
 98.5, 10000, 9850, 50, 30, 40, 30,
 15000.0, 5000.0, 3000.0, 4000.0, 3000.0,
 128, 0.85, 0, '2026-04-01 09:30:15', '2026-04-01 12:45:30',
 9860, 45, 42, 28,
 14000.0, 4500.0, 4200.0, 2800.0),

-- 批號 2：BGA256 — Qualcomm SM8650，低良率批（觸發異常偵測）
('V1.0.5', '00:1A:2B:3C:4D:02', 'BGA256-20260402-001', 'Qualcomm', 'BGA256',
 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'CL-2026040201', 'AO-2026040201', 'T5381-02', 'TB-BGA256-007', 'OP-KH-008', 'SL-20260402-A',
 'BGA256_PROD_V1_20260402_141022.stdf',
 95.2, 5000, 4760, 80, 60, 55, 45,
 48000.0, 16000.0, 12000.0, 11000.0, 9000.0,
 256, 1.52, 0, '2026-04-02 14:10:22', '2026-04-02 18:35:48',
 4770, 75, 70, 55,
 46000.0, 15000.0, 14000.0, 11000.0),

-- 批號 3：SOIC16 — Realtek RTL8125，正常批
('V2.1.0', '00:1A:2B:3C:4D:03', 'SOIC16-20260403-001', 'Realtek', 'SOIC16',
 'BD-SOIC16-C01', 'SOIC16_PROD_V2', 'RTL8125',
 'CL-2026040301', 'AO-2026040301', 'T5381-03', 'TB-SOIC16-002', 'OP-KH-015', 'SL-20260403-A',
 'SOIC16_PROD_V2_20260403_080530.stdf',
 99.1, 20000, 19820, 60, 40, 50, 30,
 9000.0, 3000.0, 2000.0, 2500.0, 1500.0,
 64, 0.42, 0, '2026-04-03 08:05:30', '2026-04-03 14:20:15',
 19830, 55, 50, 35,
 8500.0, 2750.0, 2500.0, 1750.0),

-- 批號 4：BGA256 — Qualcomm SM8650，正常批（SITE_MEAN 規格計算用歷史樣本）
('V1.0.5', '00:1A:2B:3C:4D:04', 'BGA256-20260405-001', 'Qualcomm', 'BGA256',
 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'CL-2026040501', 'AO-2026040501', 'T5381-02', 'TB-BGA256-007', 'OP-KH-008', 'SL-20260405-A',
 'BGA256_PROD_V1_20260405_100000.stdf',
 97.8, 5000, 4890, 35, 25, 30, 20,
 22000.0, 7000.0, 5000.0, 6000.0, 4000.0,
 256, 1.50, 0, '2026-04-05 10:00:00', '2026-04-05 14:30:00',
 4895, 30, 28, 22,
 21000.0, 6000.0, 5600.0, 4400.0),

-- 批號 5：BGA256 — Qualcomm SM8650，正常批（SITE_MEAN 規格計算用歷史樣本）
('V1.0.5', '00:1A:2B:3C:4D:05', 'BGA256-20260408-001', 'Qualcomm', 'BGA256',
 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'CL-2026040801', 'AO-2026040801', 'T5381-02', 'TB-BGA256-007', 'OP-KH-008', 'SL-20260408-A',
 'BGA256_PROD_V1_20260408_090000.stdf',
 98.1, 5000, 4905, 30, 20, 28, 17,
 19000.0, 6000.0, 4000.0, 5600.0, 3400.0,
 256, 1.48, 0, '2026-04-08 09:00:00', '2026-04-08 13:15:00',
 4910, 28, 25, 18,
 18000.0, 5600.0, 5000.0, 3600.0)
ON DUPLICATE KEY UPDATE file_name = VALUES(file_name);

-- -----------------------------------------------------------------------------
-- 2-1. 根表：lots_info — SITE_MEAN 歷史樣本（以 NOW() 為基準的相對時間，橫跨最近 100 天）
--
-- 【設計說明】
-- ComputeAndInsertSiteMeanSpec() 以 C# 端的 DateTime.Now.AddMonths(-1) 為取樣起點，
-- 若測試資料使用固定日期，隨著時間推移就會全部落在一個月之外導致樣本數不足。
-- 因此本段一律以 DATE_SUB(NOW(), INTERVAL n ...) 產生相對時間，讓資料永遠橫跨最近約 100 天（>3 個月）：
--   - HIST 段（41 ~ 107 天前，共 12 筆）：一個月之外的歷史資料，用於驗證取樣區間確實有生效
--   - RECENT 段（20 小時 ~ 約 26.7 天前，共 32 筆）：一個月之內的資料，即 SITE_MEAN 實際採用的樣本
--     筆數 32 > DetectionSpecService.MinimumSampleCount(=30)，可滿足最小樣本數門檻
-- db_key / file_name 以「相對天/時數」命名（非絕對日期），確保重複執行本檔時鍵值穩定且唯一。
-- 序號以 derived table（UNION ALL 數列）產生，避免逐筆手寫造成維護困難。
-- -----------------------------------------------------------------------------
INSERT INTO lots_info
    (version, mac_address, db_key, customer, package, bonding_diagram, program, device,
     os_machine_id, os_test_board_id, file_name,
     yield, total, pass, total_test_items, average_test_time, start, stop)
SELECT
    'V1.0.5',
    CONCAT('00:1A:2B:3C:4E:', LPAD(HEX(s.n), 2, '0')),
    CONCAT('BGA256-HIST-D', LPAD(35 + s.n * 6, 3, '0')),
    'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
    'T5381-02', 'TB-BGA256-007',
    CONCAT('BGA256_PROD_V1_HIST_D', LPAD(35 + s.n * 6, 3, '0'), '.stdf'),
    98.0, 5000, 4900, 256, 1.50,
    DATE_SUB(NOW(), INTERVAL (35 + s.n * 6) DAY),
    DATE_SUB(NOW(), INTERVAL (35 + s.n * 6) DAY) + INTERVAL 4 HOUR
FROM (
    SELECT  1 AS n UNION ALL SELECT  2 UNION ALL SELECT  3 UNION ALL SELECT  4
    UNION ALL SELECT  5 UNION ALL SELECT  6 UNION ALL SELECT  7 UNION ALL SELECT  8
    UNION ALL SELECT  9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
) AS s
ON DUPLICATE KEY UPDATE
    start = VALUES(start),
    stop  = VALUES(stop);

-- RECENT 段：一個月之內的 32 筆樣本（每筆相隔 20 小時，最舊者約 26.7 天前）
INSERT INTO lots_info
    (version, mac_address, db_key, customer, package, bonding_diagram, program, device,
     os_machine_id, os_test_board_id, file_name,
     yield, total, pass, total_test_items, average_test_time, start, stop)
SELECT
    'V1.0.5',
    CONCAT('00:1A:2B:3C:4F:', LPAD(HEX(s.n), 2, '0')),
    CONCAT('BGA256-RECENT-H', LPAD(s.n * 20, 3, '0')),
    'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
    'T5381-02', 'TB-BGA256-007',
    CONCAT('BGA256_PROD_V1_RECENT_H', LPAD(s.n * 20, 3, '0'), '.stdf'),
    98.1, 5000, 4905, 256, 1.49,
    DATE_SUB(NOW(), INTERVAL s.n * 20 HOUR),
    DATE_SUB(NOW(), INTERVAL s.n * 20 HOUR) + INTERVAL 4 HOUR
FROM (
    SELECT  1 AS n UNION ALL SELECT  2 UNION ALL SELECT  3 UNION ALL SELECT  4
    UNION ALL SELECT  5 UNION ALL SELECT  6 UNION ALL SELECT  7 UNION ALL SELECT  8
    UNION ALL SELECT  9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12
    UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15 UNION ALL SELECT 16
    UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
    UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24
    UNION ALL SELECT 25 UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28
    UNION ALL SELECT 29 UNION ALL SELECT 30 UNION ALL SELECT 31 UNION ALL SELECT 32
) AS s
ON DUPLICATE KEY UPDATE
    start = VALUES(start),
    stop  = VALUES(stop);

-- -----------------------------------------------------------------------------
-- 3. anomaly_lots
-- -----------------------------------------------------------------------------
INSERT INTO anomaly_lots
    (lots_info_id, site_id, detection_method_id, detection_value, offset_value, spec_upper_limit, spec_lower_limit)
VALUES
(
    (SELECT id FROM lots_info WHERE db_key = 'BGA256-20260402-001'),
    2,
    (SELECT id FROM detection_methods WHERE method_key = 'SITE_MEAN'),
    3.380000000, 0.030000000, 3.350000000, 3.150000000
)
ON DUPLICATE KEY UPDATE detection_value = VALUES(detection_value);

-- -----------------------------------------------------------------------------
-- 4. anomaly_units
-- -----------------------------------------------------------------------------
INSERT INTO anomaly_units
    (anomaly_lot_id, test_item_name, site_id, unit_id, detection_value, offset_value, spec_upper_limit, spec_lower_limit)
VALUES
(
    (SELECT al.id FROM anomaly_lots al
     JOIN lots_info li ON al.lots_info_id = li.id
     JOIN detection_methods dm ON al.detection_method_id = dm.id
     WHERE li.db_key = 'BGA256-20260402-001' AND dm.method_key = 'SITE_MEAN'),
    'VOH_PIN12', 2, '', 3.380000000, 0.030000000, 3.350000000, 3.150000000
),
(
    (SELECT al.id FROM anomaly_lots al
     JOIN lots_info li ON al.lots_info_id = li.id
     JOIN detection_methods dm ON al.detection_method_id = dm.id
     WHERE li.db_key = 'BGA256-20260402-001' AND dm.method_key = 'SITE_MEAN'),
    'VOH_PIN12', 2, 'U-BGA256-00587', 3.390000000, 0.040000000, 3.350000000, 3.150000000
)
ON DUPLICATE KEY UPDATE detection_value = VALUES(detection_value);

-- -----------------------------------------------------------------------------
-- 5. anomaly_lot_process_mapping (欄位: lots_info_id)
-- -----------------------------------------------------------------------------
INSERT INTO anomaly_lot_process_mapping
    (lots_info_id, plant_name, station_name, machine_id, trackin_user, trackout_user, recipe)
VALUES
(
    (SELECT id FROM lots_info WHERE db_key = 'BGA256-20260402-001'),
    'KH-FAB2', 'MOLDING', 'MD-TOWA-02', 'OP-KH-005', 'OP-KH-006', 'MD-BGA256-EMC-V3'
);

-- -----------------------------------------------------------------------------
-- 6. anomaly_unit_process_mapping
-- -----------------------------------------------------------------------------
INSERT INTO anomaly_unit_process_mapping
    (anomaly_unit_id, boat_id, boat_x, boat_y,
     wafer_barcode, wafer_id, wafer_x, wafer_y,
     substrate_id, substrate_x, substrate_y,
     wafer_max_x, wafer_max_y, boat_max_x, boat_max_y,
     plant_name, station_name, equipment_id)
VALUES
(
    (SELECT au.id FROM anomaly_units au
     JOIN anomaly_lots al ON au.anomaly_lot_id = al.id
     JOIN lots_info li ON al.lots_info_id = li.id
     JOIN detection_methods dm ON al.detection_method_id = dm.id
     WHERE li.db_key = 'BGA256-20260402-001' AND dm.method_key = 'SITE_MEAN'
       AND au.test_item_name = 'VOH_PIN12' AND au.unit_id = 'U-BGA256-00587'),
    'BOAT-FT-001', 5, 12,
    'WF-SM8650-LOT02-W05-BC', 'WF-SM8650-LOT02-W05', 8, 31,
    'SUB-BGA256-A01', 3, 6,
    30, 40, 8, 16,
    'KH-FAB1', 'FINAL_TEST', 'FT-J750-01'
);

-- -----------------------------------------------------------------------------
-- 7. detection_specs
-- -----------------------------------------------------------------------------
INSERT INTO detection_specs
    (program, test_item_name, site_id, detection_method_id,
     spec_upper_limit, spec_lower_limit, spec_calc_start_time, spec_calc_end_time,
     spec_calc_mean, spec_calc_std)
VALUES
(
    'BGA256_PROD_V1', 'VOH_PIN12', 1,
    (SELECT id FROM detection_methods WHERE method_key = 'SITE_MEAN'),
    3.350000000, 3.150000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59',
    3.248000000, 0.034000000
);

-- -----------------------------------------------------------------------------
-- 8. site_test_statistics
-- -----------------------------------------------------------------------------
INSERT INTO site_test_statistics
    (lots_info_id, program, site_id, test_item_name, mean_value, max_value, min_value, std_value, tester_id, start_time, end_time)
VALUES
((SELECT id FROM lots_info WHERE db_key = 'BGA256-20260402-001'), 'BGA256_PROD_V1', 1, 'VOH_PIN12', 3.268000000, 3.380000000, 3.162000000, 0.035000000, 'FT-J750-01', '2026-04-02 14:10:22', '2026-04-02 18:35:48'),
((SELECT id FROM lots_info WHERE db_key = 'BGA256-20260405-001'), 'BGA256_PROD_V1', 1, 'VOH_PIN12', 3.252000000, 3.345000000, 3.170000000, 0.031000000, 'FT-J750-01', '2026-04-05 10:00:00', '2026-04-05 14:30:00'),
((SELECT id FROM lots_info WHERE db_key = 'BGA256-20260408-001'), 'BGA256_PROD_V1', 1, 'VOH_PIN12', 3.241000000, 3.338000000, 3.158000000, 0.029000000, 'FT-J750-01', '2026-04-08 09:00:00', '2026-04-08 13:15:00'),
((SELECT id FROM lots_info WHERE db_key = 'QFN48-20260401-001'), 'QFN48_PROD_V3', 1, 'FREQ_OSC', 2.500200000, 2.509800000, 2.490500000, 0.003100000, 'FT-J750-02', '2026-04-01 09:30:15', '2026-04-01 12:45:30'),
((SELECT id FROM lots_info WHERE db_key = 'SOIC16-20260403-001'), 'SOIC16_PROD_V2', 1, 'IDD_STANDBY', 1.250000000, 1.890000000, 0.820000000, 0.180000000, 'FT-93K-01', '2026-04-03 08:05:30', '2026-04-03 14:20:15')
ON DUPLICATE KEY UPDATE mean_value = VALUES(mean_value);

-- -----------------------------------------------------------------------------
-- 8-1. site_test_statistics — SITE_MEAN 規格計算用的相對時間樣本（橫跨最近約 100 天）
--
-- start_time / end_time 直接沿用 lots_info 的 start / stop，
-- 使 QuerySiteMeanRows(@SinceTime) 的過濾條件與批號時間一致。
-- 一個月內共 32 筆（RECENT 段），高於 DetectionSpecService.MinimumSampleCount(=30)；
-- mean_value 以 CRC32(db_key) 產生 3.235 ~ 3.265 之間的離散值，
-- 確保計算出的 std 不為 0，UCL/LCL 才具意義。
-- -----------------------------------------------------------------------------
INSERT INTO site_test_statistics
    (lots_info_id, program, site_id, test_item_name, mean_value, max_value, min_value, std_value, tester_id, start_time, end_time)
SELECT x.id, 'BGA256_PROD_V1', 1, 'VOH_PIN12',
       x.mean_value, x.mean_value + 0.100, x.mean_value - 0.100, 0.034,
       'FT-J750-01', x.start, x.stop
FROM (
    SELECT li.id, li.start, li.stop,
           ROUND(3.250 + ((CAST(CRC32(li.db_key) AS SIGNED) % 11) - 5) * 0.003, 3) AS mean_value
    FROM   lots_info li
    WHERE  li.db_key LIKE 'BGA256-HIST-D%'
       OR  li.db_key LIKE 'BGA256-RECENT-H%'
) AS x
ON DUPLICATE KEY UPDATE
    mean_value = VALUES(mean_value),
    max_value  = VALUES(max_value),
    min_value  = VALUES(min_value),
    std_value  = VALUES(std_value),
    start_time = VALUES(start_time),
    end_time   = VALUES(end_time);

-- -----------------------------------------------------------------------------
-- 9. good_lots
-- -----------------------------------------------------------------------------
INSERT INTO good_lots (lots_info_id, detection_method_id)
VALUES
(
    (SELECT id FROM lots_info WHERE db_key = 'SOIC16-20260403-001'),
    (SELECT id FROM detection_methods WHERE method_key = 'SITE_MEAN')
),
(
    (SELECT id FROM lots_info WHERE db_key = 'BGA256-20260405-001'),
    (SELECT id FROM detection_methods WHERE method_key = 'SITE_MEAN')
),
(
    (SELECT id FROM lots_info WHERE db_key = 'BGA256-20260408-001'),
    (SELECT id FROM detection_methods WHERE method_key = 'SITE_MEAN')
)
ON DUPLICATE KEY UPDATE updated_at = CURRENT_TIMESTAMP;

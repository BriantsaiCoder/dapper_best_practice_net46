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
-- 2-1. 根表：lots_info — SITE_MEAN 歷史樣本（相對於執行當下 NOW() 往前推 100 天）
--
-- 【設計說明】
-- ComputeAndInsertSiteMeanSpec() 以 C# 端的 DateTime.Now.AddMonths(-1) 為取樣起點，
-- 若測試資料使用固定日期，隨著時間推移就會全部落在一個月之外導致樣本數不足。
-- 因此本段一律以 DATE_SUB(NOW(), INTERVAL n DAY) 產生相對時間，
-- 讓資料「永遠」橫跨最近 100 天（超過 3 個月）：
--   - D100 ~ D37（共 10 筆）：一個月之外的歷史資料，用於驗證取樣區間確實有被過濾掉
--   - D28 ~ D01（共 8 筆） ：一個月之內的資料，即為 SITE_MEAN 規格計算實際採用的樣本
-- db_key / file_name 以「相對天數」命名（非絕對日期），確保重複執行本檔時鍵值穩定且唯一。
-- -----------------------------------------------------------------------------
INSERT INTO lots_info
    (version, mac_address, db_key, customer, package, bonding_diagram, program, device,
     os_machine_id, os_test_board_id, file_name,
     yield, total, pass, total_test_items, average_test_time, start, stop)
VALUES
-- ── 一個月之外（D100 ~ D37）：僅作為「3 個月以上歷史資料」的背景，不會被取樣 ──
('V1.0.5', '00:1A:2B:3C:4E:64', 'BGA256-HIST-D100', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D100.stdf',
 97.9, 5000, 4895, 256, 1.50, DATE_SUB(NOW(), INTERVAL 100 DAY), DATE_SUB(NOW(), INTERVAL 100 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:5D', 'BGA256-HIST-D093', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D093.stdf',
 98.0, 5000, 4900, 256, 1.49, DATE_SUB(NOW(), INTERVAL 93 DAY), DATE_SUB(NOW(), INTERVAL 93 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:56', 'BGA256-HIST-D086', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D086.stdf',
 98.2, 5000, 4910, 256, 1.51, DATE_SUB(NOW(), INTERVAL 86 DAY), DATE_SUB(NOW(), INTERVAL 86 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:4F', 'BGA256-HIST-D079', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D079.stdf',
 97.6, 5000, 4880, 256, 1.53, DATE_SUB(NOW(), INTERVAL 79 DAY), DATE_SUB(NOW(), INTERVAL 79 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:48', 'BGA256-HIST-D072', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D072.stdf',
 98.4, 5000, 4920, 256, 1.47, DATE_SUB(NOW(), INTERVAL 72 DAY), DATE_SUB(NOW(), INTERVAL 72 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:41', 'BGA256-HIST-D065', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D065.stdf',
 98.1, 5000, 4905, 256, 1.48, DATE_SUB(NOW(), INTERVAL 65 DAY), DATE_SUB(NOW(), INTERVAL 65 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:3A', 'BGA256-HIST-D058', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D058.stdf',
 97.7, 5000, 4885, 256, 1.52, DATE_SUB(NOW(), INTERVAL 58 DAY), DATE_SUB(NOW(), INTERVAL 58 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:33', 'BGA256-HIST-D051', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D051.stdf',
 98.3, 5000, 4915, 256, 1.46, DATE_SUB(NOW(), INTERVAL 51 DAY), DATE_SUB(NOW(), INTERVAL 51 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:2C', 'BGA256-HIST-D044', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D044.stdf',
 98.0, 5000, 4900, 256, 1.50, DATE_SUB(NOW(), INTERVAL 44 DAY), DATE_SUB(NOW(), INTERVAL 44 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:25', 'BGA256-HIST-D037', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_HIST_D037.stdf',
 97.9, 5000, 4895, 256, 1.51, DATE_SUB(NOW(), INTERVAL 37 DAY), DATE_SUB(NOW(), INTERVAL 37 DAY) + INTERVAL 4 HOUR),

-- ── 一個月之內（D28 ~ D01）：SITE_MEAN 規格計算實際採用的 8 筆樣本 ──
('V1.0.5', '00:1A:2B:3C:4E:1C', 'BGA256-RECENT-D28', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D28.stdf',
 98.2, 5000, 4910, 256, 1.49, DATE_SUB(NOW(), INTERVAL 28 DAY), DATE_SUB(NOW(), INTERVAL 28 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:15', 'BGA256-RECENT-D21', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D21.stdf',
 98.4, 5000, 4920, 256, 1.48, DATE_SUB(NOW(), INTERVAL 21 DAY), DATE_SUB(NOW(), INTERVAL 21 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:0E', 'BGA256-RECENT-D14', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D14.stdf',
 98.1, 5000, 4905, 256, 1.50, DATE_SUB(NOW(), INTERVAL 14 DAY), DATE_SUB(NOW(), INTERVAL 14 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:0A', 'BGA256-RECENT-D10', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D10.stdf',
 97.8, 5000, 4890, 256, 1.52, DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:06', 'BGA256-RECENT-D06', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D06.stdf',
 98.3, 5000, 4915, 256, 1.47, DATE_SUB(NOW(), INTERVAL 6 DAY), DATE_SUB(NOW(), INTERVAL 6 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:03', 'BGA256-RECENT-D03', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D03.stdf',
 98.0, 5000, 4900, 256, 1.49, DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:02', 'BGA256-RECENT-D02', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D02.stdf',
 97.9, 5000, 4895, 256, 1.51, DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY) + INTERVAL 4 HOUR),
('V1.0.5', '00:1A:2B:3C:4E:01', 'BGA256-RECENT-D01', 'Qualcomm', 'BGA256', 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'T5381-02', 'TB-BGA256-007', 'BGA256_PROD_V1_RECENT_D01.stdf',
 98.2, 5000, 4910, 256, 1.48, DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY) + INTERVAL 4 HOUR)
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
-- 8-1. site_test_statistics — SITE_MEAN 規格計算用的相對時間樣本（橫跨最近 100 天）
--
-- start_time / end_time 直接沿用 lots_info 的 start / stop，
-- 使 QuerySiteMeanRows(@SinceTime) 的過濾條件與批號時間一致。
-- 一個月內共 8 筆（D28、D21、D14、D10、D06、D03、D02、D01），
-- 遠高於 DetectionSpecService.MinimumSampleCount(=2)，
-- 且 mean_value 有適度離散，計算出的 std 不為 0，UCL/LCL 才具意義。
-- -----------------------------------------------------------------------------
INSERT INTO site_test_statistics
    (lots_info_id, program, site_id, test_item_name, mean_value, max_value, min_value, std_value, tester_id, start_time, end_time)
SELECT li.id, 'BGA256_PROD_V1', 1, 'VOH_PIN12', v.mean_value, v.max_value, v.min_value, v.std_value, 'FT-J750-01', li.start, li.stop
FROM (
    -- 一個月之外的歷史資料（不會被 SITE_MEAN 取樣，僅示範取樣區間確實有生效）
    SELECT 'BGA256-HIST-D100' AS db_key, 3.301 AS mean_value, 3.402 AS max_value, 3.201 AS min_value, 0.041 AS std_value
    UNION ALL SELECT 'BGA256-HIST-D093', 3.295, 3.396, 3.194, 0.040
    UNION ALL SELECT 'BGA256-HIST-D086', 3.288, 3.389, 3.187, 0.039
    UNION ALL SELECT 'BGA256-HIST-D079', 3.284, 3.385, 3.183, 0.038
    UNION ALL SELECT 'BGA256-HIST-D072', 3.279, 3.380, 3.178, 0.038
    UNION ALL SELECT 'BGA256-HIST-D065', 3.273, 3.374, 3.172, 0.037
    UNION ALL SELECT 'BGA256-HIST-D058', 3.270, 3.371, 3.169, 0.036
    UNION ALL SELECT 'BGA256-HIST-D051', 3.266, 3.367, 3.165, 0.036
    UNION ALL SELECT 'BGA256-HIST-D044', 3.261, 3.362, 3.160, 0.035
    UNION ALL SELECT 'BGA256-HIST-D037', 3.258, 3.359, 3.157, 0.035
    -- 一個月之內的樣本（實際參與 mean / std 計算）
    UNION ALL SELECT 'BGA256-RECENT-D28', 3.252, 3.351, 3.153, 0.034
    UNION ALL SELECT 'BGA256-RECENT-D21', 3.247, 3.346, 3.148, 0.033
    UNION ALL SELECT 'BGA256-RECENT-D14', 3.256, 3.355, 3.157, 0.034
    UNION ALL SELECT 'BGA256-RECENT-D10', 3.243, 3.342, 3.144, 0.033
    UNION ALL SELECT 'BGA256-RECENT-D06', 3.259, 3.358, 3.160, 0.035
    UNION ALL SELECT 'BGA256-RECENT-D03', 3.250, 3.349, 3.151, 0.033
    UNION ALL SELECT 'BGA256-RECENT-D02', 3.245, 3.344, 3.146, 0.032
    UNION ALL SELECT 'BGA256-RECENT-D01', 3.254, 3.353, 3.155, 0.034
) AS v
JOIN lots_info li ON li.db_key = v.db_key
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

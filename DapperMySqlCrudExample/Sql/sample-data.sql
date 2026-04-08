-- =============================================================================
-- Dapper Best Practice (.NET 4.6 + MySQL) — 半導體封測業範例資料
-- =============================================================================
-- ★ 執行順序：
--   1. schema-legacy.sql  （建立 lots_info）
--   2. schema.sql          （建立 9 張核心表 + detection_methods 種子資料）
--   3. 本檔 sample-data.sql（插入範例資料）
--
-- ★ 本檔提供每張資料表各 3 筆範例資料，模擬以下半導體後段封測場景：
--   - 三個批號分屬不同客戶 / 封裝 / 程式（QFN48、BGA256、SOIC16）
--   - BGA256 批為低良率批，觸發 YIELD 與 SITE_MEAN 異常偵測
--   - QFN48 批因平均值接近邊界，觸發 MEAN 異常偵測
--   - 測試項目使用真實 Final Test 參數（IDD_STANDBY、VOH、FREQ_OSC 等）
--   - Process Mapping 反映封測典型製程流程（Die Attach → Wire Bond → Molding → Final Test）
--
-- ★ 假設 detection_methods 已由 schema.sql 寫入 4 筆種子資料：
--   id=1 YIELD, id=2 SITE_STD, id=3 MEAN, id=4 SITE_MEAN
-- =============================================================================

-- =============================================================================
-- 層級 0：lots_info（外鍵根節點）
-- =============================================================================
-- 三個批號分別代表不同客戶 / 封裝型式 / 測試程式
-- lots_info_id=1: QFN48 正常批（良率 98.5%）
-- lots_info_id=2: BGA256 低良率批（良率 95.2%，將觸發異常偵測）
-- lots_info_id=3: SOIC16 正常批（良率 99.1%）

INSERT INTO lots_info
    (id, version, mac_address, db_key, customer, package, bonding_diagram, program, device,
     control_lot, ao_lot, os_machine_id, os_test_board_id, user_id, schedule_lot, file_name,
     yield, total, pass, open_pin_fail, short_pin_fail, leakage_pin_fail, nvtep_pin_fail,
     total_ppm, open_pin_fail_ppm, short_pin_fail_ppm, leakage_pin_fail_ppm, nvtep_pin_fail_ppm,
     total_test_items, average_test_time, clear_count, start, stop,
     pass_without_ocr, `open`, open_without_ocr, short_others,
     pass_without_ocr_ppm, open_ppm, open_without_ocr_ppm, short_others_ppm)
VALUES
-- 批號 1：QFN48 — MediaTek MT6985，正常批
(1, 'V3.2.1', '00:1A:2B:3C:4D:01', 'QFN48-20260401-001', 'MediaTek', 'QFN48',
 'BD-QFN48-A01', 'QFN48_PROD_V3', 'MT6985',
 'CL-2026040101', 'AO-2026040101', 'T5381-01', 'TB-QFN48-003', 'OP-KH-012', 'SL-20260401-A',
 'QFN48_PROD_V3_20260401_093015.stdf',
 98.5, 10000, 9850, 50, 30, 40, 30,
 15000.0, 5000.0, 3000.0, 4000.0, 3000.0,
 128, 0.85, 0, '2026-04-01 09:30:15', '2026-04-01 12:45:30',
 9860, 45, 42, 28,
 14000.0, 4500.0, 4200.0, 2800.0),

-- 批號 2：BGA256 — Qualcomm SM8650，低良率批（觸發異常偵測）
(2, 'V1.0.5', '00:1A:2B:3C:4D:02', 'BGA256-20260402-001', 'Qualcomm', 'BGA256',
 'BD-BGA256-B02', 'BGA256_PROD_V1', 'SM8650',
 'CL-2026040201', 'AO-2026040201', 'T5381-02', 'TB-BGA256-007', 'OP-KH-008', 'SL-20260402-A',
 'BGA256_PROD_V1_20260402_141022.stdf',
 95.2, 5000, 4760, 80, 60, 55, 45,
 48000.0, 16000.0, 12000.0, 11000.0, 9000.0,
 256, 1.52, 0, '2026-04-02 14:10:22', '2026-04-02 18:35:48',
 4770, 75, 70, 55,
 46000.0, 15000.0, 14000.0, 11000.0),

-- 批號 3：SOIC16 — Realtek RTL8125，正常批
(3, 'V2.1.0', '00:1A:2B:3C:4D:03', 'SOIC16-20260403-001', 'Realtek', 'SOIC16',
 'BD-SOIC16-C01', 'SOIC16_PROD_V2', 'RTL8125',
 'CL-2026040301', 'AO-2026040301', 'T5381-03', 'TB-SOIC16-002', 'OP-KH-015', 'SL-20260403-A',
 'SOIC16_PROD_V2_20260403_080530.stdf',
 99.1, 20000, 19820, 60, 40, 50, 30,
 9000.0, 3000.0, 2000.0, 2500.0, 1500.0,
 64, 0.42, 0, '2026-04-03 08:05:30', '2026-04-03 14:20:15',
 19830, 55, 50, 35,
 8500.0, 2750.0, 2500.0, 1750.0);

-- =============================================================================
-- 層級 1：anomaly_lots（異常批號主表）
-- =============================================================================
-- detection_methods 種子資料已由 schema.sql 寫入，此處不重複。
-- anomaly_lots 記錄哪些批號在哪種偵測方法中被判定為異常。

INSERT INTO anomaly_lots
    (lots_info_id, detection_method_id,
     spec_upper_limit, spec_lower_limit, spec_calc_start_time, spec_calc_end_time)
VALUES
-- BGA256 低良率批 → YIELD 偵測異常（良率 95.2% 低於規格下限 97.0%）
(2, 1,
 100.000000000, 97.000000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59'),

-- BGA256 低良率批 → SITE_MEAN 偵測異常（Site 平均值偏移）
(2, 4,
 3.350000000, 3.150000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59'),

-- QFN48 正常批 → MEAN 偵測異常（平均值 2.508 接近 UCL 2.510 邊界觸發）
(1, 3,
 2.510000000, 2.490000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59');

-- =============================================================================
-- 層級 2：anomaly_test_items（異常測項明細）
-- =============================================================================
-- 記錄每個異常批號中具體哪些測試項目超出規格。
-- 測項名稱使用半導體 Final Test 常見參數：
--   IDD_STANDBY : 待機電流（mA）
--   VOH_PIN12   : Pin 12 輸出高電壓（V）
--   FREQ_OSC    : 內部振盪器頻率（MHz）

INSERT INTO anomaly_test_items
    (anomaly_lot_id, test_item_name, detection_value,
     spec_upper_limit, spec_lower_limit, spec_calc_start_time, spec_calc_end_time)
VALUES
-- anomaly_lot=1 (BGA256/YIELD)：IDD_STANDBY 待機電流異常偏高
(1, 'IDD_STANDBY', 5.230000000,
 5.000000000, 0.100000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59'),

-- anomaly_lot=2 (BGA256/SITE_MEAN)：VOH_PIN12 輸出電壓 Site 間偏差
(2, 'VOH_PIN12', 3.380000000,
 3.350000000, 3.150000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59'),

-- anomaly_lot=3 (QFN48/MEAN)：FREQ_OSC 振盪頻率偏移
(3, 'FREQ_OSC', 2.508500000,
 2.510000000, 2.490000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59');

-- =============================================================================
-- 層級 3：anomaly_units（異常 Unit 明細）
-- =============================================================================
-- 記錄具體哪些 Unit（晶粒/顆粒）在該測項中超出規格。
-- Unit ID 使用半導體慣用格式：U-{封裝}-{序號}

INSERT INTO anomaly_units
    (anomaly_test_item_id, unit_id, detection_value,
     spec_upper_limit, spec_lower_limit, spec_calc_start_time, spec_calc_end_time)
VALUES
-- test_item=1 (BGA256/IDD_STANDBY)：Unit 電流 5.42mA 超過 UCL 5.0mA
(1, 'U-BGA256-00142', 5.420000000,
 5.000000000, 0.100000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59'),

-- test_item=2 (BGA256/VOH_PIN12)：Unit 電壓 3.39V 超過 UCL 3.35V
(2, 'U-BGA256-00587', 3.390000000,
 3.350000000, 3.150000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59'),

-- test_item=3 (QFN48/FREQ_OSC)：Unit 頻率 2.5112MHz 超過 UCL 2.510MHz
(3, 'U-QFN48-03201', 2.511200000,
 2.510000000, 2.490000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59');

-- =============================================================================
-- 層級 2：anomaly_lot_process_mapping（批號製程追溯）
-- =============================================================================
-- 記錄異常批號流經的製程站點與機台，用於異常根因分析。
-- 站點名稱使用封測業典型後段製程：
--   DIE_ATTACH : 晶粒黏著（DA）
--   WIRE_BOND  : 打線（WB）
--   MOLDING    : 封膠（MD）

INSERT INTO anomaly_lot_process_mapping
    (anomaly_lot_id, station_name, equipment_id, process_time, op_id, recipe)
VALUES
-- anomaly_lot=1 (BGA256/YIELD) 流經 Die Attach 站
(1, 'DIE_ATTACH', 'DA-ASM-03', '2026-04-02 10:15:00', 'OP-KH-008', 'DA-BGA256-STD-V2'),

-- anomaly_lot=1 (BGA256/YIELD) 流經 Wire Bond 站
(1, 'WIRE_BOND', 'WB-KNS-07', '2026-04-02 11:30:00', 'OP-KH-022', 'WB-BGA256-AU-V1'),

-- anomaly_lot=2 (BGA256/SITE_MEAN) 流經 Molding 站
(2, 'MOLDING', 'MD-TOWA-02', '2026-04-02 13:00:00', 'OP-KH-005', 'MD-BGA256-EMC-V3');

-- =============================================================================
-- 層級 4：anomaly_unit_process_mapping（Unit 製程追溯）
-- =============================================================================
-- 記錄異常 Unit 在各站的 Boat 位置、Wafer 來源、SBS 載具追溯。
-- Boat: 測試載具（boat_id + XY 座標）
-- Wafer: 晶圓來源追溯（wafer_id + die 座標）
-- SBS: Substrate Boat System 載具（sbs_id + 料格座標）

INSERT INTO anomaly_unit_process_mapping
    (anomaly_unit_id, boat_id, boat_position_x, boat_position_y,
     wafer_id, wafer_position_x, wafer_position_y,
     sbs_id, sbs_position_x, sbs_position_y,
     process_time, station_name, equipment_id)
VALUES
-- unit=1 (U-BGA256-00142) 在 Final Test 站
(1, 'BOAT-FT-001', 3, 7,
 'WF-SM8650-LOT02-W03', 15, 22,
 'SBS-BGA256-A01', 2, 4,
 '2026-04-02 15:20:00', 'FINAL_TEST', 'FT-J750-01'),

-- unit=2 (U-BGA256-00587) 在 Final Test 站
(2, 'BOAT-FT-001', 5, 12,
 'WF-SM8650-LOT02-W05', 8, 31,
 'SBS-BGA256-A01', 3, 6,
 '2026-04-02 15:45:00', 'FINAL_TEST', 'FT-J750-01'),

-- unit=3 (U-QFN48-03201) 在 Marking 站
(3, 'BOAT-MK-003', 1, 2,
 'WF-MT6985-LOT01-W01', 20, 18,
 NULL, NULL, NULL,
 '2026-04-01 13:10:00', 'MARKING', 'MK-DOM-02');

-- =============================================================================
-- 獨立表：detection_specs（偵測規格）
-- =============================================================================
-- 記錄各程式 / 測項 / Site 的偵測規格上下限與統計參數。
-- 這些規格用於判斷批號是否異常。

INSERT INTO detection_specs
    (program, test_item_name, site_id, detection_method_id,
     spec_upper_limit, spec_lower_limit, spec_calc_start_time, spec_calc_end_time,
     spec_calc_mean, spec_calc_std)
VALUES
-- BGA256 程式 / YIELD 偵測（test_item_name 為 NULL，良率為批級偵測）
('BGA256_PROD_V1', NULL, 0, 1,
 100.000000000, 97.000000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59',
 98.200000000, 0.600000000),

-- BGA256 程式 / VOH_PIN12 / Site 1 / SITE_MEAN 偵測
('BGA256_PROD_V1', 'VOH_PIN12', 1, 4,
 3.350000000, 3.150000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59',
 3.248000000, 0.034000000),

-- QFN48 程式 / FREQ_OSC / Site 1 / MEAN 偵測
('QFN48_PROD_V3', 'FREQ_OSC', 1, 3,
 2.510000000, 2.490000000, '2026-03-01 00:00:00', '2026-04-01 23:59:59',
 2.500500000, 0.003200000);

-- =============================================================================
-- 獨立表：site_test_statistics（Site 測項統計值）
-- =============================================================================
-- 記錄每個批號在各 Site 的量測統計數據，包含 Cp/Cpk 製程能力指標。
-- ★ 此資料供 CrudSampleRunner 範例三（SITE_MEAN 規格計算）使用。
--   需確保至少 2 筆相同 program + site_id + test_item_name 且 mean_value / start_time 非 NULL。

INSERT INTO site_test_statistics
    (lots_info_id, program, site_id, test_item_name,
     mean_value, max_value, min_value, std_value,
     cp_value, cpk_value, tester_id, start_time, end_time)
VALUES
-- 批號 2 (BGA256) / Site 1 / VOH_PIN12 — 製程能力不足（Cpk < 1.33）
(2, 'BGA256_PROD_V1', 1, 'VOH_PIN12',
 3.268000000, 3.380000000, 3.162000000, 0.035000000,
 0.952000000, 0.781000000, 'FT-J750-01', '2026-04-02 14:10:22', '2026-04-02 18:35:48'),

-- 批號 1 (QFN48) / Site 1 / FREQ_OSC — 製程能力良好（Cpk > 1.33）
(1, 'QFN48_PROD_V3', 1, 'FREQ_OSC',
 2.500200000, 2.509800000, 2.490500000, 0.003100000,
 1.075000000, 1.052000000, 'FT-J750-02', '2026-04-01 09:30:15', '2026-04-01 12:45:30'),

-- 批號 3 (SOIC16) / Site 1 / IDD_STANDBY — 製程能力優良（Cpk > 1.67）
(3, 'SOIC16_PROD_V2', 1, 'IDD_STANDBY',
 1.250000000, 1.890000000, 0.820000000, 0.180000000,
 4.537000000, 3.472000000, 'FT-93K-01', '2026-04-03 08:05:30', '2026-04-03 14:20:15');

-- =============================================================================
-- 獨立表：good_lots（好批批號記錄）
-- =============================================================================
-- 記錄通過偵測的好批，作為下一輪 Spec 計算的採樣來源。
-- 每筆記錄一個批號在某偵測方法下的「良好」判定。

INSERT INTO good_lots
    (lots_info_id, detection_method_id)
VALUES
-- 批號 1 (QFN48) 通過 YIELD 偵測 → 列為好批
(1, 1),

-- 批號 3 (SOIC16) 通過 YIELD 偵測 → 列為好批
(3, 1),

-- 批號 3 (SOIC16) 通過 SITE_MEAN 偵測 → 列為好批
(3, 4);

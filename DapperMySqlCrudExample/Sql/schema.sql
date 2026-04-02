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
-- ★ 執行順序 & 資料表分類說明
-- =============================================================================
-- 本 schema 分為兩個區塊：
--   A. 第 2 節「整合既有系統資料表」：
--      整合既有系統的資料表定義；若您的環境已存在這些資料表，可跳過本節。
--      其中 lots_info 為本專案核心資料表的外鍵依賴，必須於核心資料表之前存在。
--   B. 第 3~11 節「本專案核心資料表」：
--      本專案 Repository 直接管理的 9 張表，
--      需在 lots_info 等外鍵參照的資料表存在後方可建立。
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
--   ◆ 關鍵 FK 依賴（執行第 3~11 節前必須先存在）：
--      • lots_info                   — 批號主資料（anomaly_lots / site_test_statistics /
--                                      good_lots 均以其 id 為外鍵）
--
--   ◆ 既有系統整合資料表（第 2 節含完整 DDL，可視需要選擇性建立）：
--      • db_key                      — 資料庫金鑰狀態追蹤
--      • db_key_ui_status            — UI 狀態記錄
--      • fail_pin_rate_info          — 壞針率資訊主表
--      • fail_pin_rate_list          — 壞針率清單（FK → fail_pin_rate_info）
--      • fail_pin_rate_list_pin_ball — 壞針清單 Pin/Ball 明細（FK → fail_pin_rate_list）
--      • fail_pin_rate_test_result   — 壞針測試結果（FK → fail_pin_rate_list）
--      • ieda_content                — IEDA 測試資料內容
--      • ieda_title                  — IEDA 批號抬頭資料
--      • lots_result                 — 批號逐筆測試結果（FK → lots_info）
--      • lots_statistic              — 批號統計值（FK → lots_info）
--      • recovery_rate               — 回收率記錄
--      • tester_device_info          — 測試機台裝置資訊
--      • tester_production_analysis  — 機台生產分析（FK → tester_device_info）
--      • tester_status               — 機台測試狀態（FK → tester_device_info）
--      • tester_sw_version           — 機台軟體版本（FK → tester_device_info）
--      • ui_status                   — UI 版本狀態
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

-- 2. 整合既有系統資料表（若已存在可跳過本節）
CREATE TABLE `db_key` (
  `id` int NOT NULL AUTO_INCREMENT,
  `datetime` int DEFAULT NULL,
  `db_key` varchar(255) DEFAULT NULL,
  `recovery_rate` tinyint NOT NULL DEFAULT '0',
  `tester` tinyint NOT NULL DEFAULT '0',
  `test_result` tinyint NOT NULL DEFAULT '0',
  `fail_pin` tinyint NOT NULL DEFAULT '0',
  `check_status` tinyint NOT NULL DEFAULT '0',
  `import_status` tinyint NOT NULL DEFAULT '0',
  `mail` tinyint NOT NULL DEFAULT '0',
  `remark` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `db_key_ui_status` (
  `id` int NOT NULL AUTO_INCREMENT,
  `datetime` int DEFAULT NULL,
  `db_key` varchar(255) DEFAULT NULL,
  `ui_status` tinyint NOT NULL DEFAULT '0',
  `check_status` tinyint NOT NULL DEFAULT '0',
  `import_status` tinyint NOT NULL DEFAULT '0',
  `mail` tinyint NOT NULL DEFAULT '0',
  `remark` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `fail_pin_rate_info` (
  `id` int NOT NULL AUTO_INCREMENT,
  `mac_address` varchar(20) DEFAULT NULL,
  `db_key` varchar(255) DEFAULT NULL COMMENT 'db_version',
  `area` varchar(20) DEFAULT NULL,
  `factory` varchar(20) DEFAULT NULL,
  `os_machine` varchar(20) DEFAULT NULL,
  `ao_lot` varchar(50) DEFAULT NULL,
  `mode` varchar(20) DEFAULT NULL,
  `data_format` varchar(10) DEFAULT NULL,
  `file_name` varchar(50) DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `total` int DEFAULT NULL,
  `pass` int DEFAULT NULL,
  `open` int DEFAULT NULL,
  `short` int DEFAULT NULL,
  `lk` int DEFAULT NULL,
  `nVTEP` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `IDX_FAIL_PIN_RATE_INFO_DB_KEY` (`db_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `fail_pin_rate_list` (
  `id` int NOT NULL AUTO_INCREMENT,
  `fail_pin_rate_info_id` int DEFAULT NULL,
  `dut` int DEFAULT NULL,
  `sn_num` varchar(50) DEFAULT NULL,
  `site` int DEFAULT NULL,
  `fail_type` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fail_pin_rate_list_fail_pin_rate_info_id` (`fail_pin_rate_info_id`),
  CONSTRAINT `fail_pin_rate_list_fail_pin_rate_info` FOREIGN KEY (`fail_pin_rate_info_id`) REFERENCES `fail_pin_rate_info` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `fail_pin_rate_list_pin_ball` (
  `id` int NOT NULL AUTO_INCREMENT,
  `fail_pin_rate_list_id` int DEFAULT NULL,
  `pin` varchar(100) DEFAULT NULL,
  `ball` varchar(10) DEFAULT NULL,
  `remark` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fail_pin_rate_list_pin_ball_fail_pin_rate_list_id` (`fail_pin_rate_list_id`),
  CONSTRAINT `fail_pin_rate_list_pin_ball_fail_pin_rate_list` FOREIGN KEY (`fail_pin_rate_list_id`) REFERENCES `fail_pin_rate_list` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `fail_pin_rate_test_result` (
  `id` int NOT NULL AUTO_INCREMENT,
  `fail_pin_rate_list_id` int DEFAULT NULL,
  `item_name` varchar(50) DEFAULT '',
  `open` double DEFAULT NULL,
  `short` double DEFAULT NULL,
  `vmeas` double DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fail_pin_rate_test_result_fail_pin_rate_list_id` (`fail_pin_rate_list_id`),
  CONSTRAINT `fail_pin_rate_test_result_fail_pin_rate_list` FOREIGN KEY (`fail_pin_rate_list_id`) REFERENCES `fail_pin_rate_list` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ieda_content` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title_id` int DEFAULT NULL,
  `touch_down` int DEFAULT NULL,
  `sw_bin` int DEFAULT NULL,
  `vi_result` int DEFAULT NULL,
  `site_index` int DEFAULT NULL,
  `index_time` double DEFAULT NULL,
  `test_time` double DEFAULT NULL,
  `re_probing_flag_retest_flag` int DEFAULT NULL,
  `handler_arm` int DEFAULT NULL,
  `temperature` varchar(8) DEFAULT '',
  `package_start_time` datetime DEFAULT NULL,
  `handler_arm_force` double DEFAULT NULL,
  `wafer_id` varchar(12) DEFAULT '',
  `wafer_x` varchar(4) DEFAULT '',
  `wafer_y` varchar(4) DEFAULT '',
  `serial_number` int DEFAULT NULL,
  `efuse_string_1` varchar(53) DEFAULT '',
  `efuse_string_2` varchar(64) DEFAULT '',
  `efuse_string_3` varchar(46) DEFAULT '',
  `efuse_string_4` varchar(37) DEFAULT '',
  `spare_para_1` varchar(6) DEFAULT '',
  `spare_para_2` varchar(6) DEFAULT '',
  `spare_para_3` varchar(6) DEFAULT '',
  `spare_para_4` varchar(20) DEFAULT '',
  `soft_bin_name` varchar(20) DEFAULT '',
  `hard_bin_number` int DEFAULT NULL,
  `hard_bin_name` varchar(20) DEFAULT '',
  `ocr_laser_mark_qr_code` varchar(25) DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `ieda_content_title_id` (`title_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ieda_title` (
  `id` int NOT NULL AUTO_INCREMENT,
  `ase_lot` varchar(30) DEFAULT '',
  `lot_id` varchar(30) DEFAULT '',
  `sub_lot` varchar(2) DEFAULT '',
  `device` varchar(32) DEFAULT '',
  `mpw_code` varchar(4) DEFAULT '',
  `produce_code` varchar(6) DEFAULT '',
  `tester_id` varchar(8) DEFAULT '',
  `oper_id` varchar(8) DEFAULT '',
  `test_program` varchar(50) DEFAULT '',
  `start_time` datetime DEFAULT NULL,
  `end_time` datetime DEFAULT NULL,
  `socket_lid` varchar(12) DEFAULT '',
  `load_board_id` varchar(12) DEFAULT '',
  `bd_file` varchar(20) DEFAULT '',
  `package_notch` varchar(1) DEFAULT '',
  `sort_stage` varchar(1) DEFAULT '',
  `test_site` varchar(8) DEFAULT '',
  `fd_file` varchar(20) DEFAULT '',
  `cover_id_side_blade` varchar(20) DEFAULT '',
  `socket_id` varchar(20) DEFAULT '',
  `handler_id` varchar(20) DEFAULT '',
  `device_rev` varchar(10) DEFAULT '',
  `tsmc_lot_id` varchar(12) DEFAULT '',
  `assembly_start_date` varchar(11) DEFAULT '',
  `assembly_end_date` varchar(11) DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `ieda_title_ase_lot` (`ase_lot`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `lots_info` (
  `id` int NOT NULL AUTO_INCREMENT,
  `version` varchar(255) DEFAULT NULL,
  `mac_address` varchar(255) DEFAULT NULL,
  `db_key` varchar(255) DEFAULT NULL,
  `customer` varchar(255) DEFAULT NULL,
  `package` varchar(255) DEFAULT NULL,
  `bonding_diagram` varchar(255) DEFAULT NULL,
  `program` varchar(255) DEFAULT NULL,
  `device` varchar(255) DEFAULT NULL,
  `control_lot` varchar(255) DEFAULT NULL,
  `ao_lot` varchar(255) DEFAULT NULL,
  `os_machine_id` varchar(255) DEFAULT NULL,
  `os_test_board_id` varchar(255) DEFAULT NULL,
  `user_id` varchar(255) DEFAULT NULL,
  `schedule_lot` varchar(255) DEFAULT NULL,
  `file_name` varchar(255) DEFAULT NULL,
  `yield` double DEFAULT NULL,
  `total` int DEFAULT NULL,
  `pass` int DEFAULT NULL,
  `open_pin_fail` int DEFAULT NULL,
  `short_pin_fail` int DEFAULT NULL,
  `leakage_pin_fail` int DEFAULT NULL,
  `nvtep_pin_fail` int DEFAULT NULL,
  `total_ppm` double DEFAULT NULL,
  `open_pin_fail_ppm` double DEFAULT NULL,
  `short_pin_fail_ppm` double DEFAULT NULL,
  `leakage_pin_fail_ppm` double DEFAULT NULL,
  `nvtep_pin_fail_ppm` double DEFAULT NULL,
  `total_test_items` int DEFAULT NULL,
  `average_test_time` double DEFAULT NULL,
  `clear_count` double DEFAULT NULL,
  `start` datetime DEFAULT NULL,
  `stop` datetime DEFAULT NULL,
  `pass_without_ocr` int DEFAULT NULL,
  `open` int DEFAULT NULL,
  `open_without_ocr` int DEFAULT NULL,
  `short_others` int DEFAULT NULL,
  `pass_without_ocr_ppm` double DEFAULT NULL,
  `open_ppm` double DEFAULT NULL,
  `open_without_ocr_ppm` double DEFAULT NULL,
  `short_others_ppm` double DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `file_name` (`file_name`),
  KEY `IDX_LOTS_INFO_DB_KEY` (`db_key`),
  KEY `idx_program` (`program`),
  KEY `idx_customer ` (`customer`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `lots_result` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `lot_id` int DEFAULT NULL,
  `serial` int DEFAULT NULL,
  `retest_loc` varchar(2) NOT NULL DEFAULT '',
  `sn_num` varchar(50) DEFAULT NULL,
  `site_id` int DEFAULT NULL,
  `x` int DEFAULT NULL,
  `y` int DEFAULT NULL,
  `hbin` varchar(30) DEFAULT NULL,
  `pass/fail` enum('pass','fail') DEFAULT NULL,
  `test_time` double DEFAULT NULL,
  `index_time` double DEFAULT NULL,
  `real_time` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `lot_id` (`lot_id`),
  CONSTRAINT `lots_result_ibfk_1` FOREIGN KEY (`lot_id`) REFERENCES `lots_info` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `lots_statistic` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `lot_id` int DEFAULT NULL,
  `site_id` int DEFAULT NULL,
  `item_no` int DEFAULT NULL,
  `item_name` varchar(50) DEFAULT '',
  `net_name` varchar(50) DEFAULT '',
  `force` double DEFAULT NULL,
  `wait_time` double DEFAULT NULL,
  `spec_max` double DEFAULT NULL,
  `spec_min` double DEFAULT NULL,
  `pass` int DEFAULT NULL,
  `pass_n` int DEFAULT NULL,
  `fail` int DEFAULT NULL,
  `min` double DEFAULT NULL,
  `max` double DEFAULT NULL,
  `avg` double DEFAULT NULL,
  `avg_2` double DEFAULT NULL COMMENT '平方和平均',
  `stdev` double DEFAULT NULL,
  `cp` double DEFAULT NULL,
  `cpk` double DEFAULT NULL,
  `unit` varchar(20) DEFAULT '',
  `value` json DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `lots_statistic_lot_id` (`lot_id`),
  KEY `item_no` (`item_no`),
  CONSTRAINT `lots_statistic_lot_id` FOREIGN KEY (`lot_id`) REFERENCES `lots_info` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `recovery_rate` (
  `id` int NOT NULL AUTO_INCREMENT,
  `db_key` varchar(255) DEFAULT NULL,
  `area` varchar(20) DEFAULT NULL,
  `factory` varchar(20) DEFAULT NULL,
  `os_machine` varchar(20) DEFAULT NULL,
  `customer` varchar(20) DEFAULT NULL,
  `program` varchar(255) DEFAULT NULL,
  `ao_lot` varchar(20) DEFAULT NULL,
  `mode` varchar(20) DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `test_item` varchar(50) DEFAULT NULL,
  `defect_mode` varchar(20) DEFAULT NULL,
  `re_test_pass` varchar(20) DEFAULT NULL,
  `fail_pin_count` int DEFAULT '0',
  `total_unit` int DEFAULT '0',
  `recovery_rate` double DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `IDX_RECOVERY_RATE_DB_KEY` (`db_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `tester_device_info` (
  `id` int NOT NULL AUTO_INCREMENT,
  `db_key` varchar(255) DEFAULT 'N/A',
  `mac_address` varchar(20) DEFAULT NULL,
  `ip_address` varchar(20) DEFAULT NULL,
  `area` varchar(50) DEFAULT NULL,
  `factory` varchar(20) DEFAULT NULL,
  `machine_type` varchar(20) DEFAULT NULL,
  `machine_id` varchar(50) DEFAULT NULL,
  `customer` varchar(20) DEFAULT NULL,
  `device_production` varchar(50) DEFAULT NULL,
  `device_engineer` varchar(50) DEFAULT NULL,
  `test_program` varchar(255) DEFAULT NULL,
  `program_path` varchar(255) DEFAULT NULL,
  `lot_id` varchar(50) DEFAULT NULL,
  `wafer_id` varchar(50) DEFAULT NULL,
  `execution_mode` varchar(20) DEFAULT NULL,
  `prober/handler` varchar(50) DEFAULT NULL,
  `L/B_id` varchar(50) DEFAULT NULL,
  `dut_board_type` varchar(20) DEFAULT NULL,
  `efficiency_check` int DEFAULT NULL,
  `ui_flow_checksum` int DEFAULT NULL,
  `yield` double DEFAULT NULL,
  `file_type` varchar(20) DEFAULT NULL,
  `start_time` datetime DEFAULT NULL,
  `end_time` datetime DEFAULT NULL,
  `lead_count` int DEFAULT NULL,
  `site_qty` int DEFAULT NULL,
  `bd_leak` int DEFAULT NULL,
  `pg_leak` int DEFAULT NULL,
  `wireclose_leak` int DEFAULT NULL,
  `handler_type` varchar(20) DEFAULT NULL,
  `handler_sw_version` varchar(20) DEFAULT NULL,
  `handler_repair_start_time` varchar(20) DEFAULT NULL,
  `handler_repair_end_time` varchar(20) DEFAULT NULL,
  `doe_flag` int DEFAULT NULL,
  `hso_mode` varchar(50) DEFAULT NULL,
  `mp_api_log` int DEFAULT NULL,
  `mp_tt_log` int DEFAULT NULL,
  `smart_delay_enable` int DEFAULT NULL,
  `smart_delay_time` double DEFAULT NULL,
  `atv_information` int DEFAULT '0',
  `NetlistInfo` int DEFAULT NULL,
  `TP_CheckerDetectionResults` int DEFAULT NULL,
  `PG_LeakageEnabled` int DEFAULT NULL,
  `LeakageEnabled` int DEFAULT NULL,
  `EnhanceTestTtemQTY` int DEFAULT NULL,
  `First_Yield` double DEFAULT NULL,
  `shortFailAnalysisFlag` int DEFAULT NULL,
  `OSVersion` varchar(50) DEFAULT NULL,
  `DCT_Type` varchar(50) DEFAULT NULL,
  `DCT_Qty` int DEFAULT NULL,
  `DCT_CH_Qty` int DEFAULT NULL,
  `LB_Type` varchar(50) DEFAULT NULL,
  `ConnecterType` varchar(50) DEFAULT NULL,
  `Short_Plate_Check_Status` varchar(64) DEFAULT NULL,
  `Short_Plate_Check_Pin_qty_match` varchar(16) DEFAULT NULL,
  `TP_HighRiskLot` int DEFAULT NULL,
  `TP_WarningLot` int DEFAULT NULL,
  `TP_OverkillQTY` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `IDX_TESTER_DEVICE_INFO_DB_KEY` (`db_key`),
  KEY `idx_test_program` (`test_program`),
  KEY `idx_customer ` (`customer`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `tester_production_analysis` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_info_id` int DEFAULT NULL,
  `site1_yield` double DEFAULT NULL,
  `site2_yield` double DEFAULT NULL,
  `site3_yield` double DEFAULT NULL,
  `site4_yield` double DEFAULT NULL,
  `site5_yield` double DEFAULT NULL,
  `site6_yield` double DEFAULT NULL,
  `site7_yield` double DEFAULT NULL,
  `site8_yield` double DEFAULT NULL,
  `site9_yield` double DEFAULT NULL,
  `site10_yield` double DEFAULT NULL,
  `site11_yield` double DEFAULT NULL,
  `site12_yield` double DEFAULT NULL,
  `site13_yield` double DEFAULT NULL,
  `site14_yield` double DEFAULT NULL,
  `site15_yield` double DEFAULT NULL,
  `site16_yield` double DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `tester_device_info_tester_production_analysis` (`device_info_id`),
  CONSTRAINT `tester_device_info_tester_production_analysis` FOREIGN KEY (`device_info_id`) REFERENCES `tester_device_info` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `tester_status` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_info_id` int DEFAULT NULL,
  `dpw` varchar(20) DEFAULT NULL,
  `duts` int DEFAULT NULL,
  `csv_name` varchar(100) DEFAULT NULL,
  `uph` double DEFAULT NULL,
  `avg_test_time` double DEFAULT NULL,
  `max_test_time` double DEFAULT NULL,
  `min_test_time` double DEFAULT NULL,
  `avg_index_test_time` double DEFAULT NULL,
  `max_index_test_time` double DEFAULT NULL,
  `min_index_test_time` double DEFAULT NULL,
  `diff_time_die` double DEFAULT NULL,
  `end_time_die` double DEFAULT NULL,
  `first_time_die` double DEFAULT NULL,
  `diff_time_file` double DEFAULT NULL,
  `conclusion_file_path` varchar(255) DEFAULT NULL,
  `raw_date_file_path` varchar(255) DEFAULT NULL,
  `s2s_diff_file_path` varchar(255) DEFAULT NULL,
  `pass/fail` enum('pass','fail') DEFAULT NULL,
  `case_a_result` varchar(20) DEFAULT NULL,
  `case_b_result` varchar(20) DEFAULT NULL,
  `case_c_result` varchar(20) DEFAULT NULL,
  `pui_result` varchar(20) DEFAULT NULL,
  `pui_respond` varchar(255) DEFAULT NULL,
  `pui_file_type` varchar(20) DEFAULT NULL,
  `phi_result` varchar(20) DEFAULT NULL,
  `phi_respond` varchar(255) DEFAULT NULL,
  `phi_file_type` varchar(20) DEFAULT NULL,
  `tp_result` varchar(20) DEFAULT NULL,
  `tp_respond` varchar(100) DEFAULT NULL,
  `manual_data_module_csv_g_result` varchar(20) DEFAULT NULL,
  `manual_data_module_csv_g_respond` varchar(20) DEFAULT NULL,
  `data_module_stdf_g_result` varchar(20) DEFAULT NULL,
  `data_module_stdf_g_respond` varchar(20) DEFAULT NULL,
  `data_module_txt_g_result` varchar(20) DEFAULT NULL,
  `data_module_txt_g_respond` varchar(20) DEFAULT NULL,
  `data_module_std_g_result` varchar(20) DEFAULT NULL,
  `data_module_std_g_respond` varchar(20) DEFAULT NULL,
  `test_time_module_csv_g_result` varchar(20) DEFAULT NULL,
  `test_time_module_csv_g_respond` varchar(20) DEFAULT NULL,
  `data_module_act_smart1_txt_result` varchar(20) DEFAULT NULL,
  `data_module_act_smart1_txt_respond` varchar(20) DEFAULT NULL,
  `data_module_asekh_smart1_xml_result` varchar(100) DEFAULT NULL,
  `data_module_asekh_smart1_xml_respond` varchar(255) DEFAULT NULL,
  `data_module_act_fail_log_result` varchar(20) DEFAULT NULL,
  `data_module_act_fail_log_respond` varchar(20) DEFAULT NULL,
  `vim_result` varchar(20) DEFAULT NULL,
  `vim_respond` varchar(20) DEFAULT NULL,
  `vim_open_result` varchar(20) DEFAULT NULL,
  `vim_open_respond` varchar(20) DEFAULT NULL,
  `vicbit_result` varchar(20) DEFAULT NULL,
  `vicbit_respond` varchar(20) DEFAULT NULL,
  `vicbit_open_result` varchar(20) DEFAULT NULL,
  `vicbit_open_respond` varchar(20) DEFAULT NULL,
  `pattern_result` varchar(20) DEFAULT NULL,
  `pattern_respond` varchar(20) DEFAULT NULL,
  `SWT_result` varchar(20) DEFAULT NULL,
  `SWT_respond` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `tester_device_info_tester_status` (`device_info_id`),
  CONSTRAINT `tester_device_info_tester_status` FOREIGN KEY (`device_info_id`) REFERENCES `tester_device_info` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `tester_sw_version` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_info_id` int DEFAULT NULL,
  `pui_version` varchar(20) DEFAULT NULL,
  `library_version` varchar(50) DEFAULT NULL,
  `virtual_instrument_version` varchar(50) DEFAULT NULL,
  `mingw_version` varchar(20) DEFAULT NULL,
  `all_md5_checksum` varchar(20) DEFAULT NULL,
  `auto_learn_ui_md5` varchar(32) DEFAULT NULL,
  `dct_product_file_setting_ui_md5` varchar(32) DEFAULT NULL,
  `dct_login_ui_md5` varchar(32) DEFAULT NULL,
  `os_self_diag_2k_md5` varchar(32) DEFAULT NULL,
  `pattonkan_ui_md5` varchar(32) DEFAULT NULL,
  `dct_iv_curve_tool_md5` varchar(32) DEFAULT NULL,
  `os_tester_100ma_vi_md5` varchar(32) DEFAULT NULL,
  `os_tester_2a_vi_md5` varchar(32) DEFAULT NULL,
  `os_tester_lcr_meter_md5` varchar(32) DEFAULT NULL,
  `wire_assignment_tool_md5` varchar(32) DEFAULT NULL,
  `bga_highlight_tool_md5` varchar(32) DEFAULT NULL,
  `simplification_ui_md5` varchar(32) DEFAULT NULL,
  `os_scan_tool_md5` varchar(32) DEFAULT NULL,
  `dct_uploadtp_ui_md5` varchar(32) DEFAULT NULL,
  `dct_autodownloadtp_md5` varchar(32) DEFAULT NULL,
  `autolearn_dll_md5` varchar(32) DEFAULT NULL,
  `integration_simplified_tp_lib_dll_md5` varchar(32) DEFAULT NULL,
  `libpublic_module_dll_md5` varchar(32) DEFAULT NULL,
  `liblpsa_libs_dll_md5` varchar(32) DEFAULT NULL,
  `libvim_total_libs_dll_md5` varchar(32) DEFAULT NULL,
  `slot0_dll_md5` varchar(32) DEFAULT NULL,
  `libtid_mem_libs_dll_md5` varchar(32) DEFAULT NULL,
  `sys_tid_libs_dll_md5` varchar(32) DEFAULT NULL,
  `confirm_ftp_upload_dll_md5` varchar(32) DEFAULT NULL,
  `confirm_ftp_upload_g_dll_md5` varchar(32) DEFAULT NULL,
  `ctp_hontech_ttl_phi_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asecl_assy_csv_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asecl_fail_log_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asecl_smart1_txt_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asekh_a5_csv_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asekh_a5_summary_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asekh_change_kit_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asekh_recoverrate_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asekh_smart1_txt_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_asekh_smart1_xml_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_assy_txt_g_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_ca_csv_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_create_spec_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_csv_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_csv_g_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_intime_summary_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_std_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_stdf_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_stdf_g_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_std_g_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_txt_dll_md5` varchar(32) DEFAULT NULL,
  `data_module_txt_g_dll_md5` varchar(32) DEFAULT NULL,
  `hello_phi_dll_md5` varchar(32) DEFAULT NULL,
  `high_light_sys_dll_md5` varchar(32) DEFAULT NULL,
  `hontech_phi_dll_md5` varchar(32) DEFAULT NULL,
  `k2_150a_dll_md5` varchar(32) DEFAULT NULL,
  `manual_data_module_csv_g_dll_md5` varchar(32) DEFAULT NULL,
  `secs_gem_connect_dll_md5` varchar(32) DEFAULT NULL,
  `srm_phi_dll_md5` varchar(32) DEFAULT NULL,
  `tel_p12_phi_dll_md5` varchar(32) DEFAULT NULL,
  `test_time_module_csv_g_dll_md5` varchar(32) DEFAULT NULL,
  `rf_offset_data_module_csv_g_dll_md5` varchar(32) DEFAULT NULL,
  `2g_pat_module_inprocess_dma_dll_md5` varchar(32) DEFAULT NULL,
  `2g_multi_process_dll_md5` varchar(32) DEFAULT NULL,
  `auto_learn_pui_version` varchar(32) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `tester_device_info_tester_sw_version` (`device_info_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ui_status` (
  `id` int NOT NULL AUTO_INCREMENT,
  `mac_address` varchar(50) DEFAULT NULL,
  `area` varchar(50) DEFAULT NULL,
  `factory` varchar(10) DEFAULT NULL,
  `os_machine` varchar(50) DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `auto_learn` int DEFAULT NULL,
  `dct_product_file_setting_ui` int DEFAULT NULL,
  `dct_login_ui` int DEFAULT NULL,
  `os_self_diag_2k` int DEFAULT NULL,
  `pattonkan_ui` int DEFAULT NULL,
  `dct_i_v_curve_tool` int DEFAULT NULL,
  `os_tester_100ma_vi` int DEFAULT NULL,
  `os_tester_2a_vi` int DEFAULT NULL,
  `os_tester_lcr_meter` int DEFAULT NULL,
  `wire_assignment_tool` int DEFAULT NULL,
  `bga_highlight_tool` int DEFAULT NULL,
  `simplificationui` int DEFAULT NULL,
  `os_scan_tool` int DEFAULT NULL,
  `dct_uploadtp_ui` int DEFAULT NULL,
  `dct_autodownloadtp` int DEFAULT NULL,
  `dct_sw_control_tool` int DEFAULT NULL,
  `dct_downloadtp_kh` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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

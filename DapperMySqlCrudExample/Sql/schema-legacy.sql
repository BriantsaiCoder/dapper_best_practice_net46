-- =============================================================================
-- Dapper Best Practice (.NET 4.6 + MySQL) — 既有系統整合資料表
-- =============================================================================
-- 本檔案僅收錄核心資料表的外鍵依賴 lots_info。
-- 若您的環境已存在 lots_info，可跳過本檔。
--
-- 使用方式：
--   1. 若為全新環境，請先執行本檔建立 lots_info
--   2. 再執行 schema.sql 建立核心 9 張表
-- =============================================================================

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
  KEY `idx_customer` (`customer`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================================
-- Dapper Best Practice (.NET 4.6 + MySQL) — 資料庫 Schema
-- =============================================================================
-- 請先建立資料庫：CREATE DATABASE your_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
--
-- ★ 執行順序：若為全新環境，請先執行 schema-legacy.sql 建立 lots_info 等外鍵依賴資料表。
--
-- ★ 本檔包含 8 張核心資料表，由 Repository 直接管理：
--    1. detection_methods            — 偵測方法主表
--    2. anomaly_lots                 — 異常批號主表
--    3. anomaly_units                — 異常明細表（含測項與 Unit 層，unit_id='' 表示無 Unit 的測項層異常）
--    4. anomaly_lot_process_mapping  — 批號 Process Mapping
--    5. anomaly_unit_process_mapping — Unit Process Mapping
--    6. detection_specs              — Spec 規格表
--    7. site_test_statistics         — Site 測項統計值表
--    8. good_lots                    — 好批批號記錄表
-- =============================================================================

create table detection_methods
(
    id          tinyint unsigned auto_increment
        primary key,
    method_key  varchar(20)                        not null,
    method_name varchar(50)                        not null,
    created_at  datetime default CURRENT_TIMESTAMP null,
    updated_at  datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint method_key
        unique (method_key)
)
    collate = utf8mb4_unicode_ci;

-- Seed detection methods (idempotent)
INSERT INTO detection_methods (method_key, method_name)
VALUES
    ('YIELD', '良率偵測'),
    ('SITE_MEAN', 'Site平均值偵測'),
    ('SITE_STD', 'Site標準差偵測'),
    ('UNIT_MEAN', 'Unit平均值偵測') AS new
ON DUPLICATE KEY UPDATE
    method_name = new.method_name,
    updated_at = CURRENT_TIMESTAMP;

create table detection_specs
(
    id                   bigint auto_increment
        primary key,
    program              varchar(100)                       not null,
    test_item_name       varchar(100)                       null,
    site_id              int unsigned                       not null,
    detection_method_id  tinyint unsigned                   not null,
    spec_upper_limit     double                     null,
    spec_lower_limit     double                     null,
    spec_calc_start_time datetime                           not null,
    spec_calc_end_time   datetime                           not null,
    spec_calc_mean       double                     null,
    spec_calc_std        double                     null,
    created_at           datetime default CURRENT_TIMESTAMP null,
    updated_at           datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint fk_specs_detection_method
        foreign key (detection_method_id) references detection_methods (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create index idx_program_item_method
    on detection_specs (program, test_item_name, detection_method_id);

create index idx_program_method
    on detection_specs (program, detection_method_id);

create table lots_info
(
    id                   int auto_increment
        primary key,
    version              varchar(255) null,
    mac_address          varchar(255) null,
    db_key               varchar(255) null,
    customer             varchar(255) null,
    package              varchar(255) null,
    bonding_diagram      varchar(255) null,
    program              varchar(255) null,
    device               varchar(255) null,
    control_lot          varchar(255) null,
    ao_lot               varchar(255) null,
    os_machine_id        varchar(255) null,
    os_test_board_id     varchar(255) null,
    user_id              varchar(255) null,
    schedule_lot         varchar(255) null,
    file_name            varchar(255) null,
    yield                double       null,
    total                int          null,
    pass                 int          null,
    open_pin_fail        int          null,
    short_pin_fail       int          null,
    leakage_pin_fail     int          null,
    nvtep_pin_fail       int          null,
    total_ppm            double       null,
    open_pin_fail_ppm    double       null,
    short_pin_fail_ppm   double       null,
    leakage_pin_fail_ppm double       null,
    nvtep_pin_fail_ppm   double       null,
    total_test_items     int          null,
    average_test_time    double       null,
    clear_count          double       null,
    start                datetime     null,
    stop                 datetime     null,
    pass_without_ocr     int          null,
    open                 int          null,
    open_without_ocr     int          null,
    short_others         int          null,
    pass_without_ocr_ppm double       null,
    open_ppm             double       null,
    open_without_ocr_ppm double       null,
    short_others_ppm     double       null,
    constraint file_name
        unique (file_name)
)
    collate = utf8mb4_unicode_ci
    row_format = DYNAMIC;

create table anomaly_lots
(
    id                  bigint auto_increment
        primary key,
    lots_info_id        int                                not null,
    site_id        int unsigned                       not null,
    detection_method_id tinyint unsigned                   not null,
    detection_value     double                     null,
    offset_value        double                     null,
    spec_upper_limit    double                     null,
    spec_lower_limit    double                     null,
    created_at          datetime default CURRENT_TIMESTAMP null,
    updated_at          datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint unq_lot_method
        unique (lots_info_id, site_id, detection_method_id),
    constraint fk_anomaly_lots_detection_method
        foreign key (detection_method_id) references detection_methods (id)
            on update cascade on delete cascade,
    constraint fk_anomaly_lots_info
        foreign key (lots_info_id) references lots_info (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create table anomaly_lot_process_mapping
(
    id            bigint auto_increment
        primary key,
    lots_info_id  int                                not null,
    plant_name    varchar(100)                       null,
    station_name  varchar(100)                       null,
    machine_id    varchar(50)                        null,
    trackin_user  varchar(50)                        null,
    trackout_user varchar(50)                       null,
    recipe        varchar(50)                        null,
    created_at    datetime default CURRENT_TIMESTAMP null,
    updated_at    datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint fk_lot_process_lots_info
        foreign key (lots_info_id) references lots_info (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create index anomaly_lot_process_mapping_lots_info_id
    on anomaly_lot_process_mapping (lots_info_id);

create table anomaly_units
(
    id               bigint auto_increment
        primary key,
    anomaly_lot_id   bigint                                not null,
    test_item_name   varchar(100)                          not null,
    site_id          int unsigned                          not null,
    unit_id          VARCHAR(50) NULL DEFAULT NULL,
    detection_value  double                        null,
    offset_value     double                       null,
    spec_upper_limit double                        null,
    spec_lower_limit double                        null,
    created_at       datetime    default CURRENT_TIMESTAMP null,
    updated_at       datetime    default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint unq_lot_item_unit
        unique (anomaly_lot_id, test_item_name, unit_id),
    constraint fk_units_anomaly_lot
        foreign key (anomaly_lot_id) references anomaly_lots (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create table anomaly_unit_process_mapping
(
    id              bigint auto_increment
        primary key,
    anomaly_unit_id bigint                             not null,
    boat_id         varchar(50)                        not null,
    boat_x          smallint                           not null,
    boat_y          smallint                           not null,
    wafer_barcode   varchar(50)                        not null,
    wafer_id        varchar(50)                        not null,
    wafer_x         smallint                           not null,
    wafer_y         smallint                           not null,
    substrate_id    varchar(50)                        not null,
    substrate_x     smallint                           not null,
    substrate_y     smallint                           not null,
    wafer_max_x     smallint                           not null,
    wafer_max_y     smallint                           not null,
    boat_max_x      smallint                           not null,
    boat_max_y      smallint                           not null,
    plant_name      varchar(100)                       null,
    station_name    varchar(100)                       null,
    equipment_id    varchar(50)                        null,
    created_at      datetime default CURRENT_TIMESTAMP null,
    updated_at      datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint fk_unit_process_anomaly_unit
        foreign key (anomaly_unit_id) references anomaly_units (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create table good_lots
(
    id                  bigint auto_increment
        primary key,
    lots_info_id        int                                not null,
    detection_method_id tinyint unsigned                   not null,
    created_at          datetime default CURRENT_TIMESTAMP null,
    updated_at          datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint unq_lot_method
        unique (lots_info_id, detection_method_id),
    constraint fk_good_lots_detection_method
        foreign key (detection_method_id) references detection_methods (id)
            on update cascade on delete cascade,
    constraint fk_good_lots_info
        foreign key (lots_info_id) references lots_info (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create index IDX_LOTS_INFO_DB_KEY
    on lots_info (db_key);

create table site_test_statistics
(
    id             bigint auto_increment
        primary key,
    lots_info_id   int                                not null,
    program        varchar(100)                       not null,
    site_id        int unsigned                       not null,
    test_item_name varchar(100)                       not null,
    mean_value     double                     null,
    max_value      double                     null,
    min_value      double                     null,
    std_value      double                     null,
    cp_value      double                     null,
    cpk_value      double                     null,
    tester_id      varchar(50)                        null,
    start_time     datetime                           null,
    end_time       datetime                           null,
    created_at     datetime default CURRENT_TIMESTAMP null,
    updated_at     datetime default CURRENT_TIMESTAMP null on update CURRENT_TIMESTAMP,
    constraint unq_lot_site_item
        unique (lots_info_id, site_id, test_item_name),
    constraint fk_site_test_statistics_lots_info
        foreign key (lots_info_id) references lots_info (id)
            on update cascade on delete cascade
)
    collate = utf8mb4_unicode_ci;

create index idx_program_site_item_time
    on site_test_statistics (program, site_id, test_item_name, start_time);

create index idx_start_time
    on site_test_statistics (start_time);

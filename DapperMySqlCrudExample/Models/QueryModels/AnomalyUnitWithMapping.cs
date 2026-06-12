namespace DapperMySqlCrudExample.Models.QueryModels
{
    /// <summary>
    /// 異常明細與製程追溯的複合唯讀 DTO，對應
    /// anomaly_units INNER JOIN anomaly_unit_process_mapping 的查詢結果。
    /// <para>僅用於 unit_id 非空字串（有 Unit）的列，無 Unit 的測項層異常不會出現在此結果集。</para>
    /// </summary>
    public sealed class AnomalyUnitWithMapping
    {
        // ── anomaly_units 欄位 ──────────────────────────────────────────

        /// <summary>anomaly_units 主鍵。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常批號 ID（外鍵 anomaly_lots.id）。</summary>
        public long AnomalyLotId { get; set; }

        /// <summary>測試項目名稱。</summary>
        public string TestItemName { get; set; }

        /// <summary>測試 Site 編號。</summary>
        public uint SiteId { get; set; }

        /// <summary>Unit 識別碼（本 DTO 只出現非空字串的列）。</summary>
        public string UnitId { get; set; }

        /// <summary>Unit 層偵測值；允許 Null。</summary>
        public decimal? DetectionValue { get; set; }

        /// <summary>偵測值與規格的偏移量；允許 Null。</summary>
        public decimal? OffsetValue { get; set; }

        /// <summary>規格上限；允許 Null。</summary>
        public decimal? SpecUpperLimit { get; set; }

        /// <summary>規格下限；允許 Null。</summary>
        public decimal? SpecLowerLimit { get; set; }

        // ── anomaly_unit_process_mapping 欄位 ───────────────────────────

        /// <summary>anomaly_unit_process_mapping 主鍵。</summary>
        public long MappingId { get; set; }

        /// <summary>Boat 識別碼。</summary>
        public string BoatId { get; set; }

        /// <summary>Unit 在 Boat 上的 X 座標。</summary>
        public short BoatX { get; set; }

        /// <summary>Unit 在 Boat 上的 Y 座標。</summary>
        public short BoatY { get; set; }

        /// <summary>Wafer 條碼。</summary>
        public string WaferBarcode { get; set; }

        /// <summary>Wafer 識別碼。</summary>
        public string WaferId { get; set; }

        /// <summary>Unit 在 Wafer 上的 X 座標。</summary>
        public short WaferX { get; set; }

        /// <summary>Unit 在 Wafer 上的 Y 座標。</summary>
        public short WaferY { get; set; }

        /// <summary>Substrate 識別碼。</summary>
        public string SubstrateId { get; set; }

        /// <summary>Unit 在 Substrate 上的 X 座標。</summary>
        public short SubstrateX { get; set; }

        /// <summary>Unit 在 Substrate 上的 Y 座標。</summary>
        public short SubstrateY { get; set; }

        /// <summary>Wafer 最大 X 座標。</summary>
        public short WaferMaxX { get; set; }

        /// <summary>Wafer 最大 Y 座標。</summary>
        public short WaferMaxY { get; set; }

        /// <summary>Boat 最大 X 座標。</summary>
        public short BoatMaxX { get; set; }

        /// <summary>Boat 最大 Y 座標。</summary>
        public short BoatMaxY { get; set; }

        /// <summary>廠區名稱；允許 Null。</summary>
        public string MappingPlantName { get; set; }

        /// <summary>站點名稱。</summary>
        public string MappingStationName { get; set; }

        /// <summary>機台 ID。</summary>
        public string EquipmentId { get; set; }
    }
}

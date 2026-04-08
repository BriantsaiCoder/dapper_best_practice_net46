using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常 Unit Process Mapping，記錄 Unit 流經的 Boat ID、XY 座標與站點資訊。
    /// </summary>
    public sealed class AnomalyUnitProcessMapping
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常 Unit ID（外鍵 anomaly_units.id）。</summary>
        public long AnomalyUnitId { get; set; }

        /// <summary>Boat 識別碼。</summary>
        public string BoatId { get; set; }

        /// <summary>Unit 在 Boat 上的 X 座標位置。</summary>
        public short BoatPositionX { get; set; }

        /// <summary>Unit 在 Boat 上的 Y 座標位置。</summary>
        public short BoatPositionY { get; set; }

        /// <summary>Wafer 識別碼；允許 Null。</summary>
        public string WaferId { get; set; }

        /// <summary>Unit 在 Wafer 上的 X 座標位置；允許 Null。</summary>
        public short? WaferPositionX { get; set; }

        /// <summary>Unit 在 Wafer 上的 Y 座標位置；允許 Null。</summary>
        public short? WaferPositionY { get; set; }

        /// <summary>SBS 識別碼；允許 Null。</summary>
        public string SbsId { get; set; }

        /// <summary>Unit 在 SBS 上的 X 座標位置；允許 Null。</summary>
        public short? SbsPositionX { get; set; }

        /// <summary>Unit 在 SBS 上的 Y 座標位置；允許 Null。</summary>
        public short? SbsPositionY { get; set; }

        /// <summary>Unit 在此站點的處理時間；允許 Null。</summary>
        public DateTime? ProcessTime { get; set; }

        /// <summary>站點名稱。</summary>
        public string StationName { get; set; }

        /// <summary>機台 ID。</summary>
        public string EquipmentId { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

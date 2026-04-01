using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常 Unit Process Mapping，記錄 Unit 流經的 Boat ID、XY 座標與站點資訊。
    /// </summary>
    public class AnomalyUnitProcessMapping
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常 Unit ID（外鍵 anomaly_units.id）。</summary>
        public long AnomalyUnitId { get; set; }

        /// <summary>Boat 識別碼。</summary>
        public string BoatId { get; set; }

        /// <summary>Unit 在 Boat 上的 X 座標位置。</summary>
        public short PositionX { get; set; }

        /// <summary>Unit 在 Boat 上的 Y 座標位置。</summary>
        public short PositionY { get; set; }

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

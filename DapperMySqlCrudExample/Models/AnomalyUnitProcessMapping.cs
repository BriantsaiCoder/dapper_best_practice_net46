using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常 Unit Process Mapping，記錄 Unit 流經的 Boat / Wafer / Substrate 座標與站點資訊。
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
        public short BoatX { get; set; }

        /// <summary>Unit 在 Boat 上的 Y 座標位置。</summary>
        public short BoatY { get; set; }

        /// <summary>Wafer 條碼。</summary>
        public string WaferBarcode { get; set; }

        /// <summary>Wafer 識別碼。</summary>
        public string WaferId { get; set; }

        /// <summary>Unit 在 Wafer 上的 X 座標位置。</summary>
        public short WaferX { get; set; }

        /// <summary>Unit 在 Wafer 上的 Y 座標位置。</summary>
        public short WaferY { get; set; }

        /// <summary>Substrate 識別碼。</summary>
        public string SubstrateId { get; set; }

        /// <summary>Unit 在 Substrate 上的 X 座標位置。</summary>
        public short SubstrateX { get; set; }

        /// <summary>Unit 在 Substrate 上的 Y 座標位置。</summary>
        public short SubstrateY { get; set; }

        /// <summary>Wafer 最大 X 座標。</summary>
        public short WaferMaxX { get; set; }

        /// <summary>Wafer 最大 Y 座標。</summary>
        public short WaferMaxY { get; set; }

        /// <summary>Boat 最大 X 座標。</summary>
        public short BoatMaxX { get; set; }

        /// <summary>Boat 最大 Y 座標。</summary>
        public short BoatMaxY { get; set; }

        /// <summary>交易時間；允許 Null。</summary>
        public DateTime? TxnTime { get; set; }

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

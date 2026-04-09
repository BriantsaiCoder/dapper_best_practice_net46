using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常 Unit 明細，記錄批號中各 Unit 的偵測值與規格範圍。
    /// </summary>
    public sealed class AnomalyUnit
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常測項 ID（外鍵 anomaly_test_items.id）。</summary>
        public long AnomalyTestItemId { get; set; }

        /// <summary>Unit 識別碼。</summary>
        public string UnitId { get; set; }

        /// <summary>Unit 實際偵測值；允許 Null。</summary>
        public decimal? DetectionValue { get; set; }

        /// <summary>偵測值與規格的偏移量；允許 Null。</summary>
        public decimal? OffsetValue { get; set; }

        /// <summary>規格上限；允許 Null 表示尚未計算。</summary>
        public decimal? SpecUpperLimit { get; set; }

        /// <summary>規格下限；允許 Null 表示尚未計算。</summary>
        public decimal? SpecLowerLimit { get; set; }

        /// <summary>Spec 計算採樣的起始時間。</summary>
        public DateTime? SpecCalcStartTime { get; set; }

        /// <summary>Spec 計算採樣的結束時間。</summary>
        public DateTime? SpecCalcEndTime { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常測項明細，記錄批號中超出規格的測試項目名稱與偵測值。
    /// </summary>
    public class AnomalyTestItem
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常批號 ID（外鍵 anomaly_lots.id）。</summary>
        public long AnomalyLotId { get; set; }

        /// <summary>測試項目名稱。</summary>
        public string TestItemName { get; set; }

        /// <summary>實際偵測值；允許 Null。</summary>
        public decimal? DetectionValue { get; set; }

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

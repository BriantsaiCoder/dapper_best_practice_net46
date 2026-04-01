using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常批號主表，記錄觸發偵測的批號與對應的 Spec 上下限。
    /// </summary>
    public class AnomalyLot
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的批號資訊 ID（外鍵 lots_info.id）。</summary>
        public int LotsInfoId { get; set; }

        /// <summary>偵測方法 ID（外鍵 detection_methods.id）。</summary>
        public byte DetectionMethodId { get; set; }

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

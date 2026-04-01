using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 偵測規格表，儲存依 Program / TestItem / Site / MethodId 計算出的
    /// UCL 上限、LCL 下限及統計輔助值。
    /// </summary>
    public class DetectionSpec
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>測試程式代碼。</summary>
        public string Program { get; set; }

        /// <summary>測試項目名稱。</summary>
        public string TestItemName { get; set; }

        /// <summary>Site 編號（對應量測站點）。</summary>
        public uint SiteId { get; set; }

        /// <summary>偵測方法 ID（外鍵 detection_methods.id）。</summary>
        public byte DetectionMethodId { get; set; }

        /// <summary>規格上限（UCL = mean + 6σ）；允許 Null 表示尚未計算。</summary>
        public decimal? SpecUpperLimit { get; set; }

        /// <summary>規格下限（LCL = mean - 6σ）；允許 Null 表示尚未計算。</summary>
        public decimal? SpecLowerLimit { get; set; }

        /// <summary>本次 Spec 計算採樣資料的起始時間。</summary>
        public DateTime SpecCalcStartTime { get; set; }

        /// <summary>本次 Spec 計算採樣資料的結束時間。</summary>
        public DateTime SpecCalcEndTime { get; set; }

        /// <summary>採樣資料的平均值；允許 Null。</summary>
        public decimal? SpecCalcMean { get; set; }

        /// <summary>採樣資料的標準差；允許 Null。</summary>
        public decimal? SpecCalcStd { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// Site 測項統計值，儲存量測站點的平均值、Cp/Cpk 等製程能力指標。
    /// </summary>
    public class SiteTestStatistic
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的批號資訊 ID（外鍵 lots_info.id）。</summary>
        public int LotsInfoId { get; set; }

        /// <summary>測試程式代碼。</summary>
        public string Program { get; set; }

        /// <summary>Site 編號（對應量測站點）。</summary>
        public uint SiteId { get; set; }

        /// <summary>測試項目名稱。</summary>
        public string TestItemName { get; set; }

        /// <summary>本批 Site 量測值的平均值；允許 Null。</summary>
        public decimal? MeanValue { get; set; }

        /// <summary>本批 Site 量測值的最大值；允許 Null。</summary>
        public decimal? MaxValue { get; set; }

        /// <summary>本批 Site 量測值的最小值；允許 Null。</summary>
        public decimal? MinValue { get; set; }

        /// <summary>本批 Site 量測值的標準差；允許 Null。</summary>
        public decimal? StdValue { get; set; }

        /// <summary>製程能力指數 Cp；允許 Null。</summary>
        public decimal? CpValue { get; set; }

        /// <summary>製程能力指數 Cpk（含偏移修正）；允許 Null。</summary>
        public decimal? CpkValue { get; set; }

        /// <summary>測試機台 ID。</summary>
        public string TesterId { get; set; }

        /// <summary>批號在此 Site 的測試開始時間；允許 Null。</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>批號在此 Site 的測試結束時間；允許 Null。</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 偵測方法主表，定義異常偵測的方法代碼與適用層級。
    /// </summary>
    public sealed class DetectionMethod
    {
        /// <summary>主鍵（TINYINT，自動遞增）。</summary>
        public byte Id { get; set; }

        /// <summary>方法識別鍵（唯一索引，供程式判斷使用）。</summary>
        public string MethodKey { get; set; }

        /// <summary>方法名稱，供人類閱讀使用。</summary>
        public string MethodName { get; set; }

        /// <summary>是否需要測項明細層級（anomaly_test_items）。</summary>
        public bool HasTestItem { get; set; }

        /// <summary>是否需要 Unit 層級明細（anomaly_units）。</summary>
        public bool HasUnitLevel { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

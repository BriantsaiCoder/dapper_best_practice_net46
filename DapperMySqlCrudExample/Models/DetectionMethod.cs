using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 偵測方法主表，定義異常偵測的方法代碼與適用層級。
    /// </summary>
    /// <remarks>
    /// 【新手導讀】Dapper 的 Model 設計原則：
    /// 1. 不需要任何 ORM Attribute（如 EF 的 [Table]、[Column]），映射完全靠 Repository 中 SQL 的 AS 別名。
    /// 2. 所有屬性必須有 public setter（{ get; set; }），因為 Dapper 透過 setter 將查詢結果寫入物件。
    /// 3. 屬性名稱與 SQL AS 別名一致即可（大小寫不敏感），不需與資料庫欄位名稱相同。
    /// 本專案所有 Model 皆遵循此模式（sealed POCO class + public auto-properties）。
    /// </remarks>
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

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常明細，合併測項層與 Unit 層的異常記錄。
    /// <para>
    /// <b>UnitId = ""（空字串）</b>：無 Unit 的測項層異常，
    /// DetectionValue 儲存測項整體偵測值。
    /// </para>
    /// <para>
    /// <b>UnitId 有值</b>：Unit 層異常，
    /// DetectionValue 儲存該 Unit 的量測值。
    /// </para>
    /// </summary>
    public sealed class AnomalyUnit
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常批號 ID（外鍵 anomaly_lots.id）。</summary>
        public long AnomalyLotId { get; set; }

        /// <summary>測試項目名稱。</summary>
        public string TestItemName { get; set; }

        /// <summary>測試 Site 編號（INT UNSIGNED）。</summary>
        public uint SiteId { get; set; }

        /// <summary>
        /// Unit 識別碼。空字串（""）表示無 Unit 的測項層異常；有值表示 Unit 層異常。
        /// </summary>
        public string UnitId { get; set; }

        /// <summary>實際偵測值；允許 Null。</summary>
        public decimal? DetectionValue { get; set; }

        /// <summary>偵測值與規格的偏移量；允許 Null。</summary>
        public decimal? OffsetValue { get; set; }

        /// <summary>規格上限；允許 Null 表示尚未計算。</summary>
        public decimal? SpecUpperLimit { get; set; }

        /// <summary>規格下限；允許 Null 表示尚未計算。</summary>
        public decimal? SpecLowerLimit { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 異常批號 Process Mapping，記錄批號流經的站點與機台資訊。
    /// </summary>
    public sealed class AnomalyLotProcessMapping
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的異常批號 ID（外鍵 anomaly_lots.id）。</summary>
        public long AnomalyLotId { get; set; }

        /// <summary>站點名稱。</summary>
        public string StationName { get; set; }

        /// <summary>機台 ID。</summary>
        public string EquipmentId { get; set; }

        /// <summary>批號在此站點的處理時間；允許 Null。</summary>
        public DateTime? ProcessTime { get; set; }

        /// <summary>操作員 ID；允許 Null。</summary>
        public string OpId { get; set; }

        /// <summary>製程 Recipe 名稱；允許 Null。</summary>
        public string Recipe { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

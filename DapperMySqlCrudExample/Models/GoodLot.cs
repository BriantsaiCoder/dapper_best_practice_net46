using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// 好批批號記錄，用於數值偏差偵測模組的 Spec 計算採樣來源。
    /// </summary>
    public sealed class GoodLot
    {
        /// <summary>主鍵（自動遞增）。</summary>
        public long Id { get; set; }

        /// <summary>關聯的批號資訊 ID（外鍵 lots_info.id）。</summary>
        public int LotsInfoId { get; set; }

        /// <summary>偵測方法 ID（外鍵 detection_methods.id）。</summary>
        public byte DetectionMethodId { get; set; }

        /// <summary>記錄建立時間（由資料庫自動填入）。</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>記錄最後更新時間（由資料庫自動填入）。</summary>
        public DateTime UpdatedAt { get; set; }
    }
}

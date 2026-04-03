using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>
    /// SITE_MEAN 規格計算用的歷史統計資料列。
    /// 對應 site_test_statistics 的 mean_value 與 start_time 欄位。
    /// </summary>
    public class SiteMeanHistoryRow
    {
        public decimal MeanValue { get; set; }
        public DateTime? StartTime { get; set; }
    }
}

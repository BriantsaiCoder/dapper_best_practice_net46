using System;

namespace DapperMySqlCrudExample.Models.QueryModels
{
    /// <summary>
    /// 用於 SITE_MEAN 規格計算的歷史統計資料列。
    /// 僅包含計算所需的兩個欄位，由 <see cref="Repositories.SiteTestStatisticRepository.QuerySiteMeanRows"/> 回傳。
    /// </summary>
    public sealed class SiteMeanRow
    {
        public decimal MeanValue { get; set; }
        public DateTime? StartTime { get; set; }
    }
}

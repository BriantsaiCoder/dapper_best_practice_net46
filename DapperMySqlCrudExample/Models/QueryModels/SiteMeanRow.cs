using System;

namespace DapperMySqlCrudExample.Models.QueryModels
{
    /// <summary>
    /// 用於 SITE_MEAN 規格計算的歷史統計資料列。
    /// 僅包含計算所需的兩個欄位，由 <see cref="Repositories.SiteTestStatisticRepository.QuerySiteMeanRows"/> 回傳。
    /// </summary>
    public sealed class SiteMeanRow
    {
        /// <summary>該筆統計記錄的平均值。</summary>
        public decimal MeanValue { get; set; }

        /// <summary>該筆統計記錄的起始時間。SQL 查詢已篩選 IS NOT NULL，保證有值。</summary>
        public DateTime StartTime { get; set; }
    }
}

namespace DapperMySqlCrudExample.Models.QueryModels
{
    /// <summary>
    /// SITE_MEAN 規格計算所需的三個引數。
    /// 由 <see cref="Repositories.SiteTestStatisticRepository.GetCalcParamsFromLatestSample"/> 回傳。
    /// </summary>
    public sealed class SiteMeanCalcParams
    {
        /// <summary>程式名稱（對應 site_test_statistics.program）。</summary>
        public string ProgramName { get; set; }

        /// <summary>Site 編號（對應 site_test_statistics.site_id）。</summary>
        public uint SiteId { get; set; }

        /// <summary>測項名稱（對應 site_test_statistics.test_item_name）。</summary>
        public string TestItemName { get; set; }
    }
}

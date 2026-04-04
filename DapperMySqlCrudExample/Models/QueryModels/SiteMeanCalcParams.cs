namespace DapperMySqlCrudExample.Models.QueryModels
{
    /// <summary>
    /// SITE_MEAN 規格計算所需的三個引數。
    /// 由 <see cref="Repositories.SiteTestStatisticRepository.GetCalcParamsFromLatestSample"/> 回傳。
    /// </summary>
    public sealed class SiteMeanCalcParams
    {
        public string ProgramName { get; set; }
        public uint SiteId { get; set; }
        public string TestItemName { get; set; }
    }
}

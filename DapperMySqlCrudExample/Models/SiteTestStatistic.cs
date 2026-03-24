using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>Site 測項統計值表</summary>
    public class SiteTestStatistic
    {
        public long Id { get; set; }
        public int LotsInfoId { get; set; }
        public string Program { get; set; }
        public uint SiteId { get; set; }
        public string TestItemName { get; set; }
        public decimal? MeanValue { get; set; }
        public decimal? MaxValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? StdValue { get; set; }
        public decimal? CpValue { get; set; }
        public decimal? CpkValue { get; set; }
        public string TesterId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

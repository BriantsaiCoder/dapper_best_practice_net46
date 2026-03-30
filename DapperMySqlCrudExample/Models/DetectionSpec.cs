using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>Spec 規格表</summary>
    public class DetectionSpec
    {
        public long Id { get; set; }
        public string Program { get; set; }
        public string TestItemName { get; set; }
        public uint SiteId { get; set; }
        public byte DetectionMethodId { get; set; }
        public decimal? SpecUpperLimit { get; set; }
        public decimal? SpecLowerLimit { get; set; }
        public DateTime SpecCalcStartTime { get; set; }
        public DateTime SpecCalcEndTime { get; set; }
        public decimal? SpecCalcMean { get; set; }
        public decimal? SpecCalcStd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>異常 Unit 明細表</summary>
    public class AnomalyUnit
    {
        public long Id { get; set; }
        public long AnomalyTestItemId { get; set; }
        public string UnitId { get; set; }
        public decimal? DetectionValue { get; set; }
        public decimal? SpecUpperLimit { get; set; }
        public decimal? SpecLowerLimit { get; set; }
        public DateTime? SpecCalcStartTime { get; set; }
        public DateTime? SpecCalcEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

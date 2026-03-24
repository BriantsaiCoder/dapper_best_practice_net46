using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>異常測項明細表</summary>
    public class AnomalyTestItem
    {
        public long Id { get; set; }
        public long AnomalyLotId { get; set; }
        public string TestItemName { get; set; }
        public decimal? DetectionValue { get; set; }
        public decimal? SpecUpperLimit { get; set; }
        public decimal? SpecLowerLimit { get; set; }
        public DateTime? SpecCalcStartTime { get; set; }
        public DateTime? SpecCalcEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

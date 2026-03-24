using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>異常批號主表</summary>
    public class AnomalyLot
    {
        public long Id { get; set; }
        public int LotsInfoId { get; set; }
        public byte DetectionMethodId { get; set; }
        public decimal? SpecUpperLimit { get; set; }
        public decimal? SpecLowerLimit { get; set; }
        public DateTime? SpecCalcStartTime { get; set; }
        public DateTime? SpecCalcEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

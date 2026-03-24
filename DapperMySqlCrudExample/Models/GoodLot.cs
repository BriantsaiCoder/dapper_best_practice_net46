using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>好批批號記錄表（數值偏差偵測模組）</summary>
    public class GoodLot
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

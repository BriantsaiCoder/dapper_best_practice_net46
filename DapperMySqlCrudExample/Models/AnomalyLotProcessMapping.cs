using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>批號 Process Mapping（站點 &amp; 機台）</summary>
    public class AnomalyLotProcessMapping
    {
        public long Id { get; set; }
        public long AnomalyLotId { get; set; }
        public string StationName { get; set; }
        public string EquipmentId { get; set; }
        public DateTime? ProcessTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

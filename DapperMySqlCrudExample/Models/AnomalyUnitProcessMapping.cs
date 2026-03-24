using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>Unit Process Mapping（Boat ID &amp; XY 座標）</summary>
    public class AnomalyUnitProcessMapping
    {
        public long Id { get; set; }
        public long AnomalyUnitId { get; set; }
        public string BoatId { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public DateTime? ProcessTime { get; set; }
        public string StationName { get; set; }
        public string EquipmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

using System;

namespace DapperMySqlCrudExample.Models
{
    /// <summary>偵測方法主表</summary>
    public class DetectionMethod
    {
        public byte Id { get; set; }
        public string MethodCode { get; set; }
        public string MethodName { get; set; }
        public bool HasTestItem { get; set; }
        public bool HasUnitLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

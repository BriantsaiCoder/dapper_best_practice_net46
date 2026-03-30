using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Spec 規格 Repository 介面</summary>
    public interface IDetectionSpecRepository
    {
        IEnumerable<DetectionSpec> GetAll();
        DetectionSpec GetById(long id);
        IEnumerable<DetectionSpec> GetByProgramAndMethod(string program, byte detectionMethodId);

        /// <summary>
        /// 依 detection method name 及 program 查詢最近一個月內計算的 spec 資料。
        /// </summary>
        /// <param name="program">產品程式名稱</param>
        /// <param name="detectionMethodName">偵測方法名稱（對應 detection_methods.method_name）</param>
        /// <returns>符合條件的 DetectionSpec 集合</returns>
        IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(string program, string detectionMethodName);

        /// <summary>
        /// 取最近一個月內 spec_calc_end_time 最大的單筆記錄（最新有效規格）。
        /// 找不到時回傳 null。
        /// </summary>
        /// <param name="program">產品程式名稱</param>
        /// <param name="detectionMethodName">偵測方法名稱（對應 detection_methods.method_name）</param>
        /// <returns>最新一筆 DetectionSpec，或 null</returns>
        DetectionSpec GetLatestByProgramAndMethodName(string program, string detectionMethodName);

        long Insert(DetectionSpec entity);
        bool Update(DetectionSpec entity);
        bool Delete(long id);
    }
}

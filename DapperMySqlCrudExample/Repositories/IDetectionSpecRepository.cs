using System.Collections.Generic;
using System.Data;
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
        IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(
            string program,
            string detectionMethodName
        );

        /// <summary>
        /// 取最近一個月內 spec_calc_end_time 最大的單筆記錄（最新有效規格）。
        /// 找不到時回傳 null。
        /// </summary>
        /// <param name="program">產品程式名稱</param>
        /// <param name="detectionMethodName">偵測方法名稱（對應 detection_methods.method_name）</param>
        /// <returns>最新一筆 DetectionSpec，或 null</returns>
        DetectionSpec GetLatestByProgramAndMethodName(string program, string detectionMethodName);

        long Insert(DetectionSpec entity, IDbTransaction transaction = null);
        bool Update(DetectionSpec entity, IDbTransaction transaction = null);
        bool Delete(long id, IDbTransaction transaction = null);

        /// <summary>
        /// 查詢 site_test_statistics 中指定 program / site / test_item 的 mean_value，
        /// 優先取「近一個月內且 mean_value 不為 NULL」的資料（需 ≥ 30 筆）；
        /// 若不足，改取最新 30 筆（mean_value 不為 NULL）。
        /// 以算術平均數 ± 6 × 樣本標準差作為 spec 上下限，
        /// 寫入 detection_specs（detection_method = SITE_MEAN），回傳新記錄的 ID。
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// 當有效資料筆數為 0，或所有 start_time 皆為 NULL 時拋出。
        /// </exception>
        long ComputeAndInsertSiteMeanSpec(string programName, uint siteId, string testItemName);
    }
}

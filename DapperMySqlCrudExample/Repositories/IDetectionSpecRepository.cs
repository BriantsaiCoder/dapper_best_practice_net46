using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 偵測規格（detection_specs）資料表的 Repository 介面。
    /// 提供標準 CRUD、分頁、計數、存在判斷、依偵測方法查詢的業務查詢方法，
    /// 以及 SITE_MEAN 規格計算。
    /// </summary>
    public interface IDetectionSpecRepository
    {
        /// <summary>取得所有偵測規格記錄。</summary>
        /// <returns>DetectionSpec 集合（可能為空）。</returns>
        IEnumerable<DetectionSpec> GetAll();

        /// <summary>依主鍵查詢單筆偵測規格。</summary>
        /// <param name="id">記錄主鍵 ID。</param>
        /// <returns>找到時回傳 <see cref="DetectionSpec"/>，否則回傳 null。</returns>
        DetectionSpec GetById(long id);

        /// <summary>依 program 與 detection_method_id 查詢符合條件的規格清單。</summary>
        /// <param name="program">產品程式名稱。</param>
        /// <param name="detectionMethodId">偵測方法主鍵（對應 detection_methods.id）。</param>
        /// <returns>符合條件的 DetectionSpec 集合。</returns>
        IEnumerable<DetectionSpec> GetByProgramAndMethod(string program, byte detectionMethodId);

        /// <summary>
        /// 依偵測方法名稱及 program 查詢最近一個月內計算的規格清單。
        /// </summary>
        /// <param name="program">產品程式名稱。</param>
        /// <param name="detectionMethodName">偵測方法名稱（對應 detection_methods.method_name）。</param>
        /// <returns>符合條件的 DetectionSpec 集合（可能為空）。</returns>
        IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(
            string program,
            string detectionMethodName
        );

        /// <summary>
        /// 取最近一個月內 spec_calc_end_time 最大的單筆規格（最新有效規格）。
        /// </summary>
        /// <param name="program">產品程式名稱。</param>
        /// <param name="detectionMethodName">偵測方法名稱（對應 detection_methods.method_name）。</param>
        /// <returns>最新一筆 <see cref="DetectionSpec"/>，找不到時回傳 null。</returns>
        DetectionSpec GetLatestByProgramAndMethodName(string program, string detectionMethodName);

        /// <summary>新增一筆偵測規格，回傳新記錄的自動遞增 ID。</summary>
        /// <param name="entity">要新增的 DetectionSpec 實體。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>新記錄的自動遞增主鍵 ID。</returns>
        long Insert(DetectionSpec entity, IDbTransaction transaction = null);

        /// <summary>更新一筆既有的偵測規格。</summary>
        /// <param name="entity">包含更新資料的 DetectionSpec 實體（Id 欄位必填）。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Update(DetectionSpec entity, IDbTransaction transaction = null);

        /// <summary>依主鍵刪除一筆偵測規格。</summary>
        /// <param name="id">要刪除記錄的主鍵 ID。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Delete(long id, IDbTransaction transaction = null);

        /// <summary>判斷指定主鍵的偵測規格是否存在。</summary>
        /// <param name="id">記錄主鍵 ID。</param>
        /// <returns>記錄存在則回傳 true，否則回傳 false。</returns>
        bool Exists(long id);

        /// <summary>取得 detection_specs 資料表的總記錄數。</summary>
        /// <returns>記錄總數。</returns>
        int GetCount();

        /// <summary>依偏移量與筆數分頁取得偵測規格清單（依 id 升冪排序）。</summary>
        /// <param name="offset">略過的記錄數（從 0 開始）。</param>
        /// <param name="limit">最多回傳的記錄筆數。</param>
        /// <returns>該分頁的 DetectionSpec 集合。</returns>
        IEnumerable<DetectionSpec> GetPaged(int offset, int limit);

        /// <summary>
        /// 依據歷史 site_test_statistics 資料計算 Mean ± 6σ，
        /// 建立新的 detection_specs 記錄（使用 SITE_MEAN 偵測方法）。
        /// </summary>
        /// <param name="programName">測試程式名稱。</param>
        /// <param name="siteId">Site 編號。</param>
        /// <param name="testItemName">測項名稱。</param>
        /// <returns>新建 detection_specs 記錄的主鍵 id。</returns>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="programName"/> 或 <paramref name="testItemName"/> 為 null、空字串或空白字元。
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// 查無足夠的 site_test_statistics 資料、所有 start_time 均為 NULL，
        /// 或 detection_methods 中未設定 method_code = 'SITE_MEAN'。
        /// </exception>
        long ComputeAndInsertSiteMeanSpec(string programName, uint siteId, string testItemName);
    }
}

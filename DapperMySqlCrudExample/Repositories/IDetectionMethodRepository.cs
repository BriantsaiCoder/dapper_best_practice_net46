using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 偵測方法（detection_methods）資料表的 Repository 介面。
    /// 注意：detection_methods.id 為 TINYINT UNSIGNED，因此 PK 型別為 <see cref="byte"/>。
    /// </summary>
    public interface IDetectionMethodRepository
    {
        /// <summary>取得所有偵測方法記錄。</summary>
        /// <returns>DetectionMethod 集合（可能為空）。</returns>
        IEnumerable<DetectionMethod> GetAll();

        /// <summary>依主鍵查詢單筆偵測方法。</summary>
        /// <param name="id">記錄主鍵 ID（TINYINT UNSIGNED）。</param>
        /// <returns>找到時回傳 <see cref="DetectionMethod"/>，否則回傳 null。</returns>
        DetectionMethod GetById(byte id);

        /// <summary>依方法代碼查詢單筆偵測方法。</summary>
        /// <param name="methodCode">唯一的方法代碼（如 "SITE_MEAN"）。</param>
        /// <returns>找到時回傳 <see cref="DetectionMethod"/>，否則回傳 null。</returns>
        DetectionMethod GetByCode(string methodCode);

        /// <summary>新增一筆偵測方法，回傳新記錄的主鍵 ID。</summary>
        /// <param name="entity">要新增的 DetectionMethod 實體。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>新記錄的主鍵 ID（TINYINT UNSIGNED）。</returns>
        byte Insert(DetectionMethod entity, IDbTransaction transaction = null);

        /// <summary>更新一筆既有的偵測方法。</summary>
        /// <param name="entity">包含更新資料的 DetectionMethod 實體（Id 欄位必填）。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Update(DetectionMethod entity, IDbTransaction transaction = null);

        /// <summary>依主鍵刪除一筆偵測方法。</summary>
        /// <param name="id">要刪除記錄的主鍵 ID（TINYINT UNSIGNED）。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Delete(byte id, IDbTransaction transaction = null);

        /// <summary>判斷指定主鍵的偵測方法是否存在。</summary>
        /// <param name="id">記錄主鍵 ID（TINYINT UNSIGNED）。</param>
        /// <returns>記錄存在則回傳 true，否則回傳 false。</returns>
        bool Exists(byte id);

        /// <summary>取得 detection_methods 資料表的總記錄數。</summary>
        /// <returns>記錄總數。</returns>
        int GetCount();

        /// <summary>依偏移量與筆數分頁取得偵測方法清單（依 id 升冪排序）。</summary>
        /// <param name="offset">略過的記錄數（從 0 開始）。</param>
        /// <param name="limit">最多回傳的記錄筆數。</param>
        /// <returns>該分頁的 DetectionMethod 集合。</returns>
        IEnumerable<DetectionMethod> GetPaged(int offset, int limit);
    }
}

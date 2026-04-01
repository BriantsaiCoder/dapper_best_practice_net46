using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 異常單元製程對應（anomaly_unit_process_mapping）資料表的 Repository 介面。
    /// </summary>
    public interface IAnomalyUnitProcessMappingRepository
    {
        /// <summary>取得所有異常單元製程對應記錄。</summary>
        /// <returns>AnomalyUnitProcessMapping 集合（可能為空）。</returns>
        IEnumerable<AnomalyUnitProcessMapping> GetAll();

        /// <summary>依主鍵查詢單筆異常單元製程對應。</summary>
        /// <param name="id">記錄主鍵 ID。</param>
        /// <returns>找到時回傳 <see cref="AnomalyUnitProcessMapping"/>，否則回傳 null。</returns>
        AnomalyUnitProcessMapping GetById(long id);

        /// <summary>依異常單元 ID 查詢製程對應清單。</summary>
        /// <param name="anomalyUnitId">外鍵，對應 anomaly_units.id。</param>
        /// <returns>符合條件的 AnomalyUnitProcessMapping 集合（可能為空）。</returns>
        IEnumerable<AnomalyUnitProcessMapping> GetByAnomalyUnitId(long anomalyUnitId);

        /// <summary>新增一筆異常單元製程對應，回傳新記錄的主鍵 ID。</summary>
        /// <param name="entity">要新增的 AnomalyUnitProcessMapping 實體。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>新記錄的主鍵 ID（LAST_INSERT_ID()）。</returns>
        long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null);

        /// <summary>更新一筆既有的異常單元製程對應。</summary>
        /// <param name="entity">包含更新資料的 AnomalyUnitProcessMapping 實體（Id 欄位必填）。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Update(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null);

        /// <summary>依主鍵刪除一筆異常單元製程對應。</summary>
        /// <param name="id">要刪除記錄的主鍵 ID。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Delete(long id, IDbTransaction transaction = null);

        /// <summary>判斷指定主鍵的異常單元製程對應是否存在。</summary>
        /// <param name="id">記錄主鍵 ID。</param>
        /// <returns>記錄存在則回傳 true，否則回傳 false。</returns>
        bool Exists(long id);

        /// <summary>取得 anomaly_unit_process_mapping 資料表的總記錄數。</summary>
        /// <returns>記錄總數。</returns>
        int GetCount();

        /// <summary>依偏移量與筆數分頁取得異常單元製程對應清單（依 id 升冪排序）。</summary>
        /// <param name="offset">略過的記錄數（從 0 開始）。</param>
        /// <param name="limit">最多回傳的記錄筆數。</param>
        /// <returns>該分頁的 AnomalyUnitProcessMapping 集合。</returns>
        IEnumerable<AnomalyUnitProcessMapping> GetPaged(int offset, int limit);
    }
}

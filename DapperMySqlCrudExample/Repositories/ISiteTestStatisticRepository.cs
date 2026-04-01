using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 站點測試統計（site_test_statistics）資料表的 Repository 介面。
    /// </summary>
    public interface ISiteTestStatisticRepository
    {
        /// <summary>取得所有站點測試統計記錄。</summary>
        /// <returns>SiteTestStatistic 集合（可能為空）。</returns>
        IEnumerable<SiteTestStatistic> GetAll();

        /// <summary>依主鍵查詢單筆站點測試統計。</summary>
        /// <param name="id">記錄主鍵 ID。</param>
        /// <returns>找到時回傳 <see cref="SiteTestStatistic"/>，否則回傳 null。</returns>
        SiteTestStatistic GetById(long id);

        /// <summary>依批次資訊 ID 查詢站點測試統計清單。</summary>
        /// <param name="lotsInfoId">外鍵，對應 lots_info.id。</param>
        /// <returns>符合條件的 SiteTestStatistic 集合（可能為空）。</returns>
        IEnumerable<SiteTestStatistic> GetByLotsInfoId(int lotsInfoId);

        /// <summary>依站點 ID 與測試項目名稱查詢統計清單。</summary>
        /// <param name="siteId">站點 ID（UNSIGNED INT）。</param>
        /// <param name="testItemName">測試項目名稱。</param>
        /// <returns>符合條件的 SiteTestStatistic 集合（可能為空）。</returns>
        IEnumerable<SiteTestStatistic> GetBySiteAndItem(uint siteId, string testItemName);

        /// <summary>新增一筆站點測試統計，回傳新記錄的主鍵 ID。</summary>
        /// <param name="entity">要新增的 SiteTestStatistic 實體。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>新記錄的主鍵 ID（LAST_INSERT_ID()）。</returns>
        long Insert(SiteTestStatistic entity, IDbTransaction transaction = null);

        /// <summary>更新一筆既有的站點測試統計。</summary>
        /// <param name="entity">包含更新資料的 SiteTestStatistic 實體（Id 欄位必填）。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Update(SiteTestStatistic entity, IDbTransaction transaction = null);

        /// <summary>依主鍵刪除一筆站點測試統計。</summary>
        /// <param name="id">要刪除記錄的主鍵 ID。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則回傳 true，否則回傳 false。</returns>
        bool Delete(long id, IDbTransaction transaction = null);

        /// <summary>判斷指定主鍵的站點測試統計是否存在。</summary>
        /// <param name="id">記錄主鍵 ID。</param>
        /// <returns>記錄存在則回傳 true，否則回傳 false。</returns>
        bool Exists(long id);

        /// <summary>取得 site_test_statistics 資料表的總記錄數。</summary>
        /// <returns>記錄總數。</returns>
        int GetCount();

        /// <summary>依偏移量與筆數分頁取得站點測試統計清單（依 id 升冪排序）。</summary>
        /// <param name="offset">略過的記錄數（從 0 開始）。</param>
        /// <param name="limit">最多回傳的記錄筆數。</param>
        /// <returns>該分頁的 SiteTestStatistic 集合。</returns>
        IEnumerable<SiteTestStatistic> GetPaged(int offset, int limit);
    }
}

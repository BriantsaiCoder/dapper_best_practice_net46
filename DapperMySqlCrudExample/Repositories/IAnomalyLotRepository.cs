using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 異常批號 Repository 介面，定義 anomaly_lots 資料表的 CRUD 操作。
    /// </summary>
    public interface IAnomalyLotRepository
    {
        /// <summary>取得所有異常批號記錄。</summary>
        /// <returns>所有 <see cref="AnomalyLot"/> 的集合。</returns>
        IEnumerable<AnomalyLot> GetAll();

        /// <summary>依主鍵取得單筆異常批號。</summary>
        /// <param name="id">主鍵 ID。</param>
        /// <returns>符合的 <see cref="AnomalyLot"/>；找不到時回傳 <c>null</c>。</returns>
        AnomalyLot GetById(long id);

        /// <summary>依批號資訊 ID 取得異常批號清單。</summary>
        /// <param name="lotsInfoId">關聯的 lots_info ID。</param>
        /// <returns>符合的 <see cref="AnomalyLot"/> 集合。</returns>
        IEnumerable<AnomalyLot> GetByLotsInfoId(int lotsInfoId);

        /// <summary>新增一筆異常批號，支援交易。</summary>
        /// <param name="entity">要新增的 <see cref="AnomalyLot"/> 資料。</param>
        /// <param name="transaction">可選的資料庫交易；傳入 <c>null</c> 時自動建立連線。</param>
        /// <returns>新插入記錄的自動遞增主鍵 ID。</returns>
        long Insert(AnomalyLot entity, IDbTransaction transaction = null);

        /// <summary>更新一筆異常批號，支援交易。</summary>
        /// <param name="entity">包含更新資料的 <see cref="AnomalyLot"/>（需含 Id）。</param>
        /// <param name="transaction">可選的資料庫交易；傳入 <c>null</c> 時自動建立連線。</param>
        /// <returns>更新成功為 <c>true</c>；影響行數為 0 時為 <c>false</c>。</returns>
        bool Update(AnomalyLot entity, IDbTransaction transaction = null);

        /// <summary>刪除一筆異常批號，支援交易。</summary>
        /// <param name="id">要刪除的主鍵 ID。</param>
        /// <param name="transaction">可選的資料庫交易；傳入 <c>null</c> 時自動建立連線。</param>
        /// <returns>刪除成功為 <c>true</c>；找不到記錄時為 <c>false</c>。</returns>
        bool Delete(long id, IDbTransaction transaction = null);

        /// <summary>確認指定 ID 的異常批號是否存在。</summary>
        /// <param name="id">主鍵 ID。</param>
        /// <returns>存在為 <c>true</c>，否則為 <c>false</c>。</returns>
        bool Exists(long id);

        /// <summary>取得異常批號的總筆數。</summary>
        /// <returns>資料表中的記錄總數。</returns>
        int GetCount();

        /// <summary>依分頁參數取得異常批號清單。</summary>
        /// <param name="offset">起始偏移量（從 0 開始）。</param>
        /// <param name="limit">最多回傳筆數。</param>
        /// <returns>指定分頁範圍的 <see cref="AnomalyLot"/> 集合。</returns>
        IEnumerable<AnomalyLot> GetPaged(int offset, int limit);
    }
}

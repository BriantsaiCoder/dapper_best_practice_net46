namespace DapperMySqlCrudExample.Services
{
    /// <summary>Spec 規格計算服務介面。</summary>
    public interface IDetectionSpecService
    {
        /// <summary>
        /// 依據 <paramref name="programName"/>、<paramref name="siteId"/> 及
        /// <paramref name="testItemName"/> 查詢最近歷史統計資料，
        /// 計算 Mean ± 6σ 後將新規格寫入 detection_specs 資料表。
        /// </summary>
        /// <param name="programName">測試程式名稱。</param>
        /// <param name="siteId">Site 編號。</param>
        /// <param name="testItemName">測項名稱。</param>
        /// <returns>新建 detection_specs 記錄的主鍵 id。</returns>
        /// <exception cref="System.InvalidOperationException">
        /// 查無足夠的 site_test_statistics 資料，或所有 start_time 均為 NULL。
        /// </exception>
        long ComputeAndInsertSiteMeanSpec(string programName, uint siteId, string testItemName);
    }
}

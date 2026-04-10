using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Models.QueryModels;
using DapperMySqlCrudExample.Repositories;
using MathNet.Numerics.Statistics;

namespace DapperMySqlCrudExample.Services
{
    /// <summary>
    /// DetectionSpec 業務邏輯服務。
    /// 負責 SITE_MEAN 規格的統計計算與寫入編排，
    /// Repository 僅負責 SQL CRUD，計算邏輯集中於此。
    /// </summary>
    /// <remarks>
    /// 【新手導讀】Repository vs Service 的職責分工：
    /// - Repository：純粹的資料存取層，只負責單一資料表的 CRUD，不包含業務邏輯。
    /// - Service：業務邏輯層，負責編排（orchestrate）多個 Repository 的操作、管理交易、執行計算。
    /// 例如本類別需要同時操作 SiteTestStatisticRepo、DetectionMethodRepo、DetectionSpecRepo，
    /// 並在同一交易中完成「查詢→計算→寫入」的完整流程，這種跨 Repository 的協作就是 Service 的職責。
    /// </remarks>
    public sealed class DetectionSpecService
    {
        private readonly DbConnectionFactory _factory;
        private readonly DetectionSpecRepository _detectionSpecRepo;
        private readonly SiteTestStatisticRepository _siteTestStatRepo;
        private readonly DetectionMethodRepository _detectionMethodRepo;

        private const string SiteMeanMethodKey = "SITE_MEAN";

        /// <summary>SITE_MEAN 計算所需的最小樣本數。僅 1 筆時 std=0，UCL=LCL=mean 會造成誤判。</summary>
        private const int MinimumSampleCount = 2;

        public DetectionSpecService(
            DbConnectionFactory factory,
            DetectionSpecRepository detectionSpecRepo,
            SiteTestStatisticRepository siteTestStatRepo,
            DetectionMethodRepository detectionMethodRepo
        )
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _detectionSpecRepo =
                detectionSpecRepo ?? throw new ArgumentNullException(nameof(detectionSpecRepo));
            _siteTestStatRepo =
                siteTestStatRepo ?? throw new ArgumentNullException(nameof(siteTestStatRepo));
            _detectionMethodRepo =
                detectionMethodRepo ?? throw new ArgumentNullException(nameof(detectionMethodRepo));
        }

        /// <summary>
        /// 依歷史 site_test_statistics 資料計算 SITE_MEAN 規格並插入 detection_specs。
        /// 取樣策略為取最新 30 筆有效資料進行統計。
        /// 使用 RepeatableRead 隔離層級確保計算期間讀取一致性。
        /// </summary>
        public long ComputeAndInsertSiteMeanSpec(
            string programName,
            uint siteId,
            string testItemName
        )
        {
            if (string.IsNullOrWhiteSpace(programName))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(programName));
            if (string.IsNullOrWhiteSpace(testItemName))
                throw new ArgumentException(
                    "參數不可為 null、空字串或空白。",
                    nameof(testItemName)
                );

            // 【新手導讀】雙層 using 管理連線與交易的生命週期：
            // 外層 using 管理連線（conn），內層 using 管理交易（tx）。
            // 離開區塊時會依反序 Dispose：先 tx（自動 Rollback 未 Commit 的交易），再 conn（歸還連線池）。
            using (var conn = _factory.Create())
            {
                // 【新手導讀】BeginTransaction() 要求連線已開啟，因此交易場景需手動 Open()。
                // 一般不需交易的 Repository 方法由 Dapper 自動管理開關連線，不須手動 Open()。
                conn.Open();
                // 【新手導讀】IsolationLevel.RepeatableRead 確保在交易期間，已讀取的資料不會被其他交易修改。
                // 這對統計計算很重要：避免「查詢歷史資料」與「寫入計算結果」之間資料被外部異動導致不一致。
                using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
                {
                    var rows = _siteTestStatRepo.QuerySiteMeanRows(
                        programName,
                        siteId,
                        testItemName,
                        tx
                    );

                    if (rows.Count < MinimumSampleCount)
                        throw new InvalidOperationException(
                            $"site_test_statistics 中符合條件的資料筆數不足（需要 {MinimumSampleCount} 筆，實際 {rows.Count} 筆；"
                                + $"program={programName}, siteId={siteId}, testItem={testItemName}）。"
                        );

                    var (mean, std) = CalculateMeanAndStd(rows);
                    var (ucl, lcl) = CalculateControlLimits(mean, std);
                    var (specCalcStart, specCalcEnd) = ExtractTimeRange(rows);

                    byte methodId = GetRequiredSiteMeanMethodId(tx);

                    var spec = BuildDetectionSpec(
                        programName,
                        siteId,
                        testItemName,
                        methodId,
                        ucl,
                        lcl,
                        specCalcStart,
                        specCalcEnd,
                        mean,
                        std
                    );

                    long newId = _detectionSpecRepo.Insert(spec, tx);
                    // 【新手導讀】必須明確呼叫 Commit() 才會真正寫入資料庫。
                    // 若在 Commit() 之前發生例外，using 區塊結束時 tx.Dispose() 會自動 Rollback，
                    // 所有在此交易中的操作都會被撤銷，確保資料一致性（全成功或全失敗）。
                    tx.Commit();
                    return newId;
                }
            }
        }

        // ── 私有輔助方法 ────────────────────────────────────────────

        /// <summary>
        /// 計算樣本平均值與標準差。
        /// 呼叫前須確保 rows.Count &gt;= <see cref="MinimumSampleCount"/>。
        /// </summary>
        private static (double mean, double std) CalculateMeanAndStd(
            IReadOnlyList<SiteMeanRow> rows
        )
        {
            // MathNet.Numerics Statistics API 接受 double；
            // DECIMAL(18,9) 最多 18 位有效數字，double 可精確表達 15-16 位，
            // 在本專案的量測數值範圍內不會造成精度遺失。
            var values = rows.Select(r => (double)r.MeanValue).ToList();
            return (Statistics.Mean(values), Statistics.StandardDeviation(values));
        }

        /// <summary>
        /// 計算管制上下限（UCL/LCL）。使用 ±6σ 規則。
        /// double → decimal 轉換同 <see cref="CalculateMeanAndStd"/> 的精度說明。
        /// </summary>
        private static (decimal ucl, decimal lcl) CalculateControlLimits(double mean, double std)
        {
            var ucl = (decimal)(mean + 6.0 * std);
            var lcl = (decimal)(mean - 6.0 * std);
            return (ucl, lcl);
        }

        /// <summary>
        /// 從歷史資料中提取時間範圍（計算起迄時間）。
        /// QuerySiteMeanRows 已篩選 start_time IS NOT NULL，此處直接取 Min/Max。
        /// </summary>
        private static (DateTime start, DateTime end) ExtractTimeRange(
            IReadOnlyList<SiteMeanRow> rows
        )
        {
            var times = rows.Select(r => r.StartTime).ToList();
            return (times.Min(), times.Max());
        }

        private byte GetRequiredSiteMeanMethodId(IDbTransaction tx)
        {
            var methodId = _detectionMethodRepo.GetIdByKey(SiteMeanMethodKey, tx);
            if (!methodId.HasValue)
                throw new InvalidOperationException(
                    "detection_methods 中找不到 method_key = 'SITE_MEAN' 的設定，無法建立 DetectionSpec。"
                );

            return methodId.Value;
        }

        /// <summary>
        /// 建立 DetectionSpec 實體。
        /// </summary>
        private static DetectionSpec BuildDetectionSpec(
            string programName,
            uint siteId,
            string testItemName,
            byte methodId,
            decimal ucl,
            decimal lcl,
            DateTime specCalcStart,
            DateTime specCalcEnd,
            double mean,
            double std
        )
        {
            return new DetectionSpec
            {
                Program = programName,
                TestItemName = testItemName,
                SiteId = siteId,
                DetectionMethodId = methodId,
                SpecUpperLimit = ucl,
                SpecLowerLimit = lcl,
                SpecCalcStartTime = specCalcStart,
                SpecCalcEndTime = specCalcEnd,
                SpecCalcMean = (decimal)mean,
                SpecCalcStd = (decimal)std,
            };
        }
    }
}

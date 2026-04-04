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

            using (var conn = _factory.Create())
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
                tx.Commit();
                return newId;
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
            var values = rows.Select(r => (double)r.MeanValue).ToList();
            return (Statistics.Mean(values), Statistics.StandardDeviation(values));
        }

        /// <summary>
        /// 計算管制上下限（UCL/LCL）。使用 ±6σ 規則。
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

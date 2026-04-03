using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;
using MathNet.Numerics.Statistics;

namespace DapperMySqlCrudExample.Calculators
{
    /// <summary>
    /// SITE_MEAN 規格計算器。
    /// <para>
    /// 依歷史 site_test_statistics 資料計算平均值與標準差，
    /// 產生 ±6σ 管制限並寫入 detection_specs。
    /// 使用 RepeatableRead 隔離層級確保「先讀後寫」的一致性。
    /// </para>
    /// </summary>
    public sealed class SiteMeanSpecCalculator
    {
        private readonly DbConnectionFactory _factory;
        private readonly SiteTestStatisticRepository _siteTestStatisticRepository;
        private readonly DetectionMethodRepository _detectionMethodRepository;
        private readonly DetectionSpecRepository _detectionSpecRepository;

        private const string SiteMeanMethodCode = "SITE_MEAN";
        private const int PreferredHistoryCount = 30;

        /// <summary>建立 <see cref="SiteMeanSpecCalculator"/> 實例。</summary>
        public SiteMeanSpecCalculator(
            DbConnectionFactory factory,
            SiteTestStatisticRepository siteTestStatisticRepository,
            DetectionMethodRepository detectionMethodRepository,
            DetectionSpecRepository detectionSpecRepository
        )
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _siteTestStatisticRepository = siteTestStatisticRepository
                ?? throw new ArgumentNullException(nameof(siteTestStatisticRepository));
            _detectionMethodRepository = detectionMethodRepository
                ?? throw new ArgumentNullException(nameof(detectionMethodRepository));
            _detectionSpecRepository = detectionSpecRepository
                ?? throw new ArgumentNullException(nameof(detectionSpecRepository));
        }

        /// <summary>
        /// 計算 SITE_MEAN 規格並插入 detection_specs，回傳新建記錄的主鍵 Id。
        /// </summary>
        /// <param name="programName">測試程式代碼。</param>
        /// <param name="siteId">Site 編號。</param>
        /// <param name="testItemName">測試項目名稱。</param>
        /// <returns>新建 detection_specs 記錄的 Id。</returns>
        /// <exception cref="ArgumentException">參數為 null、空字串或空白。</exception>
        /// <exception cref="InvalidOperationException">無歷史資料或缺少 SITE_MEAN 方法設定。</exception>
        public long Execute(string programName, uint siteId, string testItemName)
        {
            if (string.IsNullOrWhiteSpace(programName))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(programName));
            if (string.IsNullOrWhiteSpace(testItemName))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(testItemName));

            using (var conn = _factory.Create())
            using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
            {
                try
                {
                    // 1. 讀取歷史統計資料
                    var rows = _siteTestStatisticRepository.GetMeanHistory(
                        programName, siteId, testItemName, PreferredHistoryCount, tx
                    );

                    if (rows.Count == 0)
                        throw new InvalidOperationException(
                            $"No site_test_statistics data for program={programName}, "
                                + $"siteId={siteId}, testItem={testItemName}."
                        );

                    // 2. 查詢 SITE_MEAN 偵測方法 Id
                    byte? methodId = _detectionMethodRepository.GetIdByCode(SiteMeanMethodCode, tx);
                    if (!methodId.HasValue)
                        throw new InvalidOperationException(
                            "detection_methods 中找不到 method_code = 'SITE_MEAN' 的設定，無法建立 DetectionSpec。"
                        );

                    // 3. 統計計算
                    var (mean, std) = CalculateMeanAndStd(rows);
                    var (ucl, lcl) = CalculateControlLimits(mean, std);
                    var (specCalcStart, specCalcEnd) = ExtractTimeRange(rows);

                    // 4. 組裝並寫入
                    var spec = new DetectionSpec
                    {
                        Program = programName,
                        TestItemName = testItemName,
                        SiteId = siteId,
                        DetectionMethodId = methodId.Value,
                        SpecUpperLimit = ucl,
                        SpecLowerLimit = lcl,
                        SpecCalcStartTime = specCalcStart,
                        SpecCalcEndTime = specCalcEnd,
                        SpecCalcMean = (decimal)mean,
                        SpecCalcStd = (decimal)std,
                    };

                    long newId = _detectionSpecRepository.Insert(spec, tx);
                    tx.Commit();
                    return newId;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // ── 私有計算輔助方法 ─────────────────────────────────────────────────

        /// <summary>計算樣本平均值與標準差。</summary>
        private static (double mean, double std) CalculateMeanAndStd(
            IReadOnlyList<SiteMeanHistoryRow> rows
        )
        {
            if (rows.Count >= 2)
            {
                var values = rows.Select(r => (double)r.MeanValue).ToList();
                return (Statistics.Mean(values), Statistics.StandardDeviation(values));
            }

            return ((double)rows[0].MeanValue, 0.0);
        }

        /// <summary>計算管制上下限（UCL/LCL）。使用 ±6σ 規則。</summary>
        private static (decimal ucl, decimal lcl) CalculateControlLimits(double mean, double std)
        {
            var ucl = (decimal)(mean + 6.0 * std);
            var lcl = (decimal)(mean - 6.0 * std);
            return (ucl, lcl);
        }

        /// <summary>從歷史資料中提取時間範圍（計算起迄時間）。</summary>
        private static (DateTime start, DateTime end) ExtractTimeRange(
            IReadOnlyList<SiteMeanHistoryRow> rows
        )
        {
            var timesWithValue = rows.Where(r => r.StartTime.HasValue)
                .Select(r => r.StartTime.Value)
                .ToList();

            if (!timesWithValue.Any())
                throw new InvalidOperationException(
                    "All start_time values are NULL in site_test_statistics; "
                        + "cannot determine SpecCalcStartTime / SpecCalcEndTime."
                );

            return (timesWithValue.Min(), timesWithValue.Max());
        }
    }
}

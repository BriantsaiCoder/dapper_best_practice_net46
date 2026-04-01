using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;
using MathNet.Numerics.Statistics;

namespace DapperMySqlCrudExample.Services
{
    /// <summary>
    /// Spec 規格計算服務實作。
    /// <para>
    /// 從 Repository 層提取的業務邏輯：查詢歷史統計資料 → 計算 Mean ± 6σ → 寫入 detection_specs。
    /// 使用 <see cref="IsolationLevel.RepeatableRead"/> 交易確保讀取資料一致性，
    /// 並在例外發生時執行明確的 Rollback。
    /// </para>
    /// </summary>
    public sealed class DetectionSpecService : IDetectionSpecService
    {
        private const string SiteMeanMethodCode = "SITE_MEAN";
        private const int PreferredHistoryCount = 30;
        private readonly IDbConnectionFactory _factory;
        private readonly IDetectionSpecRepository _specRepo;

        /// <summary>建立 <see cref="DetectionSpecService"/> 實例。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        /// <param name="specRepo">Spec 規格 Repository。</param>
        /// <exception cref="ArgumentNullException">任一參數為 null。</exception>
        public DetectionSpecService(IDbConnectionFactory factory, IDetectionSpecRepository specRepo)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _specRepo = specRepo ?? throw new ArgumentNullException(nameof(specRepo));
        }

        /// <inheritdoc />
        public long ComputeAndInsertSiteMeanSpec(
            string programName,
            uint siteId,
            string testItemName
        )
        {
            ValidateRequiredText(programName, nameof(programName));
            ValidateRequiredText(testItemName, nameof(testItemName));

            using (var conn = _factory.Create())
            using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
            {
                try
                {
                    var rows = QuerySiteMeanRows(conn, tx, programName, siteId, testItemName);

                    if (rows.Count == 0)
                        throw new InvalidOperationException(
                            $"No site_test_statistics data for program={programName}, "
                                + $"siteId={siteId}, testItem={testItemName}."
                        );

                    double mean,
                        std;
                    if (rows.Count >= 2)
                    {
                        var values = rows.Select(r => (double)r.MeanValue).ToList();
                        mean = Statistics.Mean(values);
                        std = Statistics.StandardDeviation(values);
                    }
                    else
                    {
                        mean = (double)rows[0].MeanValue;
                        std = 0.0;
                    }

                    var ucl = (decimal)(mean + 6.0 * std);
                    var lcl = (decimal)(mean - 6.0 * std);

                    var timesWithValue = rows.Where(r => r.StartTime.HasValue)
                        .Select(r => r.StartTime.Value)
                        .ToList();

                    if (!timesWithValue.Any())
                        throw new InvalidOperationException(
                            "All start_time values are NULL in site_test_statistics; "
                                + "cannot determine SpecCalcStartTime / SpecCalcEndTime."
                        );

                    var specCalcStart = timesWithValue.Min();
                    var specCalcEnd = timesWithValue.Max();

                    byte methodId = GetRequiredSiteMeanMethodId(conn, tx);

                    var spec = new DetectionSpec
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

                    long newId = _specRepo.Insert(spec, tx);
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

        // ── 私有輔助型別 & 方法 ───────────────────────────────────────────────

        private sealed class SiteMeanRow
        {
            public decimal MeanValue { get; set; }
            public DateTime? StartTime { get; set; }
        }

        /// <summary>
        /// 雙策略查詢：優先取最近 1 個月且計數 ≥ 30 的資料；
        /// 若不足 30 筆則回退為最新 30 筆（不限時間範圍）。
        /// </summary>
        private IReadOnlyList<SiteMeanRow> QuerySiteMeanRows(
            IDbConnection conn,
            IDbTransaction tx,
            string programName,
            uint siteId,
            string testItemName
        )
        {
            var p = new
            {
                ProgramName = programName,
                SiteId = siteId,
                TestItemName = testItemName,
            };

            const string sql1 =
                @"SELECT mean_value AS MeanValue, start_time AS StartTime
                  FROM   site_test_statistics
                  WHERE  program        = @ProgramName
                    AND  site_id        = @SiteId
                    AND  test_item_name = @TestItemName
                    AND  start_time    >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
                    AND  mean_value    IS NOT NULL
                  ORDER BY start_time DESC";

            var rows = conn.Query<SiteMeanRow>(sql1, p, tx).ToList();
            if (rows.Count >= PreferredHistoryCount)
                return rows;

            const string sql2 =
                @"SELECT mean_value AS MeanValue, start_time AS StartTime
                  FROM   site_test_statistics
                  WHERE  program        = @ProgramName
                    AND  site_id        = @SiteId
                    AND  test_item_name = @TestItemName
                    AND  mean_value    IS NOT NULL
                  ORDER BY start_time DESC
                  LIMIT 30";

            return conn.Query<SiteMeanRow>(sql2, p, tx).ToList();
        }

        private static void ValidateRequiredText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("參數不可為 null、空字串或空白。", parameterName);
        }

        private static byte GetRequiredSiteMeanMethodId(IDbConnection conn, IDbTransaction tx)
        {
            const string sql =
                "SELECT id FROM detection_methods WHERE method_code = @MethodCode LIMIT 1";

            var methodId = conn.ExecuteScalar<byte?>(sql, new { MethodCode = SiteMeanMethodCode }, tx);
            if (!methodId.HasValue)
                throw new InvalidOperationException(
                    "detection_methods 中找不到 method_code = 'SITE_MEAN' 的設定，無法建立 DetectionSpec。"
                );

            return methodId.Value;
        }
    }
}

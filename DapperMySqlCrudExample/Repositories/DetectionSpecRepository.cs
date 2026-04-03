using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using MathNet.Numerics.Statistics;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 偵測規格（detection_specs）資料表的 Repository 實作。
    /// <para>
    /// 提供標準 CRUD、分頁、計數、存在判斷、依偵測方法名稱的業務查詢，
    /// 以及 SITE_MEAN 規格計算方法。
    /// </para>
    /// </summary>
    public sealed class DetectionSpecRepository
    {
        private readonly DbConnectionFactory _factory;

        private const string SiteMeanMethodCode = "SITE_MEAN";
        private const int PreferredHistoryCount = 30;

        /// <summary>建立 <see cref="DetectionSpecRepository"/> 實例。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> 為 null。</exception>
        public DetectionSpecRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"ds.id                   AS Id,
              ds.program              AS Program,
              ds.test_item_name       AS TestItemName,
              ds.site_id              AS SiteId,
              ds.detection_method_id  AS DetectionMethodId,
              ds.spec_upper_limit     AS SpecUpperLimit,
              ds.spec_lower_limit     AS SpecLowerLimit,
              ds.spec_calc_start_time AS SpecCalcStartTime,
              ds.spec_calc_end_time   AS SpecCalcEndTime,
              ds.spec_calc_mean       AS SpecCalcMean,
              ds.spec_calc_std        AS SpecCalcStd,
              ds.created_at           AS CreatedAt,
              ds.updated_at           AS UpdatedAt";
        public IEnumerable<DetectionSpec> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM detection_specs ds ORDER BY ds.id";
            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql);
        }
        public DetectionSpec GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_specs ds WHERE ds.id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionSpec>(sql, new { Id = id });
        }
        public IEnumerable<DetectionSpec> GetByProgramAndMethod(
            string program,
            byte detectionMethodId
        )
        {
            var sql =
                $@"SELECT {SelectColumns}
                   FROM   detection_specs ds
                   WHERE  ds.program             = @Program
                     AND  ds.detection_method_id = @DetectionMethodId";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(
                    sql,
                    new { Program = program, DetectionMethodId = detectionMethodId }
                );
        }
        public IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(
            string program,
            string detectionMethodName
        )
        {
            var sql =
                $@"SELECT {SelectColumns}
                   FROM   detection_specs   ds
                   JOIN   detection_methods dm ON dm.id = ds.detection_method_id
                   WHERE  ds.program     = @Program
                     AND  dm.method_name = @DetectionMethodName
                     AND  ds.spec_calc_end_time >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
                   ORDER BY ds.spec_calc_end_time DESC";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(
                    sql,
                    new { Program = program, DetectionMethodName = detectionMethodName }
                );
        }
        public DetectionSpec GetLatestByProgramAndMethodName(
            string program,
            string detectionMethodName
        )
        {
            var sql =
                $@"SELECT {SelectColumns}
                   FROM   detection_specs   ds
                   JOIN   detection_methods dm ON dm.id = ds.detection_method_id
                   WHERE  ds.program     = @Program
                     AND  dm.method_name = @DetectionMethodName
                     AND  ds.spec_calc_end_time >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
                   ORDER BY ds.spec_calc_end_time DESC
                   LIMIT 1";

            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionSpec>(
                    sql,
                    new { Program = program, DetectionMethodName = detectionMethodName }
                );
        }
        public long Insert(DetectionSpec entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"INSERT INTO detection_specs
                      (program, test_item_name, site_id, detection_method_id,
                       spec_upper_limit, spec_lower_limit,
                       spec_calc_start_time, spec_calc_end_time,
                       spec_calc_mean, spec_calc_std)
                  VALUES
                      (@Program, @TestItemName, @SiteId, @DetectionMethodId,
                       @SpecUpperLimit, @SpecLowerLimit,
                       @SpecCalcStartTime, @SpecCalcEndTime,
                       @SpecCalcMean, @SpecCalcStd);
                  SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }
        public bool Update(DetectionSpec entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"UPDATE detection_specs
                  SET    program              = @Program,
                         test_item_name       = @TestItemName,
                         site_id              = @SiteId,
                         detection_method_id  = @DetectionMethodId,
                         spec_upper_limit     = @SpecUpperLimit,
                         spec_lower_limit     = @SpecLowerLimit,
                         spec_calc_start_time = @SpecCalcStartTime,
                         spec_calc_end_time   = @SpecCalcEndTime,
                         spec_calc_mean       = @SpecCalcMean,
                         spec_calc_std        = @SpecCalcStd
                  WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_specs WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM detection_specs WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM detection_specs";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }
        public IEnumerable<DetectionSpec> GetPaged(int offset, int limit)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset 不可小於 0。");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit 必須大於 0。");

            var sql =
                $"SELECT {SelectColumns} FROM detection_specs ds ORDER BY ds.id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql, new { Offset = offset, Limit = limit });
        }

        // ── SITE_MEAN 規格計算 ──────────────────────────────────────────────
        /// <summary>
        /// 依歷史 site_test_statistics 資料計算 SITE_MEAN 規格並插入 detection_specs。
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
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(testItemName));

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

                    var (mean, std) = CalculateMeanAndStd(rows);
                    var (ucl, lcl) = CalculateControlLimits(mean, std);
                    var (specCalcStart, specCalcEnd) = ExtractTimeRange(rows);
                    byte methodId = GetRequiredSiteMeanMethodId(conn, tx);

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

                    long newId = Insert(spec, tx);
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

        // ── 私有輔助型別 & 方法 ────────────────────────────────────────────

        private sealed class SiteMeanRow
        {
            public decimal MeanValue { get; set; }
            public DateTime? StartTime { get; set; }
        }

        /// <summary>
        /// 計算樣本平均值與標準差。
        /// </summary>
        private static (double mean, double std) CalculateMeanAndStd(IReadOnlyList<SiteMeanRow> rows)
        {
            if (rows.Count >= 2)
            {
                var values = rows.Select(r => (double)r.MeanValue).ToList();
                return (Statistics.Mean(values), Statistics.StandardDeviation(values));
            }

            return ((double)rows[0].MeanValue, 0.0);
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
        /// </summary>
        private static (DateTime start, DateTime end) ExtractTimeRange(IReadOnlyList<SiteMeanRow> rows)
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

        /// <summary>
        /// 雙策略查詢：優先取最近 1 個月且計數 ≥ 30 的資料；
        /// 若不足 30 筆則回退為最新 30 筆（不限時間範圍）。
        /// </summary>
        private static IReadOnlyList<SiteMeanRow> QuerySiteMeanRows(
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
                  LIMIT @Limit";

            return conn.Query<SiteMeanRow>(sql2, new { p.ProgramName, p.SiteId, p.TestItemName, Limit = PreferredHistoryCount }, tx).ToList();
        }

        private static byte GetRequiredSiteMeanMethodId(IDbConnection conn, IDbTransaction tx)
        {
            const string sql =
                "SELECT id FROM detection_methods WHERE method_code = @MethodCode";

            var methodId = conn.ExecuteScalar<byte?>(sql, new { MethodCode = SiteMeanMethodCode }, tx);
            if (!methodId.HasValue)
                throw new InvalidOperationException(
                    "detection_methods 中找不到 method_code = 'SITE_MEAN' 的設定，無法建立 DetectionSpec。"
                );

            return methodId.Value;
        }
    }
}

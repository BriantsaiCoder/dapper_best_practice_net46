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
    /// <summary>Spec 規格 Repository 實作</summary>
    public class DetectionSpecRepository : IDetectionSpecRepository
    {
        private readonly IDbConnectionFactory _factory;
        private byte? _siteMeanMethodId;

        public DetectionSpecRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns =
            @"
            id                   AS Id,
            program              AS Program,
            test_item_name       AS TestItemName,
            site_id              AS SiteId,
            detection_method_id  AS DetectionMethodId,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            spec_calc_mean       AS SpecCalcMean,
            spec_calc_std        AS SpecCalcStd,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        private const string JoinSelectColumns =
            @"
            ds.id                   AS Id,
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
            var sql = $"SELECT {SelectColumns} FROM detection_specs ORDER BY id LIMIT 10000";
            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql);
        }

        public DetectionSpec GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_specs WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionSpec>(sql, new { Id = id });
        }

        public IEnumerable<DetectionSpec> GetByProgramAndMethod(
            string program,
            byte detectionMethodId
        )
        {
            var sql =
                $@"
                SELECT {SelectColumns}
                FROM   detection_specs
                WHERE  program = @Program
                  AND  detection_method_id = @DetectionMethodId";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(
                    sql,
                    new { Program = program, DetectionMethodId = detectionMethodId }
                );
        }

        /// <summary>
        /// 依 detection method name 及 program 查詢最近一個月內計算的 spec 資料。
        /// 篩選條件：spec_calc_end_time >= NOW() - INTERVAL 1 MONTH。
        /// </summary>
        public IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(
            string program,
            string detectionMethodName
        )
        {
            var sql =
                $@"
                SELECT {JoinSelectColumns}
                FROM   detection_specs   ds
                JOIN   detection_methods dm ON dm.id = ds.detection_method_id
                WHERE  ds.program      = @Program
                  AND  dm.method_name  = @DetectionMethodName
                  AND  ds.spec_calc_end_time >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
                ORDER BY ds.spec_calc_end_time DESC";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(
                    sql,
                    new { Program = program, DetectionMethodName = detectionMethodName }
                );
        }

        /// <summary>
        /// 取最近一個月內 spec_calc_end_time 最大的單筆記錄（最新有效規格）。
        /// 找不到時回傳 null。
        /// </summary>
        public DetectionSpec GetLatestByProgramAndMethodName(
            string program,
            string detectionMethodName
        )
        {
            var sql =
                $@"
                SELECT {JoinSelectColumns}
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
            const string sql =
                @"
                INSERT INTO detection_specs
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
            const string sql =
                @"
                UPDATE detection_specs
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

        // ── 私有輔助型別 & 方法 ──────────────────────────────────────────────────

        private sealed class SiteMeanRow
        {
            public decimal MeanValue { get; set; }
            public DateTime? StartTime { get; set; }
        }

        private IReadOnlyList<SiteMeanRow> QuerySiteMeanRows(
            IDbConnection conn,
            IDbTransaction transaction,
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
            var rows = conn.Query<SiteMeanRow>(sql1, p, transaction).ToList();
            if (rows.Count >= 30)
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
            return conn.Query<SiteMeanRow>(sql2, p, transaction).ToList();
        }

        private byte GetOrCacheSiteMeanMethodId(IDbConnection conn, IDbTransaction transaction)
        {
            if (_siteMeanMethodId.HasValue)
                return _siteMeanMethodId.Value;

            _siteMeanMethodId = conn.ExecuteScalar<byte>(
                "SELECT id FROM detection_methods WHERE method_code = 'SITE_MEAN' LIMIT 1",
                transaction: transaction
            );
            return _siteMeanMethodId.Value;
        }

        /// <summary>查詢最近 30 筆 site_test_statistics，計算 Mean ± 3σ 後寫入 detection_specs。</summary>
        /// <remarks>使用單一連線 + 交易確保原子性，method_id 快取避免重複查詢。</remarks>
        public long ComputeAndInsertSiteMeanSpec(
            string programName,
            uint siteId,
            string testItemName
        )
        {
            using (var transaction = _factory.BeginTransaction())
            {
                var conn = transaction.Connection;
                var rows = QuerySiteMeanRows(conn, transaction, programName, siteId, testItemName);

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

                var methodId = GetOrCacheSiteMeanMethodId(conn, transaction);

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

                var newId = Insert(spec, transaction);
                transaction.Commit();
                return newId;
            }
        }
    }
}

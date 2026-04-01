using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 偵測規格（detection_specs）資料表的 Repository 實作。
    /// <para>
    /// 提供標準 CRUD、分頁、計數、存在判斷，以及依偵測方法名稱的業務查詢方法。
    /// 原有業務計算邏輯（SITE_MEAN 規格計算）已提取至
    /// <see cref="DapperMySqlCrudExample.Services.DetectionSpecService"/>。
    /// </para>
    /// </summary>
    public sealed class DetectionSpecRepository : IDetectionSpecRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 <see cref="DetectionSpecRepository"/> 實例。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public DetectionSpecRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns =
            @"id                   AS Id,
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

        /// <inheritdoc />
        public IEnumerable<DetectionSpec> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM detection_specs ORDER BY id LIMIT 10000";
            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql);
        }

        /// <inheritdoc />
        public DetectionSpec GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_specs WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionSpec>(sql, new { Id = id });
        }

        /// <inheritdoc />
        public IEnumerable<DetectionSpec> GetByProgramAndMethod(
            string program,
            byte detectionMethodId
        )
        {
            var sql =
                $@"SELECT {SelectColumns}
                   FROM   detection_specs
                   WHERE  program             = @Program
                     AND  detection_method_id = @DetectionMethodId";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(
                    sql,
                    new { Program = program, DetectionMethodId = detectionMethodId }
                );
        }

        /// <inheritdoc />
        public IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(
            string program,
            string detectionMethodName
        )
        {
            var sql =
                $@"SELECT {JoinSelectColumns}
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

        /// <inheritdoc />
        public DetectionSpec GetLatestByProgramAndMethodName(
            string program,
            string detectionMethodName
        )
        {
            var sql =
                $@"SELECT {JoinSelectColumns}
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

        /// <inheritdoc />
        public long Insert(DetectionSpec entity, IDbTransaction transaction = null)
        {
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

            return _factory.ExecuteScalar<long>(sql, entity, transaction);
        }

        /// <inheritdoc />
        public bool Update(DetectionSpec entity, IDbTransaction transaction = null)
        {
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

            return _factory.Execute(sql, entity, transaction);
        }

        /// <inheritdoc />
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_specs WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc />
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM detection_specs WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc />
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM detection_specs";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc />
        public IEnumerable<DetectionSpec> GetPaged(int offset, int limit)
        {
            var sql =
                $"SELECT {SelectColumns} FROM detection_specs ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

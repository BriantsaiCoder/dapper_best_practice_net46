using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// 偵測規格（detection_specs）資料表的 Repository 實作。
    /// 提供標準 CRUD、分頁、計數、存在判斷及依偵測方法名稱的業務查詢。
    /// </summary>
    public sealed class DetectionSpecRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 <see cref="DetectionSpecRepository"/> 實例。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> 為 null。</exception>
        public DetectionSpecRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>單表查詢用（與其他 Repository 一致，不帶表別名）。</summary>
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

        /// <summary>JOIN 查詢用（帶 ds. 表別名，避免與其他表欄位衝突）。</summary>
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

        public IEnumerable<DetectionSpec> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM detection_specs ORDER BY id";
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
                     AND  ds.spec_calc_end_time >= @SinceTime
                   ORDER BY ds.spec_calc_end_time DESC";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(
                    sql,
                    new
                    {
                        Program = program,
                        DetectionMethodName = detectionMethodName,
                        SinceTime = DateTime.Now.AddMonths(-1)
                    }
                );
        }

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
                     AND  ds.spec_calc_end_time >= @SinceTime
                   ORDER BY ds.spec_calc_end_time DESC
                   LIMIT 1";

            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionSpec>(
                    sql,
                    new
                    {
                        Program = program,
                        DetectionMethodName = detectionMethodName,
                        SinceTime = DateTime.Now.AddMonths(-1)
                    }
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
            const string sql = "SELECT 1 FROM detection_specs WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
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
                $"SELECT {SelectColumns} FROM detection_specs ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql, new { Offset = offset, Limit = limit });
        }

    }
}

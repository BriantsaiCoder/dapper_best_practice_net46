using System.Collections.Generic;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Spec 規格 Repository 實作</summary>
    public class DetectionSpecRepository : IDetectionSpecRepository
    {
        private readonly IDbConnectionFactory _factory;

        public DetectionSpecRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns = @"
            id                   AS Id,
            program              AS Program,
            test_item_name       AS TestItemName,
            site_id              AS SiteId,
            detection_method_id  AS DetectionMethodId,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

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

        public IEnumerable<DetectionSpec> GetByProgramAndMethod(string program, byte detectionMethodId)
        {
            var sql = $@"
                SELECT {SelectColumns}
                FROM   detection_specs
                WHERE  program = @Program
                  AND  detection_method_id = @DetectionMethodId";

            using (var conn = _factory.Create())
                return conn.Query<DetectionSpec>(sql, new { Program = program, DetectionMethodId = detectionMethodId });
        }

        public long Insert(DetectionSpec entity)
        {
            const string sql = @"
                INSERT INTO detection_specs
                    (program, test_item_name, site_id, detection_method_id,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@Program, @TestItemName, @SiteId, @DetectionMethodId,
                     @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(DetectionSpec entity)
        {
            const string sql = @"
                UPDATE detection_specs
                SET    program              = @Program,
                       test_item_name       = @TestItemName,
                       site_id              = @SiteId,
                       detection_method_id  = @DetectionMethodId,
                       spec_upper_limit     = @SpecUpperLimit,
                       spec_lower_limit     = @SpecLowerLimit,
                       spec_calc_start_time = @SpecCalcStartTime,
                       spec_calc_end_time   = @SpecCalcEndTime
                WHERE  id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id)
        {
            using (var conn = _factory.Create())
                return conn.Execute("DELETE FROM detection_specs WHERE id = @Id", new { Id = id }) > 0;
        }
    }
}

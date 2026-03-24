using System.Collections.Generic;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常 Unit 明細 Repository 實作</summary>
    public class AnomalyUnitRepository : IAnomalyUnitRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AnomalyUnitRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns = @"
            id                    AS Id,
            anomaly_test_item_id  AS AnomalyTestItemId,
            unit_id               AS UnitId,
            detection_value       AS DetectionValue,
            spec_upper_limit      AS SpecUpperLimit,
            spec_lower_limit      AS SpecLowerLimit,
            spec_calc_start_time  AS SpecCalcStartTime,
            spec_calc_end_time    AS SpecCalcEndTime,
            created_at            AS CreatedAt,
            updated_at            AS UpdatedAt";

        public IEnumerable<AnomalyUnit> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnit>(sql);
        }

        public AnomalyUnit GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyUnit>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyUnit> GetByAnomalyTestItemId(long anomalyTestItemId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units WHERE anomaly_test_item_id = @AnomalyTestItemId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnit>(sql, new { AnomalyTestItemId = anomalyTestItemId });
        }

        public long Insert(AnomalyUnit entity)
        {
            const string sql = @"
                INSERT INTO anomaly_units
                    (anomaly_test_item_id, unit_id, detection_value,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@AnomalyTestItemId, @UnitId, @DetectionValue,
                     @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyUnit entity)
        {
            const string sql = @"
                UPDATE anomaly_units
                SET    anomaly_test_item_id  = @AnomalyTestItemId,
                       unit_id               = @UnitId,
                       detection_value       = @DetectionValue,
                       spec_upper_limit      = @SpecUpperLimit,
                       spec_lower_limit      = @SpecLowerLimit,
                       spec_calc_start_time  = @SpecCalcStartTime,
                       spec_calc_end_time    = @SpecCalcEndTime
                WHERE  id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id)
        {
            using (var conn = _factory.Create())
                return conn.Execute("DELETE FROM anomaly_units WHERE id = @Id", new { Id = id }) > 0;
        }
    }
}

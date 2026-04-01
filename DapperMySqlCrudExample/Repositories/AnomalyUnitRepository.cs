using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// <see cref="IAnomalyUnitRepository"/> 的 Dapper 實作，對應 anomaly_units 資料表。
    /// </summary>
    public class AnomalyUnitRepository : IAnomalyUnitRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 AnomalyUnitRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
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

        /// <inheritdoc/>
        public IEnumerable<AnomalyUnit> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnit>(sql);
        }

        /// <inheritdoc/>
        public AnomalyUnit GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyUnit>(sql, new { Id = id });
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyUnit> GetByAnomalyTestItemId(long anomalyTestItemId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units WHERE anomaly_test_item_id = @AnomalyTestItemId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnit>(sql, new { AnomalyTestItemId = anomalyTestItemId });
        }

        /// <inheritdoc/>
        public long Insert(AnomalyUnit entity, IDbTransaction transaction = null)
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

            return _factory.ExecuteScalar<long>(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Update(AnomalyUnit entity, IDbTransaction transaction = null)
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

            return _factory.Execute(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_units WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc/>
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_units WHERE id = @Id";
            return _factory.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_units";
            return _factory.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyUnit> GetPaged(int offset, int limit)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnit>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

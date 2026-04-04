using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// AnomalyUnitRepository — anomaly_units 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class AnomalyUnitRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 AnomalyUnitRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyUnitRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
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

        public AnomalyUnit GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_units WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyUnit>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyUnit> GetByAnomalyTestItemId(long anomalyTestItemId)
        {
            var sql =
                $"SELECT {SelectColumns} FROM anomaly_units WHERE anomaly_test_item_id = @AnomalyTestItemId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnit>(sql, new { AnomalyTestItemId = anomalyTestItemId });
        }

        public long Insert(AnomalyUnit entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                INSERT INTO anomaly_units
                    (anomaly_test_item_id, unit_id, detection_value,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@AnomalyTestItemId, @UnitId, @DetectionValue,
                     @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyUnit entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE anomaly_units
                SET    anomaly_test_item_id  = @AnomalyTestItemId,
                       unit_id               = @UnitId,
                       detection_value       = @DetectionValue,
                       spec_upper_limit      = @SpecUpperLimit,
                       spec_lower_limit      = @SpecLowerLimit,
                       spec_calc_start_time  = @SpecCalcStartTime,
                       spec_calc_end_time    = @SpecCalcEndTime
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_units WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        public bool Exists(long id)
        {
            const string sql = "SELECT 1 FROM anomaly_units WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }

        /// <remarks>
        /// ⚠ 注意：COUNT(1) 在大量資料表上可能導致全表掃描，
        /// 僅適合資料量可控的場景或管理用途。
        /// </remarks>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_units";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }
    }
}

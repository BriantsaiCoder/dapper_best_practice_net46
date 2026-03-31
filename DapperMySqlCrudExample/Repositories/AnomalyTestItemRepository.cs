using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常測項明細 Repository 實作</summary>
    public class AnomalyTestItemRepository : IAnomalyTestItemRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AnomalyTestItemRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns =
            @"
            id                   AS Id,
            anomaly_lot_id       AS AnomalyLotId,
            test_item_name       AS TestItemName,
            detection_value      AS DetectionValue,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        public IEnumerable<AnomalyTestItem> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_test_items ORDER BY id LIMIT 10000";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyTestItem>(sql);
        }

        public AnomalyTestItem GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_test_items WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyTestItem>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyTestItem> GetByAnomalyLotId(long anomalyLotId)
        {
            var sql =
                $"SELECT {SelectColumns} FROM anomaly_test_items WHERE anomaly_lot_id = @AnomalyLotId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyTestItem>(sql, new { AnomalyLotId = anomalyLotId });
        }

        public long Insert(AnomalyTestItem entity, IDbTransaction transaction = null)
        {
            const string sql =
                @"
                INSERT INTO anomaly_test_items
                    (anomaly_lot_id, test_item_name, detection_value,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@AnomalyLotId, @TestItemName, @DetectionValue,
                     @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyTestItem entity, IDbTransaction transaction = null)
        {
            const string sql =
                @"
                UPDATE anomaly_test_items
                SET    anomaly_lot_id       = @AnomalyLotId,
                       test_item_name       = @TestItemName,
                       detection_value      = @DetectionValue,
                       spec_upper_limit     = @SpecUpperLimit,
                       spec_lower_limit     = @SpecLowerLimit,
                       spec_calc_start_time = @SpecCalcStartTime,
                       spec_calc_end_time   = @SpecCalcEndTime
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;
            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_test_items WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;
            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }
    }
}

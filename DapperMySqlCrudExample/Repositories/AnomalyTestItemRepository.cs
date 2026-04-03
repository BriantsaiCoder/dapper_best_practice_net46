using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// AnomalyTestItemRepository — anomaly_test_items 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class AnomalyTestItemRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 AnomalyTestItemRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyTestItemRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns = @"
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
            var sql = $"SELECT {SelectColumns} FROM anomaly_test_items ORDER BY id";
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
            var sql = $"SELECT {SelectColumns} FROM anomaly_test_items WHERE anomaly_lot_id = @AnomalyLotId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyTestItem>(sql, new { AnomalyLotId = anomalyLotId });
        }

        public long Insert(AnomalyTestItem entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
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
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
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

        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_test_items WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_test_items";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }

        public IEnumerable<AnomalyTestItem> GetPaged(int offset, int limit)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset 不可小於 0。");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit 必須大於 0。");

            var sql = $"SELECT {SelectColumns} FROM anomaly_test_items ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyTestItem>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

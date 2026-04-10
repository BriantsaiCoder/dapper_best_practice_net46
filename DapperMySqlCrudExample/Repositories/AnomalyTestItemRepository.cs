using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        private const string SelectColumns =
            @"
            id                   AS Id,
            anomaly_lot_id       AS AnomalyLotId,
            test_item_name       AS TestItemName,
            site_id              AS SiteId,
            detection_value      AS DetectionValue,
            offset_value         AS OffsetValue,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public AnomalyTestItem GetById(long id)
        {
            const string sql =
                "SELECT " + SelectColumns + " FROM anomaly_test_items WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<AnomalyTestItem>(sql, new { Id = id });
            }
        }

        /// <summary>依 anomaly_lot_id 查詢多筆資料。</summary>
        public IReadOnlyList<AnomalyTestItem> GetByAnomalyLotId(long anomalyLotId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_test_items WHERE anomaly_lot_id = @AnomalyLotId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyTestItem>(sql, new { AnomalyLotId = anomalyLotId })
                    .ToList();
            }
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        public long Insert(AnomalyTestItem entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string insertSql =
                @"
                INSERT INTO anomaly_test_items
                    (anomaly_lot_id, test_item_name, site_id, detection_value, offset_value,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@AnomalyLotId, @TestItemName, @SiteId, @DetectionValue, @OffsetValue,
                     @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);";
            const string lastInsertIdSql = "SELECT LAST_INSERT_ID();";

            if (transaction != null)
            {
                transaction.Connection.Execute(insertSql, entity, transaction);
                return transaction.Connection.ExecuteScalar<long>(lastInsertIdSql, transaction: transaction);
            }

            using (var conn = _factory.Create())
            {
                conn.Execute(insertSql, entity);
                return conn.ExecuteScalar<long>(lastInsertIdSql);
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(AnomalyTestItem entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE anomaly_test_items
                SET    anomaly_lot_id       = @AnomalyLotId,
                       test_item_name       = @TestItemName,
                       site_id              = @SiteId,
                       detection_value      = @DetectionValue,
                       offset_value         = @OffsetValue,
                       spec_upper_limit     = @SpecUpperLimit,
                       spec_lower_limit     = @SpecLowerLimit,
                       spec_calc_start_time = @SpecCalcStartTime,
                       spec_calc_end_time   = @SpecCalcEndTime
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
            {
                return conn.Execute(sql, entity) > 0;
            }
        }

        /// <summary>依主鍵刪除一筆資料。</summary>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_test_items WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
            {
                return conn.Execute(sql, new { Id = id }) > 0;
            }
        }

        /// <summary>檢查指定主鍵的資料是否存在。</summary>
        public bool Exists(long id)
        {
            const string sql = "SELECT 1 FROM anomaly_test_items WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

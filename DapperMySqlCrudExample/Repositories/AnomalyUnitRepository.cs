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
            offset_value          AS OffsetValue,
            spec_upper_limit      AS SpecUpperLimit,
            spec_lower_limit      AS SpecLowerLimit,
            spec_calc_start_time  AS SpecCalcStartTime,
            spec_calc_end_time    AS SpecCalcEndTime,
            created_at            AS CreatedAt,
            updated_at            AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public AnomalyUnit GetById(long id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM anomaly_units WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<AnomalyUnit>(sql, new { Id = id });
            }
        }

        /// <summary>依 anomaly_test_item_id 查詢多筆資料。</summary>
        public IReadOnlyList<AnomalyUnit> GetByAnomalyTestItemId(long anomalyTestItemId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_units WHERE anomaly_test_item_id = @AnomalyTestItemId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyUnit>(sql, new { AnomalyTestItemId = anomalyTestItemId })
                    .ToList();
            }
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        /// <remarks>
        /// INSERT 與 SELECT LAST_INSERT_ID() 拆為兩步驟執行：
        /// MySql.Data 6.x 的 ExecuteScalar 處理多語句批次時，會回傳第一個語句（INSERT）的結果，
        /// 導致 LAST_INSERT_ID() 的值被忽略而回傳 0。
        /// 拆分後在同一連線（或交易）上依序執行，確保取得正確的自動遞增主鍵。
        /// </remarks>
        public long Insert(AnomalyUnit entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string insertSql =
                @"
                INSERT INTO anomaly_units
                    (anomaly_test_item_id, unit_id, detection_value, offset_value,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@AnomalyTestItemId, @UnitId, @DetectionValue, @OffsetValue,
                     @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime)";

            const string identitySql = "SELECT LAST_INSERT_ID()";

            if (transaction != null)
            {
                transaction.Connection.Execute(insertSql, entity, transaction);
                return transaction.Connection.ExecuteScalar<long>(identitySql, transaction: transaction);
            }

            using (var conn = _factory.Create())
            {
                conn.Execute(insertSql, entity);
                return conn.ExecuteScalar<long>(identitySql);
            }
        }

        /// <summary>更新一筆資料。</summary>
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
                       offset_value          = @OffsetValue,
                       spec_upper_limit      = @SpecUpperLimit,
                       spec_lower_limit      = @SpecLowerLimit,
                       spec_calc_start_time  = @SpecCalcStartTime,
                       spec_calc_end_time    = @SpecCalcEndTime
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
            const string sql = "DELETE FROM anomaly_units WHERE id = @Id";

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
            const string sql = "SELECT 1 FROM anomaly_units WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

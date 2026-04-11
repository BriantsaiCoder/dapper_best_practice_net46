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
    /// AnomalyLotRepository — anomaly_lots 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class AnomalyLotRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 AnomalyLotRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyLotRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
            id                   AS Id,
            lots_info_id         AS LotsInfoId,
            detection_method_id  AS DetectionMethodId,
            detection_value      AS DetectionValue,
            offset_value         AS OffsetValue,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public AnomalyLot GetById(long id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM anomaly_lots WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<AnomalyLot>(sql, new { Id = id });
            }
        }

        /// <summary>依 lots_info_id 查詢多筆資料。</summary>
        public IReadOnlyList<AnomalyLot> GetByLotsInfoId(int lotsInfoId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_lots WHERE lots_info_id = @LotsInfoId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyLot>(sql, new { LotsInfoId = lotsInfoId }).ToList();
            }
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        /// <remarks>
        /// INSERT 與 SELECT LAST_INSERT_ID() 拆為兩步驟執行：
        /// MySql.Data 6.x 的 ExecuteScalar 處理多語句批次時，會回傳第一個語句（INSERT）的結果，
        /// 導致 LAST_INSERT_ID() 的值被忽略而回傳 0。
        /// 拆分後在同一連線（或交易）上依序執行，確保取得正確的自動遞增主鍵。
        /// </remarks>
        public long Insert(AnomalyLot entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string insertSql =
                @"
                INSERT INTO anomaly_lots
                    (lots_info_id, detection_method_id, detection_value, offset_value,
                     spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@LotsInfoId, @DetectionMethodId, @DetectionValue, @OffsetValue,
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
                conn.Open();
                conn.Execute(insertSql, entity);
                return conn.ExecuteScalar<long>(identitySql);
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(AnomalyLot entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE anomaly_lots
                SET    lots_info_id         = @LotsInfoId,
                       detection_method_id  = @DetectionMethodId,
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
            const string sql = "DELETE FROM anomaly_lots WHERE id = @Id";

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
            const string sql = "SELECT 1 FROM anomaly_lots WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

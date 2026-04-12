using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using NLog;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// AnomalyLotProcessMappingRepository —
    /// anomaly_lot_process_mapping 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class AnomalyLotProcessMappingRepository
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 AnomalyLotProcessMappingRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyLotProcessMappingRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
            id               AS Id,
            anomaly_lot_id   AS AnomalyLotId,
            plant_name       AS PlantName,
            station_name     AS StationName,
            machine_id       AS MachineId,
            trackin_user     AS TrackinUser,
            trackout_user    AS TrackoutUser,
            recipe           AS Recipe,
            created_at       AS CreatedAt,
            updated_at       AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public AnomalyLotProcessMapping GetById(long id)
        {
            const string sql =
                "SELECT " + SelectColumns + " FROM anomaly_lot_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<AnomalyLotProcessMapping>(sql, new { Id = id });
            }
        }

        /// <summary>依 anomaly_lot_id 查詢多筆資料。</summary>
        public IReadOnlyList<AnomalyLotProcessMapping> GetByAnomalyLotId(long anomalyLotId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_lot_process_mapping WHERE anomaly_lot_id = @AnomalyLotId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyLotProcessMapping>(
                        sql,
                        new { AnomalyLotId = anomalyLotId }
                    )
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
        public long Insert(AnomalyLotProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            const string insertSql =
                @"
                INSERT INTO anomaly_lot_process_mapping
                    (anomaly_lot_id, plant_name, station_name, machine_id,
                     trackin_user, trackout_user, recipe)
                VALUES
                    (@AnomalyLotId, @PlantName, @StationName, @MachineId,
                     @TrackinUser, @TrackoutUser, @Recipe)";

            const string identitySql = "SELECT LAST_INSERT_ID()";

            try
            {
                if (transaction != null)
                {
                    transaction.Connection.Execute(insertSql, entity, transaction);
                    return transaction.Connection.ExecuteScalar<long>(
                        identitySql,
                        transaction: transaction
                    );
                }

                using (var conn = _factory.Create())
                {
                    conn.Open();
                    conn.Execute(insertSql, entity);
                    return conn.ExecuteScalar<long>(identitySql);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Insert anomaly_lot_process_mapping 失敗 | AnomalyLotId={AnomalyLotId}",
                    entity.AnomalyLotId
                );
                throw;
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(AnomalyLotProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            const string sql =
                @"
                UPDATE anomaly_lot_process_mapping
                SET    anomaly_lot_id = @AnomalyLotId,
                       plant_name     = @PlantName,
                       station_name   = @StationName,
                       machine_id     = @MachineId,
                       trackin_user   = @TrackinUser,
                       trackout_user  = @TrackoutUser,
                       recipe         = @Recipe
                WHERE  id = @Id";

            try
            {
                if (transaction != null)
                {
                    return transaction.Connection.Execute(sql, entity, transaction) > 0;
                }

                using (var conn = _factory.Create())
                {
                    return conn.Execute(sql, entity) > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Update anomaly_lot_process_mapping 失敗 | Id={Id}", entity.Id);
                throw;
            }
        }

        /// <summary>依主鍵刪除一筆資料。</summary>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_lot_process_mapping WHERE id = @Id";

            try
            {
                if (transaction != null)
                {
                    return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;
                }

                using (var conn = _factory.Create())
                {
                    return conn.Execute(sql, new { Id = id }) > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Delete anomaly_lot_process_mapping 失敗 | Id={Id}", id);
                throw;
            }
        }

        /// <summary>檢查指定主鍵的資料是否存在。</summary>
        public bool Exists(long id)
        {
            const string sql = "SELECT 1 FROM anomaly_lot_process_mapping WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

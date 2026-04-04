using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// AnomalyLotProcessMappingRepository —
    /// anomaly_lot_process_mapping 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class AnomalyLotProcessMappingRepository
    {
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
            station_name     AS StationName,
            equipment_id     AS EquipmentId,
            process_time     AS ProcessTime,
            created_at       AS CreatedAt,
            updated_at       AS UpdatedAt";

        public AnomalyLotProcessMapping GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyLotProcessMapping>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyLotProcessMapping> GetByAnomalyLotId(long anomalyLotId)
        {
            var sql =
                $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping WHERE anomaly_lot_id = @AnomalyLotId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLotProcessMapping>(
                    sql,
                    new { AnomalyLotId = anomalyLotId }
                );
        }

        public long Insert(AnomalyLotProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                INSERT INTO anomaly_lot_process_mapping
                    (anomaly_lot_id, station_name, equipment_id, process_time)
                VALUES
                    (@AnomalyLotId, @StationName, @EquipmentId, @ProcessTime);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyLotProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE anomaly_lot_process_mapping
                SET    anomaly_lot_id = @AnomalyLotId,
                       station_name   = @StationName,
                       equipment_id   = @EquipmentId,
                       process_time   = @ProcessTime
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_lot_process_mapping WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        public bool Exists(long id)
        {
            const string sql = "SELECT 1 FROM anomaly_lot_process_mapping WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }

        /// <remarks>
        /// ⚠ 注意：COUNT(1) 在大量資料表上可能導致全表掃描，
        /// 僅適合資料量可控的場景或管理用途。
        /// </remarks>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_lot_process_mapping";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }
    }
}

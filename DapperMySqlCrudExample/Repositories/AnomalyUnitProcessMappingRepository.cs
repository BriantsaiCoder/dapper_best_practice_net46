using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// AnomalyUnitProcessMappingRepository —
    /// anomaly_unit_process_mapping 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class AnomalyUnitProcessMappingRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 AnomalyUnitProcessMappingRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyUnitProcessMappingRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns = @"
            id                AS Id,
            anomaly_unit_id   AS AnomalyUnitId,
            boat_id           AS BoatId,
            position_x        AS PositionX,
            position_y        AS PositionY,
            process_time      AS ProcessTime,
            station_name      AS StationName,
            equipment_id      AS EquipmentId,
            created_at        AS CreatedAt,
            updated_at        AS UpdatedAt";
        public IEnumerable<AnomalyUnitProcessMapping> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(sql);
        }

        public AnomalyUnitProcessMapping GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyUnitProcessMapping>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyUnitProcessMapping> GetByAnomalyUnitId(long anomalyUnitId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping WHERE anomaly_unit_id = @AnomalyUnitId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(sql, new { AnomalyUnitId = anomalyUnitId });
        }

        public long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
                INSERT INTO anomaly_unit_process_mapping
                    (anomaly_unit_id, boat_id, position_x, position_y,
                     process_time, station_name, equipment_id)
                VALUES
                    (@AnomalyUnitId, @BoatId, @PositionX, @PositionY,
                     @ProcessTime, @StationName, @EquipmentId);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
                UPDATE anomaly_unit_process_mapping
                SET    anomaly_unit_id = @AnomalyUnitId,
                       boat_id         = @BoatId,
                       position_x      = @PositionX,
                       position_y      = @PositionY,
                       process_time    = @ProcessTime,
                       station_name    = @StationName,
                       equipment_id    = @EquipmentId
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_unit_process_mapping WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_unit_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_unit_process_mapping";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }

        public IEnumerable<AnomalyUnitProcessMapping> GetPaged(int offset, int limit)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset 不可小於 0。");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit 必須大於 0。");

            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

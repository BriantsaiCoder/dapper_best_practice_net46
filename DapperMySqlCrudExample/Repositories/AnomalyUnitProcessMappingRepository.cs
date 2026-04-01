using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// <see cref="IAnomalyUnitProcessMappingRepository"/> 的 Dapper 實作，
    /// 對應 anomaly_unit_process_mapping 資料表。
    /// </summary>
    public class AnomalyUnitProcessMappingRepository : IAnomalyUnitProcessMappingRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 AnomalyUnitProcessMappingRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyUnitProcessMappingRepository(IDbConnectionFactory factory)
        {
            _factory = RepositoryGuards.RequireFactory(factory, nameof(factory));
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

        /// <inheritdoc/>
        public IEnumerable<AnomalyUnitProcessMapping> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(sql);
        }

        /// <inheritdoc/>
        public AnomalyUnitProcessMapping GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyUnitProcessMapping>(sql, new { Id = id });
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyUnitProcessMapping> GetByAnomalyUnitId(long anomalyUnitId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping WHERE anomaly_unit_id = @AnomalyUnitId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(sql, new { AnomalyUnitId = anomalyUnitId });
        }

        /// <inheritdoc/>
        public long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            RepositoryGuards.RequireEntity(entity, nameof(entity));

            const string sql = @"
                INSERT INTO anomaly_unit_process_mapping
                    (anomaly_unit_id, boat_id, position_x, position_y,
                     process_time, station_name, equipment_id)
                VALUES
                    (@AnomalyUnitId, @BoatId, @PositionX, @PositionY,
                     @ProcessTime, @StationName, @EquipmentId);
                SELECT LAST_INSERT_ID();";

            return _factory.ExecuteScalar<long>(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Update(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            RepositoryGuards.RequireEntity(entity, nameof(entity));

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

            return _factory.Execute(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_unit_process_mapping WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc/>
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_unit_process_mapping WHERE id = @Id";
            return _factory.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_unit_process_mapping";
            return _factory.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyUnitProcessMapping> GetPaged(int offset, int limit)
        {
            RepositoryGuards.ValidatePaging(offset, limit);

            var sql = $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

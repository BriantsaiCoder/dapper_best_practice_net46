using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Unit Process Mapping Repository 實作</summary>
    public class AnomalyUnitProcessMappingRepository : IAnomalyUnitProcessMappingRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AnomalyUnitProcessMappingRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns =
            @"
            id              AS Id,
            anomaly_unit_id AS AnomalyUnitId,
            boat_id         AS BoatId,
            position_x      AS PositionX,
            position_y      AS PositionY,
            process_time    AS ProcessTime,
            station_name    AS StationName,
            equipment_id    AS EquipmentId,
            created_at      AS CreatedAt,
            updated_at      AS UpdatedAt";

        public IEnumerable<AnomalyUnitProcessMapping> GetAll()
        {
            var sql =
                $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping ORDER BY id LIMIT 10000";
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
            var sql =
                $"SELECT {SelectColumns} FROM anomaly_unit_process_mapping WHERE anomaly_unit_id = @AnomalyUnitId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(
                    sql,
                    new { AnomalyUnitId = anomalyUnitId }
                );
        }

        public long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            const string sql =
                @"
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
            const string sql =
                @"
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
    }
}

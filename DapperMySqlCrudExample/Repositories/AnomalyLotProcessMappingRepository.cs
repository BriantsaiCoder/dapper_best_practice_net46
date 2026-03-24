using System.Collections.Generic;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>批號 Process Mapping Repository 實作</summary>
    public class AnomalyLotProcessMappingRepository : IAnomalyLotProcessMappingRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AnomalyLotProcessMappingRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns = @"
            id             AS Id,
            anomaly_lot_id AS AnomalyLotId,
            station_name   AS StationName,
            equipment_id   AS EquipmentId,
            process_time   AS ProcessTime,
            created_at     AS CreatedAt,
            updated_at     AS UpdatedAt";

        public IEnumerable<AnomalyLotProcessMapping> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLotProcessMapping>(sql);
        }

        public AnomalyLotProcessMapping GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyLotProcessMapping>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyLotProcessMapping> GetByAnomalyLotId(long anomalyLotId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping WHERE anomaly_lot_id = @AnomalyLotId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLotProcessMapping>(sql, new { AnomalyLotId = anomalyLotId });
        }

        public long Insert(AnomalyLotProcessMapping entity)
        {
            const string sql = @"
                INSERT INTO anomaly_lot_process_mapping
                    (anomaly_lot_id, station_name, equipment_id, process_time)
                VALUES
                    (@AnomalyLotId, @StationName, @EquipmentId, @ProcessTime);
                SELECT LAST_INSERT_ID();";

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyLotProcessMapping entity)
        {
            const string sql = @"
                UPDATE anomaly_lot_process_mapping
                SET    anomaly_lot_id = @AnomalyLotId,
                       station_name   = @StationName,
                       equipment_id   = @EquipmentId,
                       process_time   = @ProcessTime
                WHERE  id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id)
        {
            using (var conn = _factory.Create())
                return conn.Execute("DELETE FROM anomaly_lot_process_mapping WHERE id = @Id", new { Id = id }) > 0;
        }
    }
}

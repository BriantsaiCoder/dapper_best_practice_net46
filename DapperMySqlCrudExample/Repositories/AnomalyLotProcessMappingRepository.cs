using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// <see cref="IAnomalyLotProcessMappingRepository"/> 的 Dapper 實作，
    /// 對應 anomaly_lot_process_mapping 資料表。
    /// </summary>
    public class AnomalyLotProcessMappingRepository : IAnomalyLotProcessMappingRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 AnomalyLotProcessMappingRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyLotProcessMappingRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns = @"
            id               AS Id,
            anomaly_lot_id   AS AnomalyLotId,
            station_name     AS StationName,
            equipment_id     AS EquipmentId,
            process_time     AS ProcessTime,
            created_at       AS CreatedAt,
            updated_at       AS UpdatedAt";

        /// <inheritdoc/>
        public IEnumerable<AnomalyLotProcessMapping> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLotProcessMapping>(sql);
        }

        /// <inheritdoc/>
        public AnomalyLotProcessMapping GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyLotProcessMapping>(sql, new { Id = id });
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyLotProcessMapping> GetByAnomalyLotId(long anomalyLotId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping WHERE anomaly_lot_id = @AnomalyLotId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLotProcessMapping>(sql, new { AnomalyLotId = anomalyLotId });
        }

        /// <inheritdoc/>
        public long Insert(AnomalyLotProcessMapping entity, IDbTransaction transaction = null)
        {
            const string sql = @"
                INSERT INTO anomaly_lot_process_mapping
                    (anomaly_lot_id, station_name, equipment_id, process_time)
                VALUES
                    (@AnomalyLotId, @StationName, @EquipmentId, @ProcessTime);
                SELECT LAST_INSERT_ID();";

            return _factory.ExecuteScalar<long>(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Update(AnomalyLotProcessMapping entity, IDbTransaction transaction = null)
        {
            const string sql = @"
                UPDATE anomaly_lot_process_mapping
                SET    anomaly_lot_id = @AnomalyLotId,
                       station_name   = @StationName,
                       equipment_id   = @EquipmentId,
                       process_time   = @ProcessTime
                WHERE  id = @Id";

            return _factory.Execute(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_lot_process_mapping WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc/>
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_lot_process_mapping WHERE id = @Id";
            return _factory.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_lot_process_mapping";
            return _factory.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyLotProcessMapping> GetPaged(int offset, int limit)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lot_process_mapping ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLotProcessMapping>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

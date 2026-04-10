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

        private const string SelectColumns =
            @"
            id                AS Id,
            anomaly_unit_id   AS AnomalyUnitId,
            boat_id           AS BoatId,
            boat_x            AS BoatX,
            boat_y            AS BoatY,
            wafer_barcode     AS WaferBarcode,
            wafer_id          AS WaferId,
            wafer_x           AS WaferX,
            wafer_y           AS WaferY,
            substrate_id      AS SubstrateId,
            substrate_x       AS SubstrateX,
            substrate_y       AS SubstrateY,
            wafer_max_x       AS WaferMaxX,
            wafer_max_y       AS WaferMaxY,
            boat_max_x        AS BoatMaxX,
            boat_max_y        AS BoatMaxY,
            txn_time          AS TxnTime,
            plant_name        AS PlantName,
            station_name      AS StationName,
            equipment_id      AS EquipmentId,
            created_at        AS CreatedAt,
            updated_at        AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public AnomalyUnitProcessMapping GetById(long id)
        {
            const string sql =
                "SELECT " + SelectColumns + " FROM anomaly_unit_process_mapping WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<AnomalyUnitProcessMapping>(sql, new { Id = id });
            }
        }

        /// <summary>依 anomaly_unit_id 查詢多筆資料。</summary>
        public IReadOnlyList<AnomalyUnitProcessMapping> GetByAnomalyUnitId(long anomalyUnitId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_unit_process_mapping WHERE anomaly_unit_id = @AnomalyUnitId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyUnitProcessMapping>(
                        sql,
                        new { AnomalyUnitId = anomalyUnitId }
                    )
                    .ToList();
            }
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        public long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                INSERT INTO anomaly_unit_process_mapping
                    (anomaly_unit_id, boat_id, boat_x, boat_y,
                     wafer_barcode, wafer_id, wafer_x, wafer_y,
                     substrate_id, substrate_x, substrate_y,
                     wafer_max_x, wafer_max_y, boat_max_x, boat_max_y,
                     txn_time, plant_name, station_name, equipment_id)
                VALUES
                    (@AnomalyUnitId, @BoatId, @BoatX, @BoatY,
                     @WaferBarcode, @WaferId, @WaferX, @WaferY,
                     @SubstrateId, @SubstrateX, @SubstrateY,
                     @WaferMaxX, @WaferMaxY, @BoatMaxX, @BoatMaxY,
                     @TxnTime, @PlantName, @StationName, @EquipmentId);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
            {
                return conn.ExecuteScalar<long>(sql, entity);
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE anomaly_unit_process_mapping
                SET    anomaly_unit_id   = @AnomalyUnitId,
                       boat_id           = @BoatId,
                       boat_x            = @BoatX,
                       boat_y            = @BoatY,
                       wafer_barcode     = @WaferBarcode,
                       wafer_id          = @WaferId,
                       wafer_x           = @WaferX,
                       wafer_y           = @WaferY,
                       substrate_id      = @SubstrateId,
                       substrate_x       = @SubstrateX,
                       substrate_y       = @SubstrateY,
                       wafer_max_x       = @WaferMaxX,
                       wafer_max_y       = @WaferMaxY,
                       boat_max_x        = @BoatMaxX,
                       boat_max_y        = @BoatMaxY,
                       txn_time          = @TxnTime,
                       plant_name        = @PlantName,
                       station_name      = @StationName,
                       equipment_id      = @EquipmentId
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
            const string sql = "DELETE FROM anomaly_unit_process_mapping WHERE id = @Id";

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
            const string sql = "SELECT 1 FROM anomaly_unit_process_mapping WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

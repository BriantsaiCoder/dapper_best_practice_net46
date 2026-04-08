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
            boat_position_x   AS BoatPositionX,
            boat_position_y   AS BoatPositionY,
            wafer_id          AS WaferId,
            wafer_position_x  AS WaferPositionX,
            wafer_position_y  AS WaferPositionY,
            sbs_id            AS SbsId,
            sbs_position_x    AS SbsPositionX,
            sbs_position_y    AS SbsPositionY,
            process_time      AS ProcessTime,
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
                return conn.QueryFirstOrDefault<AnomalyUnitProcessMapping>(sql, new { Id = id });
        }

        /// <summary>依 anomaly_unit_id 查詢多筆資料。</summary>
        public IReadOnlyList<AnomalyUnitProcessMapping> GetByAnomalyUnitId(long anomalyUnitId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_unit_process_mapping WHERE anomaly_unit_id = @AnomalyUnitId ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyUnitProcessMapping>(
                        sql,
                        new { AnomalyUnitId = anomalyUnitId }
                    )
                    .ToList();
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        public long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                INSERT INTO anomaly_unit_process_mapping
                    (anomaly_unit_id, boat_id, boat_position_x, boat_position_y,
                     wafer_id, wafer_position_x, wafer_position_y,
                     sbs_id, sbs_position_x, sbs_position_y,
                     process_time, station_name, equipment_id)
                VALUES
                    (@AnomalyUnitId, @BoatId, @BoatPositionX, @BoatPositionY,
                     @WaferId, @WaferPositionX, @WaferPositionY,
                     @SbsId, @SbsPositionX, @SbsPositionY,
                     @ProcessTime, @StationName, @EquipmentId);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
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
                       boat_position_x   = @BoatPositionX,
                       boat_position_y   = @BoatPositionY,
                       wafer_id          = @WaferId,
                       wafer_position_x  = @WaferPositionX,
                       wafer_position_y  = @WaferPositionY,
                       sbs_id            = @SbsId,
                       sbs_position_x    = @SbsPositionX,
                       sbs_position_y    = @SbsPositionY,
                       process_time      = @ProcessTime,
                       station_name      = @StationName,
                       equipment_id      = @EquipmentId
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        /// <summary>依主鍵刪除一筆資料。</summary>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_unit_process_mapping WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        /// <summary>檢查指定主鍵的資料是否存在。</summary>
        public bool Exists(long id)
        {
            const string sql = "SELECT 1 FROM anomaly_unit_process_mapping WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }
    }
}

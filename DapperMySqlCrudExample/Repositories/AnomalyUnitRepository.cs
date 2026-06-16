using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Models.QueryModels;
using NLog;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// AnomalyUnitRepository — anomaly_units 資料表的 Dapper 資料存取。
    /// <para>
    /// anomaly_units 為合併測項與 Unit 層的異常明細表：
    /// unit_id = "" 表示無 Unit 的測項層異常；unit_id 有值表示 Unit 層異常。
    /// </para>
    /// </summary>
    public sealed class AnomalyUnitRepository
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 AnomalyUnitRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyUnitRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
            id                AS Id,
            anomaly_lot_id    AS AnomalyLotId,
            test_item_name    AS TestItemName,
            site_id           AS SiteId,
            unit_id           AS UnitId,
            detection_value   AS DetectionValue,
            offset_value      AS OffsetValue,
            spec_upper_limit  AS SpecUpperLimit,
            spec_lower_limit  AS SpecLowerLimit,
            created_at        AS CreatedAt,
            updated_at        AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public AnomalyUnit GetById(long id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM anomaly_units WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<AnomalyUnit>(sql, new { Id = id });
            }
        }

        /// <summary>依 anomaly_lot_id 查詢多筆異常明細。</summary>
        public IReadOnlyList<AnomalyUnit> GetByAnomalyLotId(long anomalyLotId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_units WHERE anomaly_lot_id = @AnomalyLotId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyUnit>(sql, new { AnomalyLotId = anomalyLotId }).ToList();
            }
        }

        /// <summary>依 anomaly_lot_id 與 test_item_name 查詢多筆異常明細。</summary>
        public IReadOnlyList<AnomalyUnit> GetByAnomalyLotIdAndTestItemName(
            long anomalyLotId,
            string testItemName
        )
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM anomaly_units"
                + " WHERE anomaly_lot_id = @AnomalyLotId AND test_item_name = @TestItemName"
                + " ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyUnit>(
                        sql,
                        new { AnomalyLotId = anomalyLotId, TestItemName = testItemName }
                    )
                    .ToList();
            }
        }

        /// <summary>
        /// 依 anomaly_lot_id 查詢有 Unit 的異常明細及其製程追溯（unit_id 非空字串）。
        /// 回傳結果為 anomaly_units INNER JOIN anomaly_unit_process_mapping 的複合資料。
        /// </summary>
        public IReadOnlyList<AnomalyUnitWithMapping> GetWithMappingByAnomalyLotId(long anomalyLotId)
        {
            const string sql =
                @"SELECT
                    au.id                AS Id,
                    au.anomaly_lot_id    AS AnomalyLotId,
                    au.test_item_name    AS TestItemName,
                    au.site_id           AS SiteId,
                    au.unit_id           AS UnitId,
                    au.detection_value   AS DetectionValue,
                    au.offset_value      AS OffsetValue,
                    au.spec_upper_limit  AS SpecUpperLimit,
                    au.spec_lower_limit  AS SpecLowerLimit,
                    m.id                 AS MappingId,
                    m.boat_id            AS BoatId,
                    m.boat_x             AS BoatX,
                    m.boat_y             AS BoatY,
                    m.wafer_barcode      AS WaferBarcode,
                    m.wafer_id           AS WaferId,
                    m.wafer_x            AS WaferX,
                    m.wafer_y            AS WaferY,
                    m.substrate_id       AS SubstrateId,
                    m.substrate_x        AS SubstrateX,
                    m.substrate_y        AS SubstrateY,
                    m.wafer_max_x        AS WaferMaxX,
                    m.wafer_max_y        AS WaferMaxY,
                    m.boat_max_x         AS BoatMaxX,
                    m.boat_max_y         AS BoatMaxY,
                    m.plant_name         AS MappingPlantName,
                    m.station_name       AS MappingStationName,
                    m.equipment_id       AS EquipmentId
                FROM anomaly_units au
                INNER JOIN anomaly_unit_process_mapping m ON m.anomaly_unit_id = au.id
                WHERE au.anomaly_lot_id = @AnomalyLotId AND au.unit_id <> '' -- 排除測項層異常（unit_id = '' 的列）
                ORDER BY au.id";
            using (var conn = _factory.Create())
            {
                return conn.Query<AnomalyUnitWithMapping>(sql, new { AnomalyLotId = anomalyLotId })
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
        public long Insert(AnomalyUnit entity, IDbTransaction transaction = null)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            const string insertSql =
                @"
                INSERT INTO anomaly_units
                    (anomaly_lot_id, test_item_name, site_id, unit_id,
                     detection_value, offset_value, spec_upper_limit, spec_lower_limit)
                VALUES
                    (@AnomalyLotId, @TestItemName, @SiteId, @UnitId,
                     @DetectionValue, @OffsetValue, @SpecUpperLimit, @SpecLowerLimit)";

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
                    "Insert anomaly_units 失敗 | AnomalyLotId={AnomalyLotId} | TestItemName={TestItemName} | UnitId={UnitId}",
                    entity.AnomalyLotId,
                    entity.TestItemName,
                    entity.UnitId
                );
                throw;
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(AnomalyUnit entity, IDbTransaction transaction = null)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            const string sql =
                @"
                UPDATE anomaly_units
                SET    anomaly_lot_id   = @AnomalyLotId,
                       test_item_name   = @TestItemName,
                       site_id          = @SiteId,
                       unit_id          = @UnitId,
                       detection_value  = @DetectionValue,
                       offset_value     = @OffsetValue,
                       spec_upper_limit = @SpecUpperLimit,
                       spec_lower_limit = @SpecLowerLimit
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
                _logger.Error(ex, "Update anomaly_units 失敗 | Id={Id}", entity.Id);
                throw;
            }
        }

        /// <summary>依主鍵刪除一筆資料。</summary>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_units WHERE id = @Id";

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
                _logger.Error(ex, "Delete anomaly_units 失敗 | Id={Id}", id);
                throw;
            }
        }

        /// <summary>檢查指定主鍵的資料是否存在。</summary>
        public bool Exists(long id)
        {
            const string sql = "SELECT 1 FROM anomaly_units WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

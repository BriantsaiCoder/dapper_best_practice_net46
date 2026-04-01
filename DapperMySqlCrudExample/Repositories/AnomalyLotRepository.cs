using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// <see cref="IAnomalyLotRepository"/> 的 Dapper 實作，對應 anomaly_lots 資料表。
    /// </summary>
    public class AnomalyLotRepository : IAnomalyLotRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 AnomalyLotRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public AnomalyLotRepository(IDbConnectionFactory factory)
        {
            _factory = RepositoryGuards.RequireFactory(factory, nameof(factory));
        }

        private const string SelectColumns = @"
            id                   AS Id,
            lots_info_id         AS LotsInfoId,
            detection_method_id  AS DetectionMethodId,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        /// <inheritdoc/>
        public IEnumerable<AnomalyLot> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lots ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLot>(sql);
        }

        /// <inheritdoc/>
        public AnomalyLot GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lots WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyLot>(sql, new { Id = id });
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyLot> GetByLotsInfoId(int lotsInfoId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lots WHERE lots_info_id = @LotsInfoId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLot>(sql, new { LotsInfoId = lotsInfoId });
        }

        /// <inheritdoc/>
        public long Insert(AnomalyLot entity, IDbTransaction transaction = null)
        {
            RepositoryGuards.RequireEntity(entity, nameof(entity));

            const string sql = @"
                INSERT INTO anomaly_lots
                    (lots_info_id, detection_method_id, spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@LotsInfoId, @DetectionMethodId, @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            return _factory.ExecuteScalar<long>(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Update(AnomalyLot entity, IDbTransaction transaction = null)
        {
            RepositoryGuards.RequireEntity(entity, nameof(entity));

            const string sql = @"
                UPDATE anomaly_lots
                SET    lots_info_id         = @LotsInfoId,
                       detection_method_id  = @DetectionMethodId,
                       spec_upper_limit     = @SpecUpperLimit,
                       spec_lower_limit     = @SpecLowerLimit,
                       spec_calc_start_time = @SpecCalcStartTime,
                       spec_calc_end_time   = @SpecCalcEndTime
                WHERE  id = @Id";

            return _factory.Execute(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM anomaly_lots WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc/>
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_lots WHERE id = @Id";
            return _factory.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM anomaly_lots";
            return _factory.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<AnomalyLot> GetPaged(int offset, int limit)
        {
            RepositoryGuards.ValidatePaging(offset, limit);

            var sql = $"SELECT {SelectColumns} FROM anomaly_lots ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLot>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

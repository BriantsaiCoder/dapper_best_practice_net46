using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// <see cref="IGoodLotRepository"/> 的 Dapper 實作，對應 good_lots 資料表。
    /// </summary>
    public class GoodLotRepository : IGoodLotRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 GoodLotRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public GoodLotRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
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
        public IEnumerable<GoodLot> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM good_lots ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<GoodLot>(sql);
        }

        /// <inheritdoc/>
        public GoodLot GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM good_lots WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<GoodLot>(sql, new { Id = id });
        }

        /// <inheritdoc/>
        public IEnumerable<GoodLot> GetByLotsInfoId(int lotsInfoId)
        {
            var sql = $"SELECT {SelectColumns} FROM good_lots WHERE lots_info_id = @LotsInfoId";
            using (var conn = _factory.Create())
                return conn.Query<GoodLot>(sql, new { LotsInfoId = lotsInfoId });
        }

        /// <inheritdoc/>
        public long Insert(GoodLot entity, IDbTransaction transaction = null)
        {
            const string sql = @"
                INSERT INTO good_lots
                    (lots_info_id, detection_method_id, spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@LotsInfoId, @DetectionMethodId, @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            return _factory.ExecuteScalar<long>(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Update(GoodLot entity, IDbTransaction transaction = null)
        {
            const string sql = @"
                UPDATE good_lots
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
            const string sql = "DELETE FROM good_lots WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc/>
        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM good_lots WHERE id = @Id";
            return _factory.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM good_lots";
            return _factory.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<GoodLot> GetPaged(int offset, int limit)
        {
            var sql = $"SELECT {SelectColumns} FROM good_lots ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<GoodLot>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

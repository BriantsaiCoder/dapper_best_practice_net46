using System.Collections.Generic;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常批號 Repository 實作</summary>
    public class AnomalyLotRepository : IAnomalyLotRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AnomalyLotRepository(IDbConnectionFactory factory)
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

        public IEnumerable<AnomalyLot> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lots ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLot>(sql);
        }

        public AnomalyLot GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lots WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<AnomalyLot>(sql, new { Id = id });
        }

        public IEnumerable<AnomalyLot> GetByLotsInfoId(int lotsInfoId)
        {
            var sql = $"SELECT {SelectColumns} FROM anomaly_lots WHERE lots_info_id = @LotsInfoId";
            using (var conn = _factory.Create())
                return conn.Query<AnomalyLot>(sql, new { LotsInfoId = lotsInfoId });
        }

        public long Insert(AnomalyLot entity)
        {
            const string sql = @"
                INSERT INTO anomaly_lots
                    (lots_info_id, detection_method_id, spec_upper_limit, spec_lower_limit,
                     spec_calc_start_time, spec_calc_end_time)
                VALUES
                    (@LotsInfoId, @DetectionMethodId, @SpecUpperLimit, @SpecLowerLimit,
                     @SpecCalcStartTime, @SpecCalcEndTime);
                SELECT LAST_INSERT_ID();";

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(AnomalyLot entity)
        {
            const string sql = @"
                UPDATE anomaly_lots
                SET    lots_info_id         = @LotsInfoId,
                       detection_method_id  = @DetectionMethodId,
                       spec_upper_limit     = @SpecUpperLimit,
                       spec_lower_limit     = @SpecLowerLimit,
                       spec_calc_start_time = @SpecCalcStartTime,
                       spec_calc_end_time   = @SpecCalcEndTime
                WHERE  id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id)
        {
            using (var conn = _factory.Create())
                return conn.Execute("DELETE FROM anomaly_lots WHERE id = @Id", new { Id = id }) > 0;
        }
    }
}

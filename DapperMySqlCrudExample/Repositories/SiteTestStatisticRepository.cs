using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// SiteTestStatisticRepository — site_test_statistics 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class SiteTestStatisticRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 SiteTestStatisticRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public SiteTestStatisticRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns = @"
            id             AS Id,
            lots_info_id   AS LotsInfoId,
            program        AS Program,
            site_id        AS SiteId,
            test_item_name AS TestItemName,
            mean_value     AS MeanValue,
            max_value      AS MaxValue,
            min_value      AS MinValue,
            std_value      AS StdValue,
            cp_value       AS CpValue,
            cpk_value      AS CpkValue,
            tester_id      AS TesterId,
            start_time     AS StartTime,
            end_time       AS EndTime,
            created_at     AS CreatedAt,
            updated_at     AS UpdatedAt";

        public IEnumerable<SiteTestStatistic> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM site_test_statistics ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<SiteTestStatistic>(sql);
        }

        public SiteTestStatistic GetById(long id)
        {
            var sql = $"SELECT {SelectColumns} FROM site_test_statistics WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<SiteTestStatistic>(sql, new { Id = id });
        }

        public IEnumerable<SiteTestStatistic> GetByLotsInfoId(int lotsInfoId)
        {
            var sql = $"SELECT {SelectColumns} FROM site_test_statistics WHERE lots_info_id = @LotsInfoId";
            using (var conn = _factory.Create())
                return conn.Query<SiteTestStatistic>(sql, new { LotsInfoId = lotsInfoId });
        }

        public IEnumerable<SiteTestStatistic> GetBySiteAndItem(uint siteId, string testItemName)
        {
            var sql = $@"
                SELECT {SelectColumns}
                FROM   site_test_statistics
                WHERE  site_id = @SiteId
                  AND  test_item_name = @TestItemName";

            using (var conn = _factory.Create())
                return conn.Query<SiteTestStatistic>(sql, new { SiteId = siteId, TestItemName = testItemName });
        }

        public SiteTestStatistic GetLatestSampleForSpecCalculation()
        {
            var sql = $@"
                SELECT {SelectColumns}
                FROM   site_test_statistics
                WHERE  mean_value IS NOT NULL
                  AND  start_time IS NOT NULL
                ORDER BY start_time DESC
                LIMIT 1";

            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<SiteTestStatistic>(sql);
        }

        public long Insert(SiteTestStatistic entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
                INSERT INTO site_test_statistics
                    (lots_info_id, program, site_id, test_item_name,
                     mean_value, max_value, min_value, std_value,
                     cp_value, cpk_value, tester_id,
                     start_time, end_time)
                VALUES
                    (@LotsInfoId, @Program, @SiteId, @TestItemName,
                     @MeanValue, @MaxValue, @MinValue, @StdValue,
                     @CpValue, @CpkValue, @TesterId,
                     @StartTime, @EndTime);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(SiteTestStatistic entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
                UPDATE site_test_statistics
                SET    lots_info_id   = @LotsInfoId,
                       program        = @Program,
                       site_id        = @SiteId,
                       test_item_name = @TestItemName,
                       mean_value     = @MeanValue,
                       max_value      = @MaxValue,
                       min_value      = @MinValue,
                       std_value      = @StdValue,
                       cp_value       = @CpValue,
                       cpk_value      = @CpkValue,
                       tester_id      = @TesterId,
                       start_time     = @StartTime,
                       end_time       = @EndTime
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM site_test_statistics WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        public bool Exists(long id)
        {
            const string sql = "SELECT COUNT(1) FROM site_test_statistics WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM site_test_statistics";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }

        public IEnumerable<SiteTestStatistic> GetPaged(int offset, int limit)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset 不可小於 0。");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit 必須大於 0。");

            var sql = $"SELECT {SelectColumns} FROM site_test_statistics ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<SiteTestStatistic>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

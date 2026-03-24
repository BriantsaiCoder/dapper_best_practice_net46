using System.Collections.Generic;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Site 測項統計值 Repository 實作</summary>
    public class SiteTestStatisticRepository : ISiteTestStatisticRepository
    {
        private readonly IDbConnectionFactory _factory;

        public SiteTestStatisticRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
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

        public long Insert(SiteTestStatistic entity)
        {
            const string sql = @"
                INSERT INTO site_test_statistics
                    (lots_info_id, program, site_id, test_item_name,
                     mean_value, max_value, min_value, std_value,
                     cp_value, cpk_value, tester_id)
                VALUES
                    (@LotsInfoId, @Program, @SiteId, @TestItemName,
                     @MeanValue, @MaxValue, @MinValue, @StdValue,
                     @CpValue, @CpkValue, @TesterId);
                SELECT LAST_INSERT_ID();";

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<long>(sql, entity);
        }

        public bool Update(SiteTestStatistic entity)
        {
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
                       tester_id      = @TesterId
                WHERE  id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(long id)
        {
            using (var conn = _factory.Create())
                return conn.Execute("DELETE FROM site_test_statistics WHERE id = @Id", new { Id = id }) > 0;
        }
    }
}

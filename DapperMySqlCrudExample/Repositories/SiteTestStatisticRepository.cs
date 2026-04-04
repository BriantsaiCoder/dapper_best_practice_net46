using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Models.QueryModels;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// SiteTestStatisticRepository — site_test_statistics 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class SiteTestStatisticRepository
    {
        private readonly DbConnectionFactory _factory;

        private const int PreferredHistoryCount = 30;

        /// <summary>建立 SiteTestStatisticRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public SiteTestStatisticRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
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

        public SiteTestStatistic GetById(long id)
        {
            const string sql =
                "SELECT " + SelectColumns + " FROM site_test_statistics WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<SiteTestStatistic>(sql, new { Id = id });
        }

        public IEnumerable<SiteTestStatistic> GetByLotsInfoId(int lotsInfoId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM site_test_statistics WHERE lots_info_id = @LotsInfoId";
            using (var conn = _factory.Create())
                return conn.Query<SiteTestStatistic>(sql, new { LotsInfoId = lotsInfoId });
        }

        /// <summary>
        /// 從最新有效樣本中取得 SITE_MEAN 規格計算所需的三個引數。
        /// 僅 SELECT 必要欄位，減少資料傳輸量。
        /// </summary>
        /// <remarks>
        /// 索引命中策略：ORDER BY start_time DESC 可利用 idx_start_time 索引排序，
        /// 搭配 LIMIT 1 快速定位最新一筆。
        /// </remarks>
        public SiteMeanCalcParams GetCalcParamsFromLatestSample()
        {
            const string sql =
                @"SELECT program        AS ProgramName,
                         site_id        AS SiteId,
                         test_item_name AS TestItemName
                  FROM   site_test_statistics
                  WHERE  mean_value IS NOT NULL
                    AND  start_time IS NOT NULL
                  ORDER BY start_time DESC
                  LIMIT 1";

            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<SiteMeanCalcParams>(sql);
        }

        /// <summary>
        /// 取最新 <see cref="PreferredHistoryCount"/> 筆有效資料用於 SITE_MEAN 規格計算。
        /// 若近期資料充足，結果自然已全為近期；不足時則涵蓋更早的歷史，
        /// 與分兩次查詢結果相同但只需一次 DB round-trip。
        /// 支援外部交易參與，遵循標準 Repository 模式。
        /// </summary>
        public IReadOnlyList<SiteMeanRow> QuerySiteMeanRows(
            string programName,
            uint siteId,
            string testItemName,
            IDbTransaction transaction = null
        )
        {
            if (string.IsNullOrWhiteSpace(programName))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(programName));
            if (string.IsNullOrWhiteSpace(testItemName))
                throw new ArgumentException(
                    "參數不可為 null、空字串或空白。",
                    nameof(testItemName)
                );

            var p = new
            {
                ProgramName = programName,
                SiteId = siteId,
                TestItemName = testItemName,
                Limit = PreferredHistoryCount,
            };

            const string sql =
                @"SELECT mean_value AS MeanValue, start_time AS StartTime
                  FROM   site_test_statistics
                  WHERE  program        = @ProgramName
                    AND  site_id        = @SiteId
                    AND  test_item_name = @TestItemName
                    AND  start_time    IS NOT NULL
                    AND  mean_value    IS NOT NULL
                  ORDER BY start_time DESC
                  LIMIT @Limit";

            if (transaction != null)
                return transaction.Connection.Query<SiteMeanRow>(sql, p, transaction).ToList();

            using (var conn = _factory.Create())
                return conn.Query<SiteMeanRow>(sql, p).ToList();
        }

        public long Insert(SiteTestStatistic entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
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
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
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
            const string sql = "SELECT 1 FROM site_test_statistics WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }
    }
}

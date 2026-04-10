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
    /// 偵測規格（detection_specs）資料表的 Repository 實作。
    /// 提供 detection_specs 的基本寫入與具業務意義的查詢。
    /// </summary>
    public sealed class DetectionSpecRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 <see cref="DetectionSpecRepository"/> 實例。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> 為 null。</exception>
        public DetectionSpecRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
            id                   AS Id,
            program              AS Program,
            test_item_name       AS TestItemName,
            site_id              AS SiteId,
            detection_method_id  AS DetectionMethodId,
            spec_upper_limit     AS SpecUpperLimit,
            spec_lower_limit     AS SpecLowerLimit,
            spec_calc_start_time AS SpecCalcStartTime,
            spec_calc_end_time   AS SpecCalcEndTime,
            spec_calc_mean       AS SpecCalcMean,
            spec_calc_std        AS SpecCalcStd,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public DetectionSpec GetById(long id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM detection_specs WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<DetectionSpec>(sql, new { Id = id });
            }
        }

        /// <summary>依 program 與 detection_method_id 查詢多筆資料。</summary>
        /// <remarks>
        /// 業務上同一組合的 spec 筆數有限，LIMIT 1000 為防禦性上限。
        /// 若未來筆數可能大幅增長，請改為分頁查詢。
        /// </remarks>
        public IReadOnlyList<DetectionSpec> GetByProgramAndMethodId(
            string program,
            byte detectionMethodId
        )
        {
            if (string.IsNullOrWhiteSpace(program))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(program));

            const string sql =
                "SELECT "
                + SelectColumns
                + @"
                   FROM   detection_specs
                   WHERE  program             = @Program
                     AND  detection_method_id = @DetectionMethodId
                   ORDER BY id
                   LIMIT 1000";

            using (var conn = _factory.Create())
            {
                return conn.Query<DetectionSpec>(
                        sql,
                        new { Program = program, DetectionMethodId = detectionMethodId }
                    )
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
        public long Insert(DetectionSpec entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string insertSql =
                @"INSERT INTO detection_specs
                      (program, test_item_name, site_id, detection_method_id,
                       spec_upper_limit, spec_lower_limit,
                       spec_calc_start_time, spec_calc_end_time,
                       spec_calc_mean, spec_calc_std)
                  VALUES
                      (@Program, @TestItemName, @SiteId, @DetectionMethodId,
                       @SpecUpperLimit, @SpecLowerLimit,
                       @SpecCalcStartTime, @SpecCalcEndTime,
                       @SpecCalcMean, @SpecCalcStd)";

            const string identitySql = "SELECT LAST_INSERT_ID()";

            if (transaction != null)
            {
                transaction.Connection.Execute(insertSql, entity, transaction);
                return transaction.Connection.ExecuteScalar<long>(identitySql, transaction: transaction);
            }

            using (var conn = _factory.Create())
            {
                conn.Execute(insertSql, entity);
                return conn.ExecuteScalar<long>(identitySql);
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(DetectionSpec entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"UPDATE detection_specs
                  SET    program              = @Program,
                         test_item_name       = @TestItemName,
                         site_id              = @SiteId,
                         detection_method_id  = @DetectionMethodId,
                         spec_upper_limit     = @SpecUpperLimit,
                         spec_lower_limit     = @SpecLowerLimit,
                         spec_calc_start_time = @SpecCalcStartTime,
                         spec_calc_end_time   = @SpecCalcEndTime,
                         spec_calc_mean       = @SpecCalcMean,
                         spec_calc_std        = @SpecCalcStd
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
            const string sql = "DELETE FROM detection_specs WHERE id = @Id";

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
            const string sql = "SELECT 1 FROM detection_specs WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

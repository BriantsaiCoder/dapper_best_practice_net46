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
    /// DetectionMethodRepository — detection_methods 資料表的 Dapper 資料存取。
    /// 主鍵 id 為 TINYINT UNSIGNED，故 PK 型別為 <see cref="byte"/>。
    /// </summary>
    public sealed class DetectionMethodRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 DetectionMethodRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public DetectionMethodRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
            id             AS Id,
            method_key     AS MethodKey,
            method_name    AS MethodName,
            has_test_item  AS HasTestItem,
            has_unit_level AS HasUnitLevel,
            created_at     AS CreatedAt,
            updated_at     AS UpdatedAt";

        /// <summary>
        /// 取得全部偵測方法。
        /// detection_methods 為低筆數主檔表，保留此方法作為 lookup 與教學用途。
        /// </summary>
        public IReadOnlyList<DetectionMethod> GetAll()
        {
            const string sql = "SELECT " + SelectColumns + " FROM detection_methods ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<DetectionMethod>(sql).ToList();
        }

        /// <summary>依主鍵查詢單筆資料。</summary>
        public DetectionMethod GetById(byte id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM detection_methods WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { Id = id });
        }

        /// <summary>依 method_key 查詢單筆偵測方法。</summary>
        public DetectionMethod GetByKey(string methodKey)
        {
            if (string.IsNullOrWhiteSpace(methodKey))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(methodKey));

            const string sql =
                "SELECT " + SelectColumns + " FROM detection_methods WHERE method_key = @MethodKey";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(
                    sql,
                    new { MethodKey = methodKey }
                );
        }

        /// <summary>
        /// 依 method_key 查詢主鍵 id。支援外部交易參與。
        /// </summary>
        /// <remarks>
        /// 通常讀取方法不接受 IDbTransaction，但本方法需在 RepeatableRead 交易中
        /// 使用（如 DetectionSpecService.ComputeAndInsertSiteMeanSpec），
        /// 以確保 SITE_MEAN 計算流程的讀取一致性。
        /// </remarks>
        public byte? GetIdByKey(string methodKey, IDbTransaction transaction = null)
        {
            if (string.IsNullOrWhiteSpace(methodKey))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(methodKey));

            const string sql = "SELECT id FROM detection_methods WHERE method_key = @MethodKey";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<byte?>(
                    sql,
                    new { MethodKey = methodKey },
                    transaction
                );

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<byte?>(sql, new { MethodKey = methodKey });
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        public byte Insert(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                INSERT INTO detection_methods
                    (method_key, method_name, has_test_item, has_unit_level)
                VALUES
                    (@MethodKey, @MethodName, @HasTestItem, @HasUnitLevel);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<byte>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<byte>(sql, entity);
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE detection_methods
                SET    method_key     = @MethodKey,
                       method_name    = @MethodName,
                       has_test_item  = @HasTestItem,
                       has_unit_level = @HasUnitLevel
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        /// <summary>依主鍵刪除一筆資料。</summary>
        public bool Delete(byte id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_methods WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        /// <summary>檢查指定主鍵的資料是否存在。</summary>
        public bool Exists(byte id)
        {
            const string sql = "SELECT 1 FROM detection_methods WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }

        /// <remarks>
        /// ⚠ 注意：COUNT(1) 在大量資料表上可能導致全表掃描，
        /// 僅適合資料量可控的場景或管理用途。
        /// </remarks>
        /// <summary>取得資料總筆數。僅適用低筆數 lookup table。</summary>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM detection_methods";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }
    }
}

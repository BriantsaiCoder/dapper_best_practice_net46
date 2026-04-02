using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// DetectionMethodRepository — detection_methods 資料表的 Dapper 資料存取。
    /// 主鍵 id 為 TINYINT UNSIGNED，故 PK 型別為 <see cref="byte"/>。
    /// </summary>
    public class DetectionMethodRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 DetectionMethodRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public DetectionMethodRepository(IDbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns = @"
            id             AS Id,
            method_code    AS MethodCode,
            method_name    AS MethodName,
            has_test_item  AS HasTestItem,
            has_unit_level AS HasUnitLevel,
            created_at     AS CreatedAt,
            updated_at     AS UpdatedAt";

        /// <inheritdoc/>
        public IEnumerable<DetectionMethod> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<DetectionMethod>(sql);
        }

        /// <inheritdoc/>
        public DetectionMethod GetById(byte id)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { Id = id });
        }

        /// <inheritdoc/>
        public DetectionMethod GetByCode(string methodCode)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods WHERE method_code = @MethodCode";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { MethodCode = methodCode });
        }

        /// <inheritdoc/>
        public byte Insert(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
                INSERT INTO detection_methods
                    (method_code, method_name, has_test_item, has_unit_level)
                VALUES
                    (@MethodCode, @MethodName, @HasTestItem, @HasUnitLevel);
                SELECT LAST_INSERT_ID();";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<byte>(sql, entity, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<byte>(sql, entity);
        }

        /// <inheritdoc/>
        public bool Update(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
                UPDATE detection_methods
                SET    method_code    = @MethodCode,
                       method_name    = @MethodName,
                       has_test_item  = @HasTestItem,
                       has_unit_level = @HasUnitLevel
                WHERE  id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        /// <inheritdoc/>
        public bool Delete(byte id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_methods WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public bool Exists(byte id)
        {
            const string sql = "SELECT COUNT(1) FROM detection_methods WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM detection_methods";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<DetectionMethod> GetPaged(int offset, int limit)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset 不可小於 0。");
            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "limit 必須大於 0。");

            var sql = $"SELECT {SelectColumns} FROM detection_methods ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<DetectionMethod>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

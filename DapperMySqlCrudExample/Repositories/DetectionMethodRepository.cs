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
    public sealed class DetectionMethodRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 DetectionMethodRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public DetectionMethodRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns = @"
            id             AS Id,
            method_key     AS MethodKey,
            method_name    AS MethodName,
            has_test_item  AS HasTestItem,
            has_unit_level AS HasUnitLevel,
            created_at     AS CreatedAt,
            updated_at     AS UpdatedAt";
        public DetectionMethod GetById(byte id)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { Id = id });
        }
        public DetectionMethod GetByKey(string methodKey)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods WHERE method_key = @MethodKey";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { MethodKey = methodKey });
        }

        /// <summary>
        /// 依 method_key 查詢主鍵 id。支援外部交易參與。
        /// </summary>
        public byte? GetIdByKey(string methodKey, IDbTransaction transaction = null)
        {
            const string sql = "SELECT id FROM detection_methods WHERE method_key = @MethodKey";

            if (transaction != null)
                return transaction.Connection.ExecuteScalar<byte?>(sql, new { MethodKey = methodKey }, transaction);

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<byte?>(sql, new { MethodKey = methodKey });
        }

        public byte Insert(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
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
        public bool Update(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            const string sql = @"
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
        public bool Delete(byte id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_methods WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }
        public bool Exists(byte id)
        {
            const string sql = "SELECT 1 FROM detection_methods WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM detection_methods";
            using (var conn = _factory.Create())
                return conn.ExecuteScalar<int>(sql);
        }
    }
}

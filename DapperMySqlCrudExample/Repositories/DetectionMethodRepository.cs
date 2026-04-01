using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>
    /// <see cref="IDetectionMethodRepository"/> 的 Dapper 實作，對應 detection_methods 資料表。
    /// 主鍵 id 為 TINYINT UNSIGNED，故 PK 型別為 <see cref="byte"/>。
    /// </summary>
    public class DetectionMethodRepository : IDetectionMethodRepository
    {
        private readonly IDbConnectionFactory _factory;

        /// <summary>建立 DetectionMethodRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public DetectionMethodRepository(IDbConnectionFactory factory)
        {
            _factory = RepositoryGuards.RequireFactory(factory, nameof(factory));
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
            RepositoryGuards.RequireEntity(entity, nameof(entity));

            const string sql = @"
                INSERT INTO detection_methods
                    (method_code, method_name, has_test_item, has_unit_level)
                VALUES
                    (@MethodCode, @MethodName, @HasTestItem, @HasUnitLevel);
                SELECT LAST_INSERT_ID();";

            return _factory.ExecuteScalar<byte>(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Update(DetectionMethod entity, IDbTransaction transaction = null)
        {
            RepositoryGuards.RequireEntity(entity, nameof(entity));

            const string sql = @"
                UPDATE detection_methods
                SET    method_code    = @MethodCode,
                       method_name    = @MethodName,
                       has_test_item  = @HasTestItem,
                       has_unit_level = @HasUnitLevel
                WHERE  id = @Id";

            return _factory.Execute(sql, entity, transaction);
        }

        /// <inheritdoc/>
        public bool Delete(byte id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_methods WHERE id = @Id";
            return _factory.Execute(sql, new { Id = id }, transaction);
        }

        /// <inheritdoc/>
        public bool Exists(byte id)
        {
            const string sql = "SELECT COUNT(1) FROM detection_methods WHERE id = @Id";
            return _factory.ExecuteScalar<int>(sql, new { Id = id }) > 0;
        }

        /// <inheritdoc/>
        public int GetCount()
        {
            const string sql = "SELECT COUNT(1) FROM detection_methods";
            return _factory.ExecuteScalar<int>(sql);
        }

        /// <inheritdoc/>
        public IEnumerable<DetectionMethod> GetPaged(int offset, int limit)
        {
            RepositoryGuards.ValidatePaging(offset, limit);

            var sql = $"SELECT {SelectColumns} FROM detection_methods ORDER BY id LIMIT @Offset, @Limit";
            using (var conn = _factory.Create())
                return conn.Query<DetectionMethod>(sql, new { Offset = offset, Limit = limit });
        }
    }
}

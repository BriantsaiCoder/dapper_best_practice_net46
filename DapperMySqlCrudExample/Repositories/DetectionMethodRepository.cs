using System.Collections.Generic;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>偵測方法 Repository 實作</summary>
    public class DetectionMethodRepository : IDetectionMethodRepository
    {
        private readonly IDbConnectionFactory _factory;

        public DetectionMethodRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        private const string SelectColumns = @"
            id             AS Id,
            method_code    AS MethodCode,
            method_name    AS MethodName,
            has_test_item  AS HasTestItem,
            has_unit_level AS HasUnitLevel,
            created_at     AS CreatedAt,
            updated_at     AS UpdatedAt";

        public IEnumerable<DetectionMethod> GetAll()
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods ORDER BY id";
            using (var conn = _factory.Create())
                return conn.Query<DetectionMethod>(sql);
        }

        public DetectionMethod GetById(byte id)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods WHERE id = @Id";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { Id = id });
        }

        public DetectionMethod GetByCode(string methodCode)
        {
            var sql = $"SELECT {SelectColumns} FROM detection_methods WHERE method_code = @MethodCode";
            using (var conn = _factory.Create())
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { MethodCode = methodCode });
        }

        public byte Insert(DetectionMethod entity)
        {
            const string sql = @"
                INSERT INTO detection_methods
                    (method_code, method_name, has_test_item, has_unit_level)
                VALUES
                    (@MethodCode, @MethodName, @HasTestItem, @HasUnitLevel);
                SELECT LAST_INSERT_ID();";

            using (var conn = _factory.Create())
                return conn.ExecuteScalar<byte>(sql, entity);
        }

        public bool Update(DetectionMethod entity)
        {
            const string sql = @"
                UPDATE detection_methods
                SET    method_code    = @MethodCode,
                       method_name    = @MethodName,
                       has_test_item  = @HasTestItem,
                       has_unit_level = @HasUnitLevel
                WHERE  id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, entity) > 0;
        }

        public bool Delete(byte id)
        {
            const string sql = "DELETE FROM detection_methods WHERE id = @Id";

            using (var conn = _factory.Create())
                return conn.Execute(sql, new { Id = id }) > 0;
        }
    }
}

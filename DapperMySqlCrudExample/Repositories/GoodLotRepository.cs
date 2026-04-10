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
    /// GoodLotRepository — good_lots 資料表的 Dapper 資料存取。
    /// </summary>
    public sealed class GoodLotRepository
    {
        private readonly DbConnectionFactory _factory;

        /// <summary>建立 GoodLotRepository 實體。</summary>
        /// <param name="factory">資料庫連線工廠。</param>
        public GoodLotRepository(DbConnectionFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private const string SelectColumns =
            @"
            id                   AS Id,
            lots_info_id         AS LotsInfoId,
            detection_method_id  AS DetectionMethodId,
            created_at           AS CreatedAt,
            updated_at           AS UpdatedAt";

        /// <summary>依主鍵查詢單筆資料。</summary>
        public GoodLot GetById(long id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM good_lots WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<GoodLot>(sql, new { Id = id });
            }
        }

        /// <summary>依 lots_info_id 查詢多筆資料。</summary>
        public IReadOnlyList<GoodLot> GetByLotsInfoId(int lotsInfoId)
        {
            const string sql =
                "SELECT "
                + SelectColumns
                + " FROM good_lots WHERE lots_info_id = @LotsInfoId ORDER BY id";
            using (var conn = _factory.Create())
            {
                return conn.Query<GoodLot>(sql, new { LotsInfoId = lotsInfoId }).ToList();
            }
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        public long Insert(GoodLot entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string insertSql =
                @"
                INSERT INTO good_lots
                    (lots_info_id, detection_method_id)
                VALUES
                    (@LotsInfoId, @DetectionMethodId);";
            const string lastInsertIdSql = "SELECT LAST_INSERT_ID();";

            if (transaction != null)
            {
                transaction.Connection.Execute(insertSql, entity, transaction);
                return transaction.Connection.ExecuteScalar<long>(lastInsertIdSql, transaction: transaction);
            }

            using (var conn = _factory.Create())
            {
                conn.Execute(insertSql, entity);
                return conn.ExecuteScalar<long>(lastInsertIdSql);
            }
        }

        /// <summary>更新一筆資料。</summary>
        public bool Update(GoodLot entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            const string sql =
                @"
                UPDATE good_lots
                SET    lots_info_id         = @LotsInfoId,
                       detection_method_id  = @DetectionMethodId
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
            const string sql = "DELETE FROM good_lots WHERE id = @Id";

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
            const string sql = "SELECT 1 FROM good_lots WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
        }
    }
}

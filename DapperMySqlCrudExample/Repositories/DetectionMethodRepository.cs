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

        // 【新手導讀】SelectColumns 是本專案所有 Repository 共用的設計模式：
        // Dapper 的自動映射機制會將 SQL 查詢結果的欄位名稱與 C# 類別的屬性名稱做比對（大小寫不敏感）。
        // 因為 MySQL 慣用 snake_case（如 method_key），而 C# 慣用 PascalCase（如 MethodKey），
        // 所以透過 SQL 的 AS 別名將 snake_case 轉為 PascalCase，讓 Dapper 能正確映射。
        // 這取代了 Entity Framework 中 [Column("method_key")] 這類 Attribute 的角色。
        // 將欄位清單定義為常數，可在多個查詢方法間共用，避免重複且易於維護。
        private const string SelectColumns =
            @"
            id             AS Id,
            method_key     AS MethodKey,
            method_name    AS MethodName,
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
            {
                // 【新手導讀】Dapper 的 Query<T>() 回傳 IEnumerable<T>，預設是延遲執行（lazy evaluation）。
                // 必須在 using 區塊內呼叫 .ToList() 強制「具體化」所有資料，
                // 否則離開 using 後連線已關閉，再嘗試讀取會拋出 ObjectDisposedException。
                return conn.Query<DetectionMethod>(sql).ToList();
            }
        }

        /// <summary>依主鍵查詢單筆資料。</summary>
        public DetectionMethod GetById(byte id)
        {
            const string sql = "SELECT " + SelectColumns + " FROM detection_methods WHERE id = @Id";
            using (var conn = _factory.Create())
            {
                // 【新手導讀】QueryFirstOrDefault<T>() 只取結果集的第一筆就停止讀取，效能優於 Query().FirstOrDefault()。
                // 找不到資料時：參考型別回傳 null，值型別回傳 default（如 int 回傳 0）。
                //
                // new { Id = id } 是 C# 匿名物件，Dapper 會自動將其屬性名稱對應到 SQL 中的 @參數：
                //   屬性 Id → 對應 SQL 的 @Id（大小寫不敏感）
                // 這是參數化查詢，可有效防止 SQL Injection 攻擊。
                return conn.QueryFirstOrDefault<DetectionMethod>(sql, new { Id = id });
            }
        }

        /// <summary>依 method_key 查詢單筆偵測方法。</summary>
        public DetectionMethod GetByKey(string methodKey)
        {
            if (string.IsNullOrWhiteSpace(methodKey))
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(methodKey));

            const string sql =
                "SELECT " + SelectColumns + " FROM detection_methods WHERE method_key = @MethodKey";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<DetectionMethod>(
                    sql,
                    new { MethodKey = methodKey }
                );
            }
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
            {
                return conn.ExecuteScalar<byte?>(sql, new { MethodKey = methodKey });
            }
        }

        /// <summary>新增一筆資料並回傳自動遞增主鍵。</summary>
        /// <remarks>
        /// 【新手導讀】交易參數模式：transaction 預設為 null，讓同一方法可在有/無交易兩種情境下使用。
        /// 有交易時透過 transaction.Connection 取得該交易綁定的連線，確保所有操作共用同一連線與交易。
        /// 無交易時自行建立新連線（走 using 區塊）。此模式在本專案所有 Repository 的寫入方法中通用。
        /// </remarks>
        public byte Insert(DetectionMethod entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // 【新手導讀】INSERT 後接 SELECT LAST_INSERT_ID() 取得 MySQL 自動遞增的主鍵值。
            // ExecuteScalar<T>() 會執行 SQL 並回傳結果集第一列第一欄的值，正好取得新主鍵。
            const string sql =
                @"
                INSERT INTO detection_methods
                    (method_key, method_name)
                VALUES
                    (@MethodKey, @MethodName);
                SELECT LAST_INSERT_ID();";

            // 【新手導讀】直接傳入 entity 物件作為參數時，Dapper 會自動將物件的所有公開屬性
            // 對應到 SQL 中的 @參數（如 entity.MethodKey → @MethodKey），不需逐一指定。
            if (transaction != null)
                return transaction.Connection.ExecuteScalar<byte>(sql, entity, transaction);

            using (var conn = _factory.Create())
            {
                return conn.ExecuteScalar<byte>(sql, entity);
            }
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
                       method_name    = @MethodName
                WHERE  id = @Id";

            // 【新手導讀】Execute() 回傳受影響的行數（affected rows）。
            // > 0 表示確實有更新到資料；若 WHERE 條件不符合任何列則回傳 0。
            if (transaction != null)
                return transaction.Connection.Execute(sql, entity, transaction) > 0;

            using (var conn = _factory.Create())
            {
                return conn.Execute(sql, entity) > 0;
            }
        }

        /// <summary>依主鍵刪除一筆資料。</summary>
        public bool Delete(byte id, IDbTransaction transaction = null)
        {
            const string sql = "DELETE FROM detection_methods WHERE id = @Id";

            if (transaction != null)
                return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

            using (var conn = _factory.Create())
            {
                return conn.Execute(sql, new { Id = id }) > 0;
            }
        }

        /// <summary>檢查指定主鍵的資料是否存在。</summary>
        public bool Exists(byte id)
        {
            // 【新手導讀】存在性檢查的高效技巧：SELECT 1 只回傳常數，不讀取實際欄位資料。
            // 用 int?（可空型別）接收結果：有資料時 HasValue=true，無資料時為 null → HasValue=false。
            // 比 SELECT COUNT(1) 更高效，因為找到第一筆就停止掃描。
            const string sql = "SELECT 1 FROM detection_methods WHERE id = @Id LIMIT 1";
            using (var conn = _factory.Create())
            {
                return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
            }
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
            {
                return conn.ExecuteScalar<int>(sql);
            }
        }
    }
}

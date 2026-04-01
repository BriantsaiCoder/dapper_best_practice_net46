using System.Data;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>
    /// 資料庫連線工廠介面。
    /// 提供資料存取層取得連線的一致入口，並支援在不同環境或測試情境中替換實作。
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>建立並開啟一條資料庫連線，呼叫端須自行以 using 管理。</summary>
        IDbConnection Create();
    }
}

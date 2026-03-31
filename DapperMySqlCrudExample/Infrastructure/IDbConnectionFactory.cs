using System.Data;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>資料庫連線工廠介面，方便單元測試時替換為假連線。</summary>
    public interface IDbConnectionFactory
    {
        /// <summary>建立並開啟一條資料庫連線，呼叫端須自行以 using 管理。</summary>
        IDbConnection Create();
    }
}

using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>
    /// 資料庫連線工廠。
    /// 每次呼叫 Create() 均回傳新的、已開啟的連線物件，
    /// 呼叫端需自行以 using 區塊管理連線的生命週期。
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory()
        {
            _connectionString = ConfigurationManager
                .ConnectionStrings["DefaultConnection"]
                .ConnectionString;
        }

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc/>
        public IDbConnection Create()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}

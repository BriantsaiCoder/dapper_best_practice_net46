using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>
    /// 資料庫連線工廠。
    /// 每次呼叫 Create() 均回傳新的、已開啟的連線物件，
    /// 呼叫端需自行以 using 區塊管理連線的生命週期。
    /// 支援交易模式，透過 BeginTransaction() 開啟交易。
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory()
        {
            var entry = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
                throw new InvalidOperationException(
                    "App.config 中找不到連線字串 'DefaultConnection'，請確認設定正確。");

            _connectionString = entry.ConnectionString;
        }

        public DbConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "連線字串不可為 null 或空白。");

            _connectionString = connectionString;
        }

        /// <inheritdoc/>
        public IDbConnection Create()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        /// <inheritdoc/>
        public IDbTransaction BeginTransaction()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn.BeginTransaction();
        }

        /// <inheritdoc/>
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn.BeginTransaction(isolationLevel);
        }
    }
}

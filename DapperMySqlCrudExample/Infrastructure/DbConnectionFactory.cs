using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using NLog;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>
    /// 資料庫連線工廠。
    /// 每次呼叫 Create() 均回傳新的、已開啟的連線物件，
    /// 呼叫端需自行以 using 區塊管理連線的生命週期。
    /// 需要交易時，請於外部以 using (var conn = Create()) using (var tx = conn.BeginTransaction()) 建立。
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private const string EnvVarName = "MYSQL_CONNECTION_STRING";
        private readonly string _connectionString;

        public DbConnectionFactory()
        {
            // 優先讀取環境變數，適用於生產環境避免將密碼寫入設定檔
            var envConnStr = Environment.GetEnvironmentVariable(EnvVarName);
            if (!string.IsNullOrWhiteSpace(envConnStr))
            {
                _connectionString = envConnStr;
                return;
            }

            var entry = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
                throw new InvalidOperationException(
                    $"找不到連線字串：環境變數 '{EnvVarName}' 未設定，且 App.config 中無 'DefaultConnection'。"
                );

            _connectionString = entry.ConnectionString;
        }

        public DbConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(
                    nameof(connectionString),
                    "連線字串不可為 null 或空白。"
                );

            _connectionString = connectionString;
        }

        /// <inheritdoc/>
        public IDbConnection Create()
        {
            var conn = new MySqlConnection(_connectionString);
            try
            {
                conn.Open();
                return conn;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "MySQL 連線開啟失敗");
                conn.Dispose();
                throw;
            }
        }

        public IDbTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            var conn = Create();
            try
            {
                return conn.BeginTransaction(isolationLevel);
            }
            catch
            {
                conn.Dispose();
                throw;
            }
        }
    }
}

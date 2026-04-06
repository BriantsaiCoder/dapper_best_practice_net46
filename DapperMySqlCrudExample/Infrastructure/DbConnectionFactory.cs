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
    /// <remarks>
    /// 【新手導讀】Dapper 是輕量級 micro-ORM，不像 Entity Framework 會自動管理連線生命週期。
    /// 使用 Dapper 時，開發者必須自行透過 using 區塊確保連線在使用完畢後歸還連線池。
    /// 本工廠類別將「建立連線」的邏輯集中管理，所有 Repository 皆透過注入此工廠取得連線。
    /// </remarks>
    public sealed class DbConnectionFactory
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
                _logger.Info("連線字串來源：環境變數 {EnvVar}", EnvVarName);
                return;
            }

            var entry = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ConnectionString))
                throw new InvalidOperationException(
                    $"找不到連線字串：環境變數 '{EnvVarName}' 未設定，且 App.config 中無 'DefaultConnection'。"
                );

            _connectionString = entry.ConnectionString;
            _logger.Info("連線字串來源：App.config DefaultConnection");
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

        /// <summary>建立並開啟一條資料庫連線，呼叫端須自行以 using 管理。</summary>
        /// <remarks>
        /// 【新手導讀】回傳型別為 IDbConnection（介面）而非 MySqlConnection（實作類別），
        /// 這是面向介面的設計，讓上層程式碼不綁死特定資料庫驅動。
        /// </remarks>
        public IDbConnection Create()
        {
            // 【新手導讀】雖然每次都 new MySqlConnection，但 MySql.Data 驅動內建連線池機制，
            // 實際上會重用已建立的 TCP 連線，不會每次都重新建立網路連線，效能無虞。
            var conn = new MySqlConnection(_connectionString);
            try
            {
                conn.Open();
                return conn;
            }
            catch (Exception ex)
            {
                // 【新手導讀】Open() 失敗時必須手動 Dispose()，因為此時尚未回傳給呼叫端，
                // 呼叫端的 using 區塊無法接手管理，若不 Dispose 會造成連線洩漏。
                _logger.Warn(ex, "MySQL 連線開啟失敗");
                conn.Dispose();
                throw;
            }
        }
    }
}

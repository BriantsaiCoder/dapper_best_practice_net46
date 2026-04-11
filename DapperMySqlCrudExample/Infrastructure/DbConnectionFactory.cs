using System;
using System.Configuration;
using System.Data;
#if NETFRAMEWORK
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif
using NLog;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>
    /// 資料庫連線工廠。
    /// 每次呼叫 Create() 均回傳新的、尚未開啟的連線物件，
    /// 呼叫端需自行以 using 區塊管理連線的生命週期。
    /// 需要交易時，請於外部以 using (var conn = Create()) { conn.Open(); using (var tx = conn.BeginTransaction()) ... } 建立。
    /// </summary>
    /// <remarks>
    /// 【新手導讀】Dapper 是輕量級 micro-ORM，不像 Entity Framework 會自動管理連線生命週期。
    /// 使用 Dapper 時，開發者必須自行透過 using 區塊確保連線在使用完畢後歸還連線池。
    /// 本工廠類別將「建立連線」的邏輯集中管理，所有 Repository 皆透過注入此工廠取得連線。
    ///
    /// 【Dapper 連線管理機制】
    /// Dapper 的 Query、Execute、ExecuteScalar 等方法內建自動開關連線的邏輯：
    /// - 傳入未開啟的連線 → Dapper 自動 Open、執行 SQL、自動 Close（連線歸還連線池的時間最短）
    /// - 傳入已開啟的連線 → Dapper 直接用，不會自動 Close（由呼叫端的 using 負責）
    /// 因此 Create() 回傳未開啟的連線，讓 Dapper 在每次 Query/Execute 時自動管理，
    /// 連線僅在 SQL 執行期間被佔用，持有時間最短。
    /// 唯一例外是需要交易時，必須在 BeginTransaction() 前手動呼叫 conn.Open()。
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

        /// <summary>建立一條資料庫連線（尚未開啟），呼叫端須自行以 using 管理。</summary>
        /// <remarks>
        /// 【新手導讀】回傳型別為 IDbConnection（介面）而非 MySqlConnection（實作類別），
        /// 這是面向介面的設計，讓上層程式碼不綁死特定資料庫驅動。
        ///
        /// 連線回傳時尚未開啟，Dapper 的 Query/Execute 等方法會自動 Open → 執行 → Close，
        /// 連線僅在 SQL 執行期間被佔用。
        /// 若需要交易，請在 BeginTransaction() 前手動呼叫 conn.Open()。
        /// </remarks>
        public IDbConnection Create()
        {
            // 【新手導讀】雖然每次都 new MySqlConnection，但 MySQL 驅動（MySql.Data / MySqlConnector）
            // 內建連線池機制，實際上會重用已建立的 TCP 連線，不會每次都重新建立網路連線，效能無虞。
            return new MySqlConnection(_connectionString);
        }
    }
}

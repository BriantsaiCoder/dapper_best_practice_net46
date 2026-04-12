using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
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
            {
                throw new InvalidOperationException(
                    $"找不到連線字串：環境變數 '{EnvVarName}' 未設定，且 App.config 中無 'DefaultConnection'。"
                );
            }

            _connectionString = entry.ConnectionString;
            _logger.Info("連線字串來源：App.config DefaultConnection");
        }

        public DbConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(
                    nameof(connectionString),
                    "連線字串不可為 null 或空白。"
                );
            }

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
            // 【新手導讀】雖然每次都 new MySqlConnection，但 MySql.Data 驅動內建連線池機制，
            // 實際上會重用已建立的 TCP 連線，不會每次都重新建立網路連線，效能無虞。
            var conn = new MySqlConnection(_connectionString);
            conn.StateChange += OnConnectionStateChange;
            return conn;
        }

        /// <summary>
        /// 連線開啟後強制指定 MySql.Data 6.10.9 認識的排序規則，修正 MySQL 8.0 相容性問題。
        /// </summary>
        /// <remarks>
        /// MySQL 8.0 將 utf8mb4 的預設排序規則改為 utf8mb4_0900_ai_ci（collation ID=255），
        /// 但 MySql.Data 6.10.9 的內部字元集對照表不包含此 ID。
        /// 當驅動處理結果集欄位中繼資料時會在 MySqlField.SetFieldEncoding() 中拋出
        /// KeyNotFoundException。
        /// 連線開啟後立即以 SET NAMES 強制使用 utf8mb4_unicode_ci（ID=224），
        /// 即可在不升級驅動的前提下正常連線 MySQL 8.0。
        ///
        /// 相容性：utf8mb4_unicode_ci 自 MySQL 5.5.3 起可用，與本專案 Schema 所需的最低版本一致。
        /// 在 MySQL 5.x 上此命令無害（5.x 不存在 collation ID=255 問題），僅多一趟往返。
        /// 若 SET NAMES 因不明原因失敗（例如極舊版本不支援 utf8mb4），僅記錄警告不中斷連線，
        /// 避免影響 MySQL 5.x 環境的正常運作。
        /// </remarks>
        private static void OnConnectionStateChange(object sender, StateChangeEventArgs e)
        {
            if (e.CurrentState == ConnectionState.Open)
            {
                try
                {
                    using (var cmd = ((MySqlConnection)sender).CreateCommand())
                    {
                        cmd.CommandText = "SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    // MySQL 5.5.3 以下不支援 utf8mb4，但本專案 Schema 已要求 utf8mb4，
                    // 理論上不會進入此分支；僅作為防禦性處理，避免 SET NAMES 失敗導致連線中斷。
                    _logger.Warn(
                        ex,
                        "SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci 失敗，可能為 MySQL 5.5.3 以下版本。"
                            + "若連線 MySQL 8.0+ 且持續出現此警告，請確認伺服器版本與字元集設定。"
                    );
                }
            }
        }
    }
}

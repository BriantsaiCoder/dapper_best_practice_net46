using System.Data;
using DapperMySqlCrudExample.Infrastructure;
using MySql.Data.MySqlClient;

namespace DapperMySqlCrudExample.Tests.Infrastructure
{
    /// <summary>
    /// 連線至真實 MySQL 執行個體的 <see cref="IDbConnectionFactory"/> 實作，
    /// 僅供整合測試使用。
    /// </summary>
    internal sealed class LiveDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// 以指定連線字串初始化工廠。
        /// </summary>
        /// <param name="connectionString">MySQL 連線字串。</param>
        public LiveDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 建立並開啟 MySQL 連線。
        /// </summary>
        public IDbConnection Create()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}

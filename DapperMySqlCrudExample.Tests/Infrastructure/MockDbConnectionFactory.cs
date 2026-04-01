using System.Data;
using DapperMySqlCrudExample.Infrastructure;

namespace DapperMySqlCrudExample.Tests.Infrastructure
{
    /// <summary>
    /// 在整合測試中提供預先建立連線的 <see cref="IDbConnectionFactory"/> 實作。
    /// 可注入已開啟的 <see cref="IDbConnection"/>，使 Repository 複用同一連線，
    /// 方便在單一交易中驗證資料並於測試結束後自動回滾。
    /// </summary>
    internal sealed class MockDbConnectionFactory : IDbConnectionFactory
    {
        private readonly IDbConnection _connection;

        /// <summary>
        /// 以指定連線初始化 MockDbConnectionFactory。
        /// </summary>
        /// <param name="connection">已開啟的資料庫連線。</param>
        public MockDbConnectionFactory(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// 回傳建構時注入的連線，不另外開啟新連線。
        /// <para>⚠️ 呼叫端不應對回傳的連線呼叫 Dispose()，
        /// 因為連線的生命週期由外部測試步驟管理。</para>
        /// </summary>
        public IDbConnection Create() => _connection;
    }
}

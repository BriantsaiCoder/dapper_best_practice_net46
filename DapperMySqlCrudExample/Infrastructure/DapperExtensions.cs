using System.Data;
using Dapper;

namespace DapperMySqlCrudExample.Infrastructure
{
    /// <summary>
    /// <see cref="IDbConnectionFactory"/> 的 Dapper 輔助擴充方法。
    /// 統一封裝「有交易時使用既有連線 / 無交易時自建短生命週期連線」的判斷邏輯，
    /// 消除各 Repository 中重複的 <c>if (transaction != null)</c> 分支。
    /// </summary>
    internal static class DapperExtensions
    {
        /// <summary>
        /// 執行 SQL 並回傳純量值。
        /// 有傳入 <paramref name="transaction"/> 時，複用其 <see cref="IDbTransaction.Connection"/>；
        /// 否則透過 <paramref name="factory"/> 建立並自動釋放連線。
        /// </summary>
        /// <typeparam name="T">純量回傳型別。</typeparam>
        /// <param name="factory">連線工廠。</param>
        /// <param name="sql">SQL 陳述式（可含 Dapper 具名參數）。</param>
        /// <param name="param">Dapper 參數物件或匿名物件；可為 null。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>查詢回傳的純量值。</returns>
        internal static T ExecuteScalar<T>(
            this IDbConnectionFactory factory,
            string sql,
            object param = null,
            IDbTransaction transaction = null
        )
        {
            if (transaction != null)
                return transaction.Connection.ExecuteScalar<T>(sql, param, transaction);

            using (var conn = factory.Create())
                return conn.ExecuteScalar<T>(sql, param);
        }

        /// <summary>
        /// 執行非查詢 SQL（UPDATE / DELETE）並回傳是否影響至少一列。
        /// 有傳入 <paramref name="transaction"/> 時，複用其 <see cref="IDbTransaction.Connection"/>；
        /// 否則透過 <paramref name="factory"/> 建立並自動釋放連線。
        /// </summary>
        /// <param name="factory">連線工廠。</param>
        /// <param name="sql">SQL 陳述式（可含 Dapper 具名參數）。</param>
        /// <param name="param">Dapper 參數物件或匿名物件；可為 null。</param>
        /// <param name="transaction">選用的資料庫交易；null 表示不使用交易。</param>
        /// <returns>受影響列數 &gt; 0 則為 true，否則為 false。</returns>
        internal static bool Execute(
            this IDbConnectionFactory factory,
            string sql,
            object param = null,
            IDbTransaction transaction = null
        )
        {
            if (transaction != null)
                return transaction.Connection.Execute(sql, param, transaction) > 0;

            using (var conn = factory.Create())
                return conn.Execute(sql, param) > 0;
        }
    }
}

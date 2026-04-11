using System;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Samples;
using NLog;

namespace DapperMySqlCrudExample
{
    /// <summary>
    /// 應用程式進入點。
    /// 負責初始化基礎設施並驗證資料庫連線可用性，接著執行資料存取示範。
    /// </summary>
    internal static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            try
            {
                var connectionFactory = new DbConnectionFactory();
                VerifyDatabaseConnectivity(connectionFactory);

                _logger.Info("應用程式啟動檢查完成，資料庫連線驗證成功。");
                Console.WriteLine("啟動檢查完成，資料庫連線正常。");

                Console.WriteLine("開始執行資料存取示範。");
                CrudSampleRunner.RunAllSamples(connectionFactory);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "應用程式啟動失敗。");
                Console.Error.WriteLine($"\n[錯誤] {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine(
                    "請確認連線字串設定正確，且目標資料庫可連線；若後續工作流程依賴特定資料表，請確認 schema 已完成部署。"
                );
            }
            finally
            {
                // 【新手導讀】NLog 使用非同步 buffer 寫入日誌檔，
                // 必須在程式結束前呼叫 Shutdown() 將 buffer 中的日誌全部 flush 到磁碟，
                // 否則最後幾筆日誌可能遺失。放在 finally 確保即使發生例外也會執行。
                LogManager.Shutdown();
            }
        }

        /// <remarks>
        /// 【新手導讀】SELECT 1 是最輕量的連線驗證方式，不存取任何資料表，
        /// 僅確認「能成功建立連線並執行 SQL」。若連線字串錯誤或資料庫不可用，會在此拋出例外。
        /// </remarks>
        private static void VerifyDatabaseConnectivity(DbConnectionFactory connectionFactory)
        {
            try
            {
                using (var connection = connectionFactory.Create())
                {
                    connection.ExecuteScalar<int>("SELECT 1");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "資料庫連線驗證失敗（SELECT 1）。");
                throw;
            }
        }

    }
}

using System;
using Dapper;
using DapperMySqlCrudExample.Demos;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Repositories;
using DapperMySqlCrudExample.Services;
using NLog;

namespace DapperMySqlCrudExample
{
    /// <summary>
    /// 應用程式進入點。
    /// 負責初始化基礎設施並驗證資料庫連線可用性。
    /// 使用 --demo 參數可執行 CRUD 示範（參見 Demos/CrudDemoRunner.cs）。
    /// </summary>
    internal static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var shouldRunDemo = ShouldRunDemo(args);

            try
            {
                var connectionFactory = new DbConnectionFactory();
                VerifyDatabaseConnectivity(connectionFactory);

                _logger.Info("應用程式啟動檢查完成，資料庫連線驗證成功。");
                Console.WriteLine("啟動檢查完成，資料庫連線正常。");

                if (shouldRunDemo)
                {
                    Console.WriteLine("已啟用 --demo，開始執行資料存取示範。");

                    var detectionSpecRepository = new DetectionSpecRepository(connectionFactory);
                    var siteTestStatisticRepository = new SiteTestStatisticRepository(connectionFactory);
                    var detectionMethodRepository = new DetectionMethodRepository(connectionFactory);

                    var detectionSpecService = new DetectionSpecService(
                        connectionFactory,
                        detectionSpecRepository,
                        siteTestStatisticRepository,
                        detectionMethodRepository
                    );

                    CrudDemoRunner.RunAllDemos(
                        connectionFactory,
                        detectionSpecRepository,
                        siteTestStatisticRepository,
                        detectionSpecService
                    );
                }
                else
                {
                    Console.WriteLine("目前為安全模式，僅進行啟動檢查。");
                    Console.WriteLine("若要執行新增 / 更新 / 刪除示範，請以 --demo 重新啟動。");
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "應用程式啟動失敗。");
                Console.Error.WriteLine($"\n[錯誤] {ex.GetType().Name}: {ex.Message}");
                Console.Error.WriteLine(
                    "請確認連線字串設定正確，且目標資料庫可連線；若後續工作流程依賴特定資料表，請確認 schema 已完成部署。"
                );
                return 1;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void VerifyDatabaseConnectivity(DbConnectionFactory connectionFactory)
        {
            using (var connection = connectionFactory.Create())
            {
                connection.ExecuteScalar<int>("SELECT 1");
            }
        }

        private static bool ShouldRunDemo(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            foreach (var arg in args)
            {
                if (string.Equals(arg, "--demo", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}

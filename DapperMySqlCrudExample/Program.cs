using System;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Repositories;
using DapperMySqlCrudExample.Samples;
using DapperMySqlCrudExample.Services;
using NLog;

namespace DapperMySqlCrudExample
{
    /// <summary>
    /// 應用程式進入點。
    /// 負責初始化基礎設施並驗證資料庫連線可用性。
    /// 使用 --sample 參數可執行 sample（參見 Samples/CrudSampleRunner.cs）。
    /// </summary>
    internal static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (HasArgument(args, "--help", "-h"))
            {
                ShowUsage();
                return 0;
            }

            var shouldRunSample = HasArgument(args, "--sample", "--demo");

            try
            {
                var connectionFactory = new DbConnectionFactory();
                VerifyDatabaseConnectivity(connectionFactory);

                _logger.Info("應用程式啟動檢查完成，資料庫連線驗證成功。");
                Console.WriteLine("啟動檢查完成，資料庫連線正常。");

                if (shouldRunSample)
                {
                    Console.WriteLine("已啟用 sample 模式，開始執行資料存取示範。");

                    var detectionSpecRepository = new DetectionSpecRepository(connectionFactory);
                    var siteTestStatisticRepository = new SiteTestStatisticRepository(
                        connectionFactory
                    );
                    var detectionMethodRepository = new DetectionMethodRepository(
                        connectionFactory
                    );

                    var detectionSpecService = new DetectionSpecService(
                        connectionFactory,
                        detectionSpecRepository,
                        siteTestStatisticRepository,
                        detectionMethodRepository
                    );

                    CrudSampleRunner.RunAllSamples(
                        connectionFactory,
                        detectionSpecRepository,
                        siteTestStatisticRepository,
                        detectionSpecService
                    );
                }
                else
                {
                    Console.WriteLine("目前為安全模式，僅進行啟動檢查。");
                    Console.WriteLine("若要執行 sample，請以 --sample 重新啟動。");
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

        private static bool HasArgument(string[] args, params string[] flags)
        {
            if (args == null || args.Length == 0)
                return false;

            foreach (var arg in args)
            {
                foreach (var flag in flags)
                {
                    if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static void ShowUsage()
        {
            Console.WriteLine("DapperMySqlCrudExample — Dapper + MySQL 最佳實踐示範");
            Console.WriteLine();
            Console.WriteLine("用法：DapperMySqlCrudExample [選項]");
            Console.WriteLine();
            Console.WriteLine("選項：");
            Console.WriteLine("  （無參數）   啟動檢查模式，僅驗證資料庫連線");
            Console.WriteLine("  --sample     執行 CRUD sample（--demo 仍可作為相容別名）");
            Console.WriteLine("  --help, -h   顯示此說明");
        }
    }
}

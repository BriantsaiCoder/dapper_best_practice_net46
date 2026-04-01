using System;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Repositories;
using DapperMySqlCrudExample.Services;
using NLog;

namespace DapperMySqlCrudExample
{
    /// <summary>
    /// 應用程式進入點。
    /// 負責初始化基礎設施並驗證資料庫連線可用性，
    /// 不在啟動階段寫入或修改業務資料。
    /// </summary>
    internal static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static IDbConnectionFactory _connectionFactory;
        private static IDetectionSpecService _detectionSpecService;

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            try
            {
                InitializeInfrastructure();
                VerifyDatabaseConnectivity();

                _logger.Info("應用程式啟動檢查完成，資料庫連線驗證成功。");
                Console.WriteLine("啟動檢查完成，資料庫連線正常。");
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

        private static void InitializeInfrastructure()
        {
            _connectionFactory = new DbConnectionFactory();

            var specRepo = new DetectionSpecRepository(_connectionFactory);
            _detectionSpecService = new DetectionSpecService(_connectionFactory, specRepo);
        }

        private static void VerifyDatabaseConnectivity()
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.ExecuteScalar<int>("SELECT 1");
            }
        }
    }
}

using System;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;
using NLog;

namespace DapperMySqlCrudExample
{
    /// <summary>
    /// 應用程式進入點。
    /// 負責初始化基礎設施並驗證資料庫連線可用性，
    /// 並示範不使用交易、使用交易與 SITE_MEAN 規格計算的資料存取範例。
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
                var detectionSpecRepository = new DetectionSpecRepository(connectionFactory);
                var siteTestStatisticRepository = new SiteTestStatisticRepository(connectionFactory);

                VerifyDatabaseConnectivity(connectionFactory);

                _logger.Info("應用程式啟動檢查完成，資料庫連線驗證成功。");
                Console.WriteLine("啟動檢查完成，資料庫連線正常。");

                if (shouldRunDemo)
                {
                    Console.WriteLine("已啟用 --demo，開始執行資料存取示範。");
                    RunNonTransactionExample(connectionFactory);
                    RunTransactionExample(connectionFactory);
                    RunComputeSiteMeanSpecExample(detectionSpecRepository, siteTestStatisticRepository);
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

        // ─────────────────────────────────────────────────────────────────────
        // 範例一：不使用交易的完整 CRUD 流程
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 示範不依賴交易的基本 CRUD 操作。
        /// 每個方法內部自行開啟並關閉連線，彼此獨立。
        /// </summary>
        private static void RunNonTransactionExample(DbConnectionFactory connectionFactory)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  範例一：不使用交易的 CRUD 操作");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            var repo = new DetectionMethodRepository(connectionFactory);

            // 清理可能殘留的示範資料，確保 MethodCode 唯一性
            var existing = repo.GetByCode("DEMO_NO_TX");
            if (existing != null) repo.Delete(existing.Id);

            // ── Insert ───────────────────────────────────────────────────────
            var newMethod = new DetectionMethod
            {
                MethodCode = "DEMO_NO_TX",
                MethodName = "展示用檢測方法（無交易）",
                HasTestItem = true,
                HasUnitLevel = false
            };

            byte newId = repo.Insert(newMethod);
            _logger.Info("RunNonTransactionExample: 新增 DetectionMethod，Id={Id}", newId);
            Console.WriteLine($"  [Insert] 新增成功 → Id={newId}, MethodCode={newMethod.MethodCode}");

            // ── GetById ──────────────────────────────────────────────────────
            var inserted = repo.GetById(newId);
            Console.WriteLine($"  [GetById] 查詢結果 → MethodName={inserted?.MethodName}, HasTestItem={inserted?.HasTestItem}");

            // ── Update ───────────────────────────────────────────────────────
            inserted.MethodName = "展示用檢測方法（無交易）— 已更新";
            bool updated = repo.Update(inserted);
            _logger.Info("RunNonTransactionExample: 更新 DetectionMethod，Id={Id}, 結果={Result}", newId, updated);
            Console.WriteLine($"  [Update] 更新成功={updated}");

            // ── GetById after update ─────────────────────────────────────────
            var afterUpdate = repo.GetById(newId);
            Console.WriteLine($"  [GetById] 更新後 MethodName={afterUpdate?.MethodName}");

            // ── GetCount / GetPaged ──────────────────────────────────────────
            int total = repo.GetCount();
            Console.WriteLine($"  [GetCount] 現有筆數={total}");

            var page = repo.GetPaged(offset: 0, limit: 3);
            Console.Write("  [GetPaged] 前三筆 MethodCode：");
            foreach (var m in page)
                Console.Write($"{m.MethodCode} ");
            Console.WriteLine();

            // ── Delete ───────────────────────────────────────────────────────
            bool deleted = repo.Delete(newId);
            _logger.Info("RunNonTransactionExample: 刪除 DetectionMethod，Id={Id}, 結果={Result}", newId, deleted);
            Console.WriteLine($"  [Delete] 刪除成功={deleted}");

            Console.WriteLine("  範例一完成。");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 範例二：使用交易的資料庫存取（展示 Commit 與 Rollback）
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 示範將多個資料庫寫入操作包覆在同一交易中。
        /// 分兩個子場景：(A) 全部成功後 Commit，(B) 模擬失敗後 Rollback。
        /// </summary>
        private static void RunTransactionExample(DbConnectionFactory connectionFactory)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  範例二：使用交易的資料庫存取");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            var repo = new DetectionMethodRepository(connectionFactory);

            // 清理可能殘留的示範資料
            foreach (var code in new[] { "TX_DEMO_A1", "TX_DEMO_A2", "TX_DEMO_B" })
            {
                var e = repo.GetByCode(code);
                if (e != null) repo.Delete(e.Id);
            }

            // ── (A) Commit 場景 ───────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine("  ── (A) Commit 場景：同一交易內新增兩筆，全部成功後提交 ──");

            byte idA1 = 0, idA2 = 0;
            using (var conn = connectionFactory.Create())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    var methodA1 = new DetectionMethod
                    {
                        MethodCode = "TX_DEMO_A1",
                        MethodName = "交易示範 A1",
                        HasTestItem = true,
                        HasUnitLevel = false
                    };
                    idA1 = repo.Insert(methodA1, tx);
                    _logger.Info("RunTransactionExample(A): Insert A1 Id={Id}", idA1);
                    Console.WriteLine($"  [TX-A Insert A1] Id={idA1}, MethodCode={methodA1.MethodCode}");

                    var methodA2 = new DetectionMethod
                    {
                        MethodCode = "TX_DEMO_A2",
                        MethodName = "交易示範 A2",
                        HasTestItem = false,
                        HasUnitLevel = true
                    };
                    idA2 = repo.Insert(methodA2, tx);
                    _logger.Info("RunTransactionExample(A): Insert A2 Id={Id}", idA2);
                    Console.WriteLine($"  [TX-A Insert A2] Id={idA2}, MethodCode={methodA2.MethodCode}");

                    tx.Commit();
                    _logger.Info("RunTransactionExample(A): Commit 成功");
                    Console.WriteLine("  [TX-A Commit] 交易提交成功。");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    _logger.Warn(ex, "RunTransactionExample(A): Rollback");
                    Console.Error.WriteLine($"  [TX-A Rollback] 例外：{ex.Message}");
                    throw;
                }
            }

            // 驗證：交易提交後可用一般連線查詢
            var verifyA1 = repo.GetById(idA1);
            var verifyA2 = repo.GetById(idA2);
            Console.WriteLine($"  [TX-A Verify] A1={verifyA1?.MethodName}, A2={verifyA2?.MethodName}");

            // 清理測試資料
            repo.Delete(idA1);
            repo.Delete(idA2);
            Console.WriteLine("  [TX-A Cleanup] 測試資料已清除。");

            // ── (B) Rollback 場景 ────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine("  ── (B) Rollback 場景：交易內新增一筆後模擬異常，驗證資料未寫入 ──");

            byte idB = 0;
            using (var conn = connectionFactory.Create())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    var methodB = new DetectionMethod
                    {
                        MethodCode = "TX_DEMO_B",
                        MethodName = "交易示範 B（應被 Rollback）",
                        HasTestItem = false,
                        HasUnitLevel = false
                    };
                    idB = repo.Insert(methodB, tx);
                    _logger.Info("RunTransactionExample(B): Insert B Id={Id}（尚未 Commit）", idB);
                    Console.WriteLine($"  [TX-B Insert B] Id={idB}（交易尚未提交）");

                    // 模擬業務邏輯錯誤，觸發 Rollback
                    throw new InvalidOperationException("模擬業務錯誤，強制 Rollback。");
                }
                catch (InvalidOperationException ex)
                {
                    tx.Rollback();
                    _logger.Warn(ex, "RunTransactionExample(B): Rollback");
                    Console.WriteLine($"  [TX-B Rollback] {ex.Message}");
                }
            }

            // 驗證：Rollback 後應查不到該筆資料
            var verifyB = repo.GetById(idB);
            bool wasRolledBack = verifyB == null;
            _logger.Info("RunTransactionExample(B): Rollback 驗證，資料不存在={Result}", wasRolledBack);
            Console.WriteLine($"  [TX-B Verify] Rollback 驗證：資料確實不存在={wasRolledBack}");

            Console.WriteLine();
            Console.WriteLine("  範例二完成。");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 範例三：SITE_MEAN 規格計算
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 示範如何透過 Repository 依歷史統計資料建立 detection_specs 記錄。
        /// 若目前資料庫尚無可用的 site_test_statistics 樣本資料，則會顯示略過訊息。
        /// </summary>
        private static void RunComputeSiteMeanSpecExample(
            DetectionSpecRepository detectionSpecRepository,
            SiteTestStatisticRepository siteTestStatisticRepository
        )
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  範例三：SITE_MEAN 規格計算");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            var sample = siteTestStatisticRepository.GetLatestSampleForSpecCalculation();
            if (sample == null)
            {
                Console.WriteLine(
                    "  [Skip] site_test_statistics 尚無 mean_value 與 start_time 俱全的樣本資料，略過規格計算示範。"
                );
                return;
            }

            try
            {
                long newSpecId = detectionSpecRepository.ComputeAndInsertSiteMeanSpec(
                    sample.Program,
                    sample.SiteId,
                    sample.TestItemName
                );
                var createdSpec = detectionSpecRepository.GetById(newSpecId);

                _logger.Info(
                    "RunComputeSiteMeanSpecExample: 建立 DetectionSpec 成功，Id={Id}, Program={Program}, SiteId={SiteId}, TestItem={TestItem}",
                    newSpecId,
                    sample.Program,
                    sample.SiteId,
                    sample.TestItemName
                );
                Console.WriteLine(
                    $"  [Compute] 新增成功 → Id={newSpecId}, Program={sample.Program}, SiteId={sample.SiteId}, TestItem={sample.TestItemName}"
                );
                Console.WriteLine(
                    $"  [Verify] UCL={createdSpec?.SpecUpperLimit}, LCL={createdSpec?.SpecLowerLimit}, Mean={createdSpec?.SpecCalcMean}, Std={createdSpec?.SpecCalcStd}"
                );

                bool cleaned = detectionSpecRepository.Delete(newSpecId);
                Console.WriteLine($"  [Cleanup] 示範資料已清除={cleaned}");
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warn(ex, "RunComputeSiteMeanSpecExample: 示範略過");
                Console.WriteLine($"  [Skip] {ex.Message}");
            }
        }
    }
}

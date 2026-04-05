using System;
using System.Data;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;
using DapperMySqlCrudExample.Services;
using NLog;

namespace DapperMySqlCrudExample.Samples
{
    /// <summary>
    /// CRUD sample 工具類別。
    /// 本類別僅供展示用途，展示如何使用 Repository 進行資料存取操作。
    /// 新工程師可參考這些示範方法了解交易與非交易模式的使用方式。
    /// </summary>
    internal static class CrudSampleRunner
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 執行所有 sample。
        /// </summary>
        public static void RunAllSamples(DbConnectionFactory connectionFactory)
        {
            RunNonTransactionExample(connectionFactory);
            RunTransactionExample(connectionFactory);

            var detectionSpecRepository = new DetectionSpecRepository(connectionFactory);
            var siteTestStatisticRepository = new SiteTestStatisticRepository(connectionFactory);
            var detectionMethodRepository = new DetectionMethodRepository(connectionFactory);
            var detectionSpecService = new DetectionSpecService(
                connectionFactory,
                detectionSpecRepository,
                siteTestStatisticRepository,
                detectionMethodRepository
            );

            RunComputeSiteMeanSpecExample(
                detectionSpecService,
                siteTestStatisticRepository,
                detectionSpecRepository
            );
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

            // 清理可能殘留的示範資料，確保 MethodKey 唯一性
            var existing = repo.GetByKey("DEMO_NO_TX");
            if (existing != null)
                repo.Delete(existing.Id);

            // ── Insert ───────────────────────────────────────────────────────
            var newMethod = new DetectionMethod
            {
                MethodKey = "DEMO_NO_TX",
                MethodName = "展示用檢測方法（無交易）",
                HasTestItem = true,
                HasUnitLevel = false,
            };

            byte newId = repo.Insert(newMethod);
            _logger.Info("RunNonTransactionExample: 新增 DetectionMethod，Id={Id}", newId);
            Console.WriteLine($"  [Insert] 新增成功 → Id={newId}, MethodKey={newMethod.MethodKey}");

            // ── GetById ──────────────────────────────────────────────────────
            var inserted = repo.GetById(newId);
            Console.WriteLine(
                $"  [GetById] 查詢結果 → MethodName={inserted?.MethodName}, HasTestItem={inserted?.HasTestItem}"
            );

            // ── Update ───────────────────────────────────────────────────────
            inserted.MethodName = "展示用檢測方法（無交易）— 已更新";
            bool updated = repo.Update(inserted);
            _logger.Info(
                "RunNonTransactionExample: 更新 DetectionMethod，Id={Id}, 結果={Result}",
                newId,
                updated
            );
            Console.WriteLine($"  [Update] 更新成功={updated}");

            // ── GetById after update ─────────────────────────────────────────
            var afterUpdate = repo.GetById(newId);
            Console.WriteLine($"  [GetById] 更新後 MethodName={afterUpdate?.MethodName}");

            // ── Exists / GetCount / GetByKey ────────────────────────────────
            bool existsAfterUpdate = repo.Exists(newId);
            Console.WriteLine($"  [Exists] 更新後資料存在={existsAfterUpdate}");

            int total = repo.GetCount();
            Console.WriteLine($"  [GetCount] 現有筆數={total}");

            var builtInMethod = repo.GetByKey("YIELD");
            Console.WriteLine($"  [GetByKey] 內建方法 YIELD 存在={builtInMethod != null}");

            // ── Delete ───────────────────────────────────────────────────────
            bool deleted = repo.Delete(newId);
            _logger.Info(
                "RunNonTransactionExample: 刪除 DetectionMethod，Id={Id}, 結果={Result}",
                newId,
                deleted
            );
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
                var e = repo.GetByKey(code);
                if (e != null)
                    repo.Delete(e.Id);
            }

            // ── (A) Commit 場景 ───────────────────────────────────────────────
            // using 區塊結束時，若 Commit() 未被呼叫，MySqlTransaction.Dispose()
            // 會自動 Rollback。因此正常路徑只需呼叫 Commit()，無需顯式 try/catch。
            Console.WriteLine();
            Console.WriteLine("  ── (A) Commit 場景：同一交易內新增兩筆，全部成功後提交 ──");

            byte idA1 = 0,
                idA2 = 0;
            using (var conn = connectionFactory.Create())
            using (var tx = conn.BeginTransaction())
            {
                var methodA1 = new DetectionMethod
                {
                    MethodKey = "TX_DEMO_A1",
                    MethodName = "交易示範 A1",
                    HasTestItem = true,
                    HasUnitLevel = false,
                };
                idA1 = repo.Insert(methodA1, tx);
                _logger.Info("RunTransactionExample(A): Insert A1 Id={Id}", idA1);
                Console.WriteLine($"  [TX-A Insert A1] Id={idA1}, MethodKey={methodA1.MethodKey}");

                var methodA2 = new DetectionMethod
                {
                    MethodKey = "TX_DEMO_A2",
                    MethodName = "交易示範 A2",
                    HasTestItem = false,
                    HasUnitLevel = true,
                };
                idA2 = repo.Insert(methodA2, tx);
                _logger.Info("RunTransactionExample(A): Insert A2 Id={Id}", idA2);
                Console.WriteLine($"  [TX-A Insert A2] Id={idA2}, MethodKey={methodA2.MethodKey}");

                tx.Commit();
                _logger.Info("RunTransactionExample(A): Commit 成功");
                Console.WriteLine("  [TX-A Commit] 交易提交成功。");
            }

            // 驗證：交易提交後可用一般連線查詢
            var verifyA1 = repo.GetById(idA1);
            var verifyA2 = repo.GetById(idA2);
            Console.WriteLine(
                $"  [TX-A Verify] A1={verifyA1?.MethodName}, A2={verifyA2?.MethodName}"
            );

            // 清理測試資料
            repo.Delete(idA1);
            repo.Delete(idA2);
            Console.WriteLine("  [TX-A Cleanup] 測試資料已清除。");

            // ── (B) Rollback 場景 ────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine(
                "  ── (B) Rollback 場景：交易內新增一筆後模擬異常，驗證資料未寫入 ──"
            );

            byte idB = 0;
            using (var conn = connectionFactory.Create())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    var methodB = new DetectionMethod
                    {
                        MethodKey = "TX_DEMO_B",
                        MethodName = "交易示範 B（應被 Rollback）",
                        HasTestItem = false,
                        HasUnitLevel = false,
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
            _logger.Info(
                "RunTransactionExample(B): Rollback 驗證，資料不存在={Result}",
                wasRolledBack
            );
            Console.WriteLine($"  [TX-B Verify] Rollback 驗證：資料確實不存在={wasRolledBack}");

            Console.WriteLine();
            Console.WriteLine("  範例二完成。");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 範例三：SITE_MEAN 規格計算
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 示範如何透過 Service 依歷史統計資料建立 detection_specs 記錄。
        /// 若目前資料庫尚無可用的 site_test_statistics 樣本資料，則會顯示略過訊息。
        /// </summary>
        /// <remarks>
        /// ★ 前置條件：site_test_statistics 中需有至少 2 筆符合條件的資料
        ///   （program + site_id + test_item_name 相同，且 mean_value、start_time 非 NULL），
        ///   且 lots_info 外鍵依賴表須已建立（參見 Sql/schema-legacy.sql）。
        ///   空資料庫執行時本範例會自動略過，不影響範例一、二。
        /// </remarks>
        private static void RunComputeSiteMeanSpecExample(
            DetectionSpecService detectionSpecService,
            SiteTestStatisticRepository siteTestStatisticRepository,
            DetectionSpecRepository detectionSpecRepository
        )
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  範例三：SITE_MEAN 規格計算");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            var calcParams = siteTestStatisticRepository.GetCalcParamsFromLatestSample();
            if (calcParams == null)
            {
                Console.WriteLine(
                    "  [Skip] site_test_statistics 尚無 mean_value 與 start_time 俱全的樣本資料，略過規格計算示範。"
                );
                Console.WriteLine(
                    "         若要執行本範例，請先匯入測試資料（至少 2 筆相同 program/site/testItem 的統計記錄）。"
                );
                return;
            }

            long newSpecId = 0;
            try
            {
                newSpecId = detectionSpecService.ComputeAndInsertSiteMeanSpec(
                    calcParams.ProgramName,
                    calcParams.SiteId,
                    calcParams.TestItemName
                );
                var createdSpec = detectionSpecRepository.GetById(newSpecId);

                _logger.Info(
                    "RunComputeSiteMeanSpecExample: 建立 DetectionSpec 成功，Id={Id}, Program={Program}, SiteId={SiteId}, TestItem={TestItem}",
                    newSpecId,
                    calcParams.ProgramName,
                    calcParams.SiteId,
                    calcParams.TestItemName
                );
                Console.WriteLine(
                    $"  [Compute] 新增成功 → Id={newSpecId}, Program={calcParams.ProgramName}, SiteId={calcParams.SiteId}, TestItem={calcParams.TestItemName}"
                );
                Console.WriteLine(
                    $"  [Verify] UCL={createdSpec?.SpecUpperLimit}, LCL={createdSpec?.SpecLowerLimit}, Mean={createdSpec?.SpecCalcMean}, Std={createdSpec?.SpecCalcStd}"
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warn(ex, "RunComputeSiteMeanSpecExample: 示範略過");
                Console.WriteLine($"  [Skip] {ex.Message}");
            }
            finally
            {
                // 確保示範資料清理，即使發生例外也要執行
                if (newSpecId > 0)
                {
                    try
                    {
                        bool cleaned = detectionSpecRepository.Delete(newSpecId);
                        Console.WriteLine($"  [Cleanup] 示範資料已清除={cleaned}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(
                            ex,
                            "RunComputeSiteMeanSpecExample: 清理示範資料失敗，Id={Id}",
                            newSpecId
                        );
                        Console.WriteLine($"  [Cleanup] 清理失敗（非致命錯誤）");
                    }
                }
            }
        }
    }
}

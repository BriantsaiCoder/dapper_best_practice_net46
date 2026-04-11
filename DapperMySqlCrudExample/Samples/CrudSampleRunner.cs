using System;
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
        internal static void RunAllSamples(DbConnectionFactory connectionFactory)
        {
            RunNonTransactionExample(connectionFactory);

            // 【新手導讀】手動建構依賴注入（Manual DI）：
            // 生產環境通常使用 IoC 容器（如 Autofac、Microsoft.Extensions.DependencyInjection）自動解析依賴，
            // 這裡為了簡化示範而手動建構。流程為：先建立 Repository，再將它們注入 Service。
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

            try
            {
                var repo = new DetectionMethodRepository(connectionFactory);

                // 清理可能殘留的示範資料，確保 MethodKey 唯一性
                var existing = repo.GetByKey("DEMO_NO_TX");
                if (existing != null)
                {
                    repo.Delete(existing.Id);
                }

                // ── Insert ───────────────────────────────────────────────────────
                var newMethod = new DetectionMethod
                {
                    MethodKey = "DEMO_NO_TX",
                    MethodName = "展示用檢測方法（無交易）",
                };

                byte newId = repo.Insert(newMethod);
                _logger.Info("RunNonTransactionExample: 新增 DetectionMethod，Id={Id}", newId);
                Console.WriteLine($"  [Insert] 新增成功 → Id={newId}, MethodKey={newMethod.MethodKey}");

                // ── GetById ──────────────────────────────────────────────────────
                var inserted = repo.GetById(newId);
                Console.WriteLine(
                    $"  [GetById] 查詢結果 → MethodName={inserted?.MethodName}"
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
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "RunNonTransactionExample: 非預期例外 | Type={Type} | Message={Message}",
                    ex.GetType().FullName,
                    ex.Message
                );
                Console.Error.WriteLine($"  [Error] 範例一執行失敗：{ex.GetType().Name}: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 範例二：SITE_MEAN 規格計算
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 示範如何透過 Service 依歷史統計資料建立 detection_specs 記錄。
        /// 若目前資料庫尚無可用的 site_test_statistics 樣本資料，則會顯示略過訊息。
        /// </summary>
        /// <remarks>
        /// ★ 前置條件：site_test_statistics 中需有至少 2 筆符合條件的資料
        ///   （program + site_id + test_item_name 相同，且 mean_value、start_time 非 NULL），
        ///   且 lots_info 外鍵依賴表須已建立（參見 Sql/schema-legacy.sql）。
        ///   空資料庫執行時本範例會自動略過，不影響範例一。
        /// </remarks>
        private static void RunComputeSiteMeanSpecExample(
            DetectionSpecService detectionSpecService,
            SiteTestStatisticRepository siteTestStatisticRepository,
            DetectionSpecRepository detectionSpecRepository
        )
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  範例二：SITE_MEAN 規格計算");
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
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "RunComputeSiteMeanSpecExample: 非預期例外 | Type={Type} | Message={Message}",
                    ex.GetType().FullName,
                    ex.Message
                );
                Console.WriteLine($"  [Error] {ex.GetType().Name}: {ex.Message}");
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

using System;
using System.Linq;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;

namespace DapperMySqlCrudExample
{
    /// <summary>
    /// .NET 4.6 + Dapper + MySQL CRUD 範例主程式。
    /// 請先在 App.config 中設定正確的 MySQL 連線字串，
    /// 並確保已依照 Sql/schema.sql 建立資料庫與資料表。
    /// </summary>
    internal static class Program
    {
        private static IDbConnectionFactory _factory;

        private static IDetectionMethodRepository _detectionMethodRepo;
        private static IAnomalyLotRepository _anomalyLotRepo;
        private static IAnomalyTestItemRepository _anomalyTestItemRepo;
        private static IAnomalyUnitRepository _anomalyUnitRepo;
        private static IAnomalyLotProcessMappingRepository _lotProcessRepo;
        private static IAnomalyUnitProcessMappingRepository _unitProcessRepo;
        private static IDetectionSpecRepository _detectionSpecRepo;
        private static ISiteTestStatisticRepository _siteStatRepo;
        private static IGoodLotRepository _goodLotRepo;

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 初始化連線工廠與所有 Repository
            _factory = new DbConnectionFactory();
            _detectionMethodRepo = new DetectionMethodRepository(_factory);
            _anomalyLotRepo = new AnomalyLotRepository(_factory);
            _anomalyTestItemRepo = new AnomalyTestItemRepository(_factory);
            _anomalyUnitRepo = new AnomalyUnitRepository(_factory);
            _lotProcessRepo = new AnomalyLotProcessMappingRepository(_factory);
            _unitProcessRepo = new AnomalyUnitProcessMappingRepository(_factory);
            _detectionSpecRepo = new DetectionSpecRepository(_factory);
            _siteStatRepo = new SiteTestStatisticRepository(_factory);
            _goodLotRepo = new GoodLotRepository(_factory);

            try
            {
                DemoDetectionMethod();
                DemoAnomalyLot();
                DemoAnomalyTestItem();
                DemoAnomalyUnit();
                DemoAnomalyLotProcessMapping();
                DemoAnomalyUnitProcessMapping();
                DemoDetectionSpec();
                DemoComputeAndInsertSiteMeanSpec();
                DemoSiteTestStatistic();
                DemoGoodLot();

                Console.WriteLine("\n========== 所有 CRUD 示範完成 ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[錯誤] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine("請確認 App.config 連線字串設定正確，且資料庫與資料表已建立。");
            }

            Console.WriteLine("\n按任意鍵結束...");
            Console.ReadKey();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. 偵測方法主表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoDetectionMethod()
        {
            PrintSection("1. DetectionMethod（偵測方法）CRUD");

            // Create
            var newMethod = new DetectionMethod
            {
                MethodCode = "DEMO_METHOD",
                MethodName = "示範偵測方法",
                HasTestItem = true,
                HasUnitLevel = false,
            };
            byte insertedId = _detectionMethodRepo.Insert(newMethod);
            Console.WriteLine($"  [Insert] 新增成功，ID = {insertedId}");

            // Read (all)
            var allMethods = _detectionMethodRepo.GetAll();
            Console.WriteLine($"  [GetAll] 共 {allMethods.Count()} 筆");

            // Read (by id)
            var found = _detectionMethodRepo.GetById(insertedId);
            Console.WriteLine(
                $"  [GetById] MethodCode={found?.MethodCode}, MethodName={found?.MethodName}"
            );

            // Read (by code)
            var byCode = _detectionMethodRepo.GetByCode("DEMO_METHOD");
            Console.WriteLine($"  [GetByCode] 找到: {byCode?.MethodName}");

            // Update
            if (found != null)
            {
                found.MethodName = "示範偵測方法（已更新）";
                bool updated = _detectionMethodRepo.Update(found);
                Console.WriteLine($"  [Update] 更新結果: {updated}");
            }

            // Delete
            bool deleted = _detectionMethodRepo.Delete(insertedId);
            Console.WriteLine($"  [Delete] 刪除結果: {deleted}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. 異常批號主表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoAnomalyLot()
        {
            PrintSection("2. AnomalyLot（異常批號）CRUD");

            // 取得一個有效的 DetectionMethod ID（需先確保 detection_methods 資料存在）
            const byte methodId = 1; // YIELD
            const int lotsInfoId = 10001;

            var newLot = new AnomalyLot
            {
                LotsInfoId = lotsInfoId,
                DetectionMethodId = methodId,
                SpecUpperLimit = 1.000000000m,
                SpecLowerLimit = 0.950000000m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 3, 31),
            };
            long lotId = _anomalyLotRepo.Insert(newLot);
            Console.WriteLine($"  [Insert] AnomalyLot ID = {lotId}");

            var getLot = _anomalyLotRepo.GetById(lotId);
            Console.WriteLine(
                $"  [GetById] LotsInfoId={getLot?.LotsInfoId}, MethodId={getLot?.DetectionMethodId}"
            );

            var byLots = _anomalyLotRepo.GetByLotsInfoId(lotsInfoId);
            Console.WriteLine($"  [GetByLotsInfoId] 找到 {byLots.Count()} 筆");

            // 分頁 / 計數 / 存在檢查
            int totalCount = _anomalyLotRepo.GetCount();
            Console.WriteLine($"  [GetCount] 總筆數 = {totalCount}");

            var paged = _anomalyLotRepo.GetPaged(0, 5);
            Console.WriteLine($"  [GetPaged] 第一頁取得 {paged.Count()} 筆");

            bool exists = _anomalyLotRepo.Exists(lotId);
            Console.WriteLine($"  [Exists] ID={lotId} 存在: {exists}");

            if (getLot != null)
            {
                getLot.SpecUpperLimit = 1.050000000m;
                Console.WriteLine($"  [Update] 結果: {_anomalyLotRepo.Update(getLot)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_anomalyLotRepo.Delete(lotId)}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. 異常測項明細表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoAnomalyTestItem()
        {
            PrintSection("3. AnomalyTestItem（異常測項）CRUD");

            // 需先建立 AnomalyLot 父資料
            var lot = new AnomalyLot
            {
                LotsInfoId = 10002,
                DetectionMethodId = 2, // STD
                SpecUpperLimit = 5.0m,
                SpecLowerLimit = 0.0m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 6, 30),
            };
            long lotId = _anomalyLotRepo.Insert(lot);

            var item = new AnomalyTestItem
            {
                AnomalyLotId = lotId,
                TestItemName = "Vth",
                DetectionValue = 3.14159m,
                SpecUpperLimit = 5.0m,
                SpecLowerLimit = 0.0m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 6, 30),
            };
            long itemId = _anomalyTestItemRepo.Insert(item);
            Console.WriteLine($"  [Insert] AnomalyTestItem ID = {itemId}");

            var getItem = _anomalyTestItemRepo.GetById(itemId);
            Console.WriteLine(
                $"  [GetById] TestItemName={getItem?.TestItemName}, Value={getItem?.DetectionValue}"
            );

            if (getItem != null)
            {
                getItem.DetectionValue = 3.5m;
                Console.WriteLine($"  [Update] 結果: {_anomalyTestItemRepo.Update(getItem)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_anomalyTestItemRepo.Delete(itemId)}");
            _anomalyLotRepo.Delete(lotId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. 異常 Unit 明細表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoAnomalyUnit()
        {
            PrintSection("4. AnomalyUnit（異常 Unit）CRUD");

            var lot = new AnomalyLot
            {
                LotsInfoId = 10003,
                DetectionMethodId = 3,
                SpecUpperLimit = 5.0m,
                SpecLowerLimit = 0.0m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var testItem = new AnomalyTestItem
            {
                TestItemName = "Ioff",
                DetectionValue = 1.23m,
                SpecUpperLimit = 2.0m,
                SpecLowerLimit = 0.5m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var unit = new AnomalyUnit
            {
                UnitId = "WAFER-001-DIE-01",
                DetectionValue = 1.99m,
                SpecUpperLimit = 2.0m,
                SpecLowerLimit = 0.5m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            long lotId,
                itemId,
                unitId;
            using (var conn = _factory.Create())
            using (var tx = conn.BeginTransaction())
            {
                lotId = _anomalyLotRepo.Insert(lot, tx);
                testItem.AnomalyLotId = lotId;
                itemId = _anomalyTestItemRepo.Insert(testItem, tx);
                unit.AnomalyTestItemId = itemId;
                unitId = _anomalyUnitRepo.Insert(unit, tx);
                tx.Commit();
            }
            Console.WriteLine($"  [Insert] AnomalyUnit ID = {unitId}");

            var getUnit = _anomalyUnitRepo.GetById(unitId);
            Console.WriteLine(
                $"  [GetById] UnitId={getUnit?.UnitId}, Value={getUnit?.DetectionValue}"
            );

            if (getUnit != null)
            {
                getUnit.DetectionValue = 1.85m;
                Console.WriteLine($"  [Update] 結果: {_anomalyUnitRepo.Update(getUnit)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_anomalyUnitRepo.Delete(unitId)}");
            _anomalyTestItemRepo.Delete(itemId);
            _anomalyLotRepo.Delete(lotId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. 批號 Process Mapping
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoAnomalyLotProcessMapping()
        {
            PrintSection("5. AnomalyLotProcessMapping（批號製程站點）CRUD");

            var lot = new AnomalyLot
            {
                LotsInfoId = 10004,
                DetectionMethodId = 1,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var mapping = new AnomalyLotProcessMapping
            {
                StationName = "Diffusion",
                EquipmentId = "EQP-D-001",
                ProcessTime = new DateTime(2024, 3, 15, 8, 30, 0),
            };
            long lotId,
                mappingId;
            using (var conn = _factory.Create())
            using (var tx = conn.BeginTransaction())
            {
                lotId = _anomalyLotRepo.Insert(lot, tx);
                mapping.AnomalyLotId = lotId;
                mappingId = _lotProcessRepo.Insert(mapping, tx);
                tx.Commit();
            }
            Console.WriteLine($"  [Insert] Mapping ID = {mappingId}");

            var getMapping = _lotProcessRepo.GetById(mappingId);
            Console.WriteLine(
                $"  [GetById] Station={getMapping?.StationName}, Equip={getMapping?.EquipmentId}"
            );

            if (getMapping != null)
            {
                getMapping.EquipmentId = "EQP-D-002";
                Console.WriteLine($"  [Update] 結果: {_lotProcessRepo.Update(getMapping)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_lotProcessRepo.Delete(mappingId)}");
            _anomalyLotRepo.Delete(lotId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. Unit Process Mapping + 交易示範
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoAnomalyUnitProcessMapping()
        {
            PrintSection("6. AnomalyUnitProcessMapping（Unit 製程 Boat）CRUD + 交易示範");

            // ── 情境一：4 張表串接 Insert，交易成功提交 ──────────────────────
            Console.WriteLine(
                "\n  [情境一] 交易成功提交（lot → testItem → unit → unitProcessMapping）"
            );
            var lot = new AnomalyLot
            {
                LotsInfoId = 10005,
                DetectionMethodId = 3,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var ti = new AnomalyTestItem
            {
                TestItemName = "Idsat",
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var au = new AnomalyUnit
            {
                UnitId = "W01-D05",
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var upMapping = new AnomalyUnitProcessMapping
            {
                BoatId = "BOAT-A-01",
                PositionX = 3,
                PositionY = 7,
                ProcessTime = new DateTime(2024, 3, 15, 9, 0, 0),
                StationName = "Diffusion",
                EquipmentId = "EQP-D-001",
            };
            long lotId,
                tiId,
                auId,
                upId;
            using (var conn = _factory.Create())
            using (var tx = conn.BeginTransaction())
            {
                lotId = _anomalyLotRepo.Insert(lot, tx);
                ti.AnomalyLotId = lotId;
                tiId = _anomalyTestItemRepo.Insert(ti, tx);
                au.AnomalyTestItemId = tiId;
                auId = _anomalyUnitRepo.Insert(au, tx);
                upMapping.AnomalyUnitId = auId;
                upId = _unitProcessRepo.Insert(upMapping, tx);
                tx.Commit();
            }
            Console.WriteLine($"  [Insert] UnitProcessMapping ID = {upId}（✅ 交易已提交）");

            var getUp = _unitProcessRepo.GetById(upId);
            Console.WriteLine(
                $"  [GetById] BoatId={getUp?.BoatId}, X={getUp?.PositionX}, Y={getUp?.PositionY}"
            );

            if (getUp != null)
            {
                getUp.BoatId = "BOAT-A-02";
                Console.WriteLine($"  [Update] 結果: {_unitProcessRepo.Update(getUp)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_unitProcessRepo.Delete(upId)}");
            _anomalyUnitRepo.Delete(auId);
            _anomalyTestItemRepo.Delete(tiId);
            _anomalyLotRepo.Delete(lotId);

            // ── 情境二：交易中途失敗，回復（Rollback）──────────────────────
            Console.WriteLine("\n  [情境二] 交易失敗回復");
            var lot2 = new AnomalyLot
            {
                LotsInfoId = 10005,
                DetectionMethodId = 3,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            var ti2 = new AnomalyTestItem
            {
                TestItemName = "Idsat_Rollback",
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
            };
            using (var conn = _factory.Create())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    long rollbackLotId = _anomalyLotRepo.Insert(lot2, tx);
                    ti2.AnomalyLotId = rollbackLotId;
                    _anomalyTestItemRepo.Insert(ti2, tx);
                    throw new InvalidOperationException("模擬業務邏輯錯誤，交易應回復");
                }
                catch (InvalidOperationException ex)
                {
                    tx.Rollback();
                    Console.WriteLine($"  ✅ 交易已回復（預期行為）：{ex.Message}");
                    Console.WriteLine("  資料未寫入資料庫");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. Spec 規格表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoDetectionSpec()
        {
            PrintSection("7. DetectionSpec（Spec 規格）CRUD");

            var spec = new DetectionSpec
            {
                Program = "PROD-A",
                TestItemName = "Vth",
                SiteId = 1,
                DetectionMethodId = 2, // STD
                SpecUpperLimit = 1.5m,
                SpecLowerLimit = 0.5m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 12, 31),
                SpecCalcMean = 1.23456789m,
                SpecCalcStd = 0.01234567m,
            };
            long specId = _detectionSpecRepo.Insert(spec);
            Console.WriteLine($"  [Insert] DetectionSpec ID = {specId}");

            var getSpec = _detectionSpecRepo.GetById(specId);
            Console.WriteLine(
                $"  [GetById] Program={getSpec?.Program}, Item={getSpec?.TestItemName}, Mean={getSpec?.SpecCalcMean}, Std={getSpec?.SpecCalcStd}"
            );

            var byProgramMethod = _detectionSpecRepo.GetByProgramAndMethod("PROD-A", 2);
            Console.WriteLine($"  [GetByProgramAndMethod] 找到 {byProgramMethod.Count()} 筆");

            var recentSpecs = _detectionSpecRepo
                .GetRecentByProgramAndMethodName("PROD-A", "示範偵測方法")
                .ToList();
            foreach (var s in recentSpecs)
            {
                Console.WriteLine(
                    $"    Spec#{s.Id} [{s.TestItemName}] 上限={FormatDecimal(s.SpecUpperLimit)}  下限={FormatDecimal(s.SpecLowerLimit)}  計算結束={s.SpecCalcEndTime:yyyy-MM-dd}"
                );
            }
            Console.WriteLine(
                $"  [GetRecentByProgramAndMethodName] Program=PROD-A, MethodName=示範偵測方法，最近一個月共 {recentSpecs.Count} 筆"
            );

            var latestSpec = _detectionSpecRepo.GetLatestByProgramAndMethodName(
                "PROD-A",
                "示範偵測方法"
            );
            if (latestSpec != null)
            {
                Console.WriteLine(
                    $"  [GetLatestByProgramAndMethodName] 最新 Spec#{latestSpec.Id} [{latestSpec.TestItemName}] 上限={FormatDecimal(latestSpec.SpecUpperLimit)}  下限={FormatDecimal(latestSpec.SpecLowerLimit)}  計算結束={latestSpec.SpecCalcEndTime:yyyy-MM-dd}"
                );
            }
            else
                Console.WriteLine($"  [GetLatestByProgramAndMethodName] 最近一個月無資料");

            if (getSpec != null)
            {
                getSpec.SpecUpperLimit = 1.8m;
                Console.WriteLine($"  [Update] 結果: {_detectionSpecRepo.Update(getSpec)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_detectionSpecRepo.Delete(specId)}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7b. ComputeAndInsertSiteMeanSpec
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoComputeAndInsertSiteMeanSpec()
        {
            PrintSection("7b. ComputeAndInsertSiteMeanSpec（計算 SITE_MEAN Spec）");
            Console.WriteLine("  [說明] 查詢 site_test_statistics 的 mean_value，");
            Console.WriteLine("         計算算術平均數與樣本標準差，以 mean±6σ 為上下限，");
            Console.WriteLine("         寫入 detection_specs（method_code = SITE_MEAN）。");
            try
            {
                long newSpecId = _detectionSpecRepo.ComputeAndInsertSiteMeanSpec(
                    programName: "PROD-A",
                    siteId: 1,
                    testItemName: "Vth"
                );
                Console.WriteLine(
                    $"  [ComputeAndInsertSiteMeanSpec] 新 DetectionSpec ID = {newSpecId}"
                );

                var spec = _detectionSpecRepo.GetById(newSpecId);
                if (spec != null)
                {
                    Console.WriteLine(
                        $"  [GetById] Program={spec.Program}, Site={spec.SiteId}, "
                            + $"Mean={spec.SpecCalcMean:F6}, Std={spec.SpecCalcStd:F6}, "
                            + $"UCL={spec.SpecUpperLimit:F6}, LCL={spec.SpecLowerLimit:F6}"
                    );
                    Console.WriteLine(
                        $"  [GetById] CalcStart={spec.SpecCalcStartTime:yyyy-MM-dd}, "
                            + $"CalcEnd={spec.SpecCalcEndTime:yyyy-MM-dd}"
                    );
                }

                Console.WriteLine(
                    $"  [Delete] 清理測試資料: {_detectionSpecRepo.Delete(newSpecId)}"
                );
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  [跳過] 資料不足，無法計算: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. Site 測項統計值表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoSiteTestStatistic()
        {
            PrintSection("8. SiteTestStatistic（Site 統計值）CRUD");

            var stat = new SiteTestStatistic
            {
                LotsInfoId = 10006,
                Program = "PROD-A",
                SiteId = 1,
                TestItemName = "Vth",
                MeanValue = 1.012m,
                MaxValue = 1.098m,
                MinValue = 0.934m,
                StdValue = 0.041m,
                CpValue = 1.23m,
                CpkValue = 1.18m,
                TesterId = "TESTER-01",
                StartTime = new DateTime(2024, 1, 1),
                EndTime = new DateTime(2024, 1, 31),
            };
            long statId = _siteStatRepo.Insert(stat);
            Console.WriteLine($"  [Insert] SiteTestStatistic ID = {statId}");

            var getStat = _siteStatRepo.GetById(statId);
            Console.WriteLine($"  [GetById] Program={getStat?.Program}, Mean={getStat?.MeanValue}");

            var bySite = _siteStatRepo.GetBySiteAndItem(1, "Vth");
            Console.WriteLine($"  [GetBySiteAndItem] 找到 {bySite.Count()} 筆");

            if (getStat != null)
            {
                getStat.MeanValue = 1.025m;
                Console.WriteLine($"  [Update] 結果: {_siteStatRepo.Update(getStat)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_siteStatRepo.Delete(statId)}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 9. 好批批號記錄表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoGoodLot()
        {
            PrintSection("9. GoodLot（好批）CRUD");

            var goodLot = new GoodLot
            {
                LotsInfoId = 10007,
                DetectionMethodId = 1, // YIELD
                SpecUpperLimit = 0.99m,
                SpecLowerLimit = 0.95m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime = new DateTime(2024, 3, 31),
            };
            long goodLotId = _goodLotRepo.Insert(goodLot);
            Console.WriteLine($"  [Insert] GoodLot ID = {goodLotId}");

            var getGoodLot = _goodLotRepo.GetById(goodLotId);
            Console.WriteLine(
                $"  [GetById] LotsInfoId={getGoodLot?.LotsInfoId}, MethodId={getGoodLot?.DetectionMethodId}"
            );

            var byLots = _goodLotRepo.GetByLotsInfoId(10007);
            Console.WriteLine($"  [GetByLotsInfoId] 找到 {byLots.Count()} 筆");

            if (getGoodLot != null)
            {
                getGoodLot.SpecUpperLimit = 0.999m;
                Console.WriteLine($"  [Update] 結果: {_goodLotRepo.Update(getGoodLot)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_goodLotRepo.Delete(goodLotId)}");
        }

        // ─────────────────────────────────────────────────────────────────────

        private static string FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("F4") : "無";
        }

        private static void PrintSection(string title)
        {
            Console.WriteLine($"\n--- {title} ---");
        }
    }
}

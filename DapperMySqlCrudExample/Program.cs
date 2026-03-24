using System;
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

        private static IDetectionMethodRepository          _detectionMethodRepo;
        private static IAnomalyLotRepository               _anomalyLotRepo;
        private static IAnomalyTestItemRepository          _anomalyTestItemRepo;
        private static IAnomalyUnitRepository              _anomalyUnitRepo;
        private static IAnomalyLotProcessMappingRepository _lotProcessRepo;
        private static IAnomalyUnitProcessMappingRepository _unitProcessRepo;
        private static IDetectionSpecRepository            _detectionSpecRepo;
        private static ISiteTestStatisticRepository        _siteStatRepo;
        private static IGoodLotRepository                  _goodLotRepo;

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 初始化連線工廠與所有 Repository
            _factory             = new DbConnectionFactory();
            _detectionMethodRepo = new DetectionMethodRepository(_factory);
            _anomalyLotRepo      = new AnomalyLotRepository(_factory);
            _anomalyTestItemRepo = new AnomalyTestItemRepository(_factory);
            _anomalyUnitRepo     = new AnomalyUnitRepository(_factory);
            _lotProcessRepo      = new AnomalyLotProcessMappingRepository(_factory);
            _unitProcessRepo     = new AnomalyUnitProcessMappingRepository(_factory);
            _detectionSpecRepo   = new DetectionSpecRepository(_factory);
            _siteStatRepo        = new SiteTestStatisticRepository(_factory);
            _goodLotRepo         = new GoodLotRepository(_factory);

            try
            {
                DemoDetectionMethod();
                DemoAnomalyLot();
                DemoAnomalyTestItem();
                DemoAnomalyUnit();
                DemoAnomalyLotProcessMapping();
                DemoAnomalyUnitProcessMapping();
                DemoDetectionSpec();
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
                MethodCode    = "DEMO_METHOD",
                MethodName    = "示範偵測方法",
                HasTestItem   = true,
                HasUnitLevel  = false
            };
            byte insertedId = _detectionMethodRepo.Insert(newMethod);
            Console.WriteLine($"  [Insert] 新增成功，ID = {insertedId}");

            // Read (all)
            var allMethods = _detectionMethodRepo.GetAll();
            int count = 0;
            foreach (var m in allMethods) count++;
            Console.WriteLine($"  [GetAll] 共 {count} 筆");

            // Read (by id)
            var found = _detectionMethodRepo.GetById(insertedId);
            Console.WriteLine($"  [GetById] MethodCode={found?.MethodCode}, MethodName={found?.MethodName}");

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
            const byte methodId  = 1;  // YIELD
            const int  lotsInfoId = 10001;

            var newLot = new AnomalyLot
            {
                LotsInfoId          = lotsInfoId,
                DetectionMethodId   = methodId,
                SpecUpperLimit      = 1.000000000m,
                SpecLowerLimit      = 0.950000000m,
                SpecCalcStartTime   = new DateTime(2024, 1, 1),
                SpecCalcEndTime     = new DateTime(2024, 3, 31)
            };
            long lotId = _anomalyLotRepo.Insert(newLot);
            Console.WriteLine($"  [Insert] AnomalyLot ID = {lotId}");

            var getLot = _anomalyLotRepo.GetById(lotId);
            Console.WriteLine($"  [GetById] LotsInfoId={getLot?.LotsInfoId}, MethodId={getLot?.DetectionMethodId}");

            var byLots = _anomalyLotRepo.GetByLotsInfoId(lotsInfoId);
            int cnt = 0; foreach (var x in byLots) cnt++;
            Console.WriteLine($"  [GetByLotsInfoId] 找到 {cnt} 筆");

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
                LotsInfoId        = 10002,
                DetectionMethodId = 2, // STD
                SpecUpperLimit    = 5.0m,
                SpecLowerLimit    = 0.0m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime   = new DateTime(2024, 6, 30)
            };
            long lotId = _anomalyLotRepo.Insert(lot);

            var item = new AnomalyTestItem
            {
                AnomalyLotId       = lotId,
                TestItemName       = "Vth",
                DetectionValue     = 3.14159m,
                SpecUpperLimit     = 5.0m,
                SpecLowerLimit     = 0.0m,
                SpecCalcStartTime  = new DateTime(2024, 1, 1),
                SpecCalcEndTime    = new DateTime(2024, 6, 30)
            };
            long itemId = _anomalyTestItemRepo.Insert(item);
            Console.WriteLine($"  [Insert] AnomalyTestItem ID = {itemId}");

            var getItem = _anomalyTestItemRepo.GetById(itemId);
            Console.WriteLine($"  [GetById] TestItemName={getItem?.TestItemName}, Value={getItem?.DetectionValue}");

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
                LotsInfoId = 10003, DetectionMethodId = 3,
                SpecUpperLimit = 5.0m, SpecLowerLimit = 0.0m,
                SpecCalcStartTime = new DateTime(2024, 1, 1), SpecCalcEndTime = new DateTime(2024, 12, 31)
            };
            long lotId = _anomalyLotRepo.Insert(lot);

            var testItem = new AnomalyTestItem
            {
                AnomalyLotId = lotId, TestItemName = "Ioff",
                DetectionValue = 1.23m, SpecUpperLimit = 2.0m, SpecLowerLimit = 0.5m,
                SpecCalcStartTime = new DateTime(2024, 1, 1), SpecCalcEndTime = new DateTime(2024, 12, 31)
            };
            long itemId = _anomalyTestItemRepo.Insert(testItem);

            var unit = new AnomalyUnit
            {
                AnomalyTestItemId = itemId,
                UnitId            = "WAFER-001-DIE-01",
                DetectionValue    = 1.99m,
                SpecUpperLimit    = 2.0m,
                SpecLowerLimit    = 0.5m,
                SpecCalcStartTime = new DateTime(2024, 1, 1),
                SpecCalcEndTime   = new DateTime(2024, 12, 31)
            };
            long unitId = _anomalyUnitRepo.Insert(unit);
            Console.WriteLine($"  [Insert] AnomalyUnit ID = {unitId}");

            var getUnit = _anomalyUnitRepo.GetById(unitId);
            Console.WriteLine($"  [GetById] UnitId={getUnit?.UnitId}, Value={getUnit?.DetectionValue}");

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
                LotsInfoId = 10004, DetectionMethodId = 1,
                SpecCalcStartTime = new DateTime(2024, 1, 1), SpecCalcEndTime = new DateTime(2024, 12, 31)
            };
            long lotId = _anomalyLotRepo.Insert(lot);

            var mapping = new AnomalyLotProcessMapping
            {
                AnomalyLotId = lotId,
                StationName  = "Diffusion",
                EquipmentId  = "EQP-D-001",
                ProcessTime  = new DateTime(2024, 3, 15, 8, 30, 0)
            };
            long mappingId = _lotProcessRepo.Insert(mapping);
            Console.WriteLine($"  [Insert] Mapping ID = {mappingId}");

            var getMapping = _lotProcessRepo.GetById(mappingId);
            Console.WriteLine($"  [GetById] Station={getMapping?.StationName}, Equip={getMapping?.EquipmentId}");

            if (getMapping != null)
            {
                getMapping.EquipmentId = "EQP-D-002";
                Console.WriteLine($"  [Update] 結果: {_lotProcessRepo.Update(getMapping)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_lotProcessRepo.Delete(mappingId)}");
            _anomalyLotRepo.Delete(lotId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. Unit Process Mapping
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoAnomalyUnitProcessMapping()
        {
            PrintSection("6. AnomalyUnitProcessMapping（Unit 製程 Boat）CRUD");

            var lot = new AnomalyLot
            {
                LotsInfoId = 10005, DetectionMethodId = 3,
                SpecCalcStartTime = new DateTime(2024, 1, 1), SpecCalcEndTime = new DateTime(2024, 12, 31)
            };
            long lotId = _anomalyLotRepo.Insert(lot);
            var ti = new AnomalyTestItem
            {
                AnomalyLotId = lotId, TestItemName = "Idsat",
                SpecCalcStartTime = new DateTime(2024, 1, 1), SpecCalcEndTime = new DateTime(2024, 12, 31)
            };
            long tiId = _anomalyTestItemRepo.Insert(ti);
            var au = new AnomalyUnit
            {
                AnomalyTestItemId = tiId, UnitId = "W01-D05",
                SpecCalcStartTime = new DateTime(2024, 1, 1), SpecCalcEndTime = new DateTime(2024, 12, 31)
            };
            long auId = _anomalyUnitRepo.Insert(au);

            var upMapping = new AnomalyUnitProcessMapping
            {
                AnomalyUnitId = auId,
                BoatId        = "BOAT-A-01",
                PositionX     = 3,
                PositionY     = 7,
                ProcessTime   = new DateTime(2024, 3, 15, 9, 0, 0),
                StationName   = "Diffusion",
                EquipmentId   = "EQP-D-001"
            };
            long upId = _unitProcessRepo.Insert(upMapping);
            Console.WriteLine($"  [Insert] UnitProcessMapping ID = {upId}");

            var getUp = _unitProcessRepo.GetById(upId);
            Console.WriteLine($"  [GetById] BoatId={getUp?.BoatId}, X={getUp?.PositionX}, Y={getUp?.PositionY}");

            if (getUp != null)
            {
                getUp.BoatId = "BOAT-A-02";
                Console.WriteLine($"  [Update] 結果: {_unitProcessRepo.Update(getUp)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_unitProcessRepo.Delete(upId)}");
            _anomalyUnitRepo.Delete(auId);
            _anomalyTestItemRepo.Delete(tiId);
            _anomalyLotRepo.Delete(lotId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. Spec 規格表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoDetectionSpec()
        {
            PrintSection("7. DetectionSpec（Spec 規格）CRUD");

            var spec = new DetectionSpec
            {
                Program             = "PROD-A",
                TestItemName        = "Vth",
                SiteId              = 1,
                DetectionMethodId   = 2, // STD
                SpecUpperLimit      = 1.5m,
                SpecLowerLimit      = 0.5m,
                SpecCalcStartTime   = new DateTime(2024, 1, 1),
                SpecCalcEndTime     = new DateTime(2024, 12, 31)
            };
            long specId = _detectionSpecRepo.Insert(spec);
            Console.WriteLine($"  [Insert] DetectionSpec ID = {specId}");

            var getSpec = _detectionSpecRepo.GetById(specId);
            Console.WriteLine($"  [GetById] Program={getSpec?.Program}, Item={getSpec?.TestItemName}");

            var byProgramMethod = _detectionSpecRepo.GetByProgramAndMethod("PROD-A", 2);
            int cnt = 0; foreach (var x in byProgramMethod) cnt++;
            Console.WriteLine($"  [GetByProgramAndMethod] 找到 {cnt} 筆");

            if (getSpec != null)
            {
                getSpec.SpecUpperLimit = 1.8m;
                Console.WriteLine($"  [Update] 結果: {_detectionSpecRepo.Update(getSpec)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_detectionSpecRepo.Delete(specId)}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. Site 測項統計值表
        // ─────────────────────────────────────────────────────────────────────
        private static void DemoSiteTestStatistic()
        {
            PrintSection("8. SiteTestStatistic（Site 統計值）CRUD");

            var stat = new SiteTestStatistic
            {
                LotsInfoId   = 10006,
                Program      = "PROD-A",
                SiteId       = 1,
                TestItemName = "Vth",
                MeanValue    = 1.012m,
                MaxValue     = 1.098m,
                MinValue     = 0.934m,
                StdValue     = 0.041m,
                CpValue      = 1.23m,
                CpkValue     = 1.18m,
                TesterId     = "TESTER-01"
            };
            long statId = _siteStatRepo.Insert(stat);
            Console.WriteLine($"  [Insert] SiteTestStatistic ID = {statId}");

            var getStat = _siteStatRepo.GetById(statId);
            Console.WriteLine($"  [GetById] Program={getStat?.Program}, Mean={getStat?.MeanValue}");

            var bySite = _siteStatRepo.GetBySiteAndItem(1, "Vth");
            int cnt = 0; foreach (var x in bySite) cnt++;
            Console.WriteLine($"  [GetBySiteAndItem] 找到 {cnt} 筆");

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
                LotsInfoId          = 10007,
                DetectionMethodId   = 1, // YIELD
                SpecUpperLimit      = 0.99m,
                SpecLowerLimit      = 0.95m,
                SpecCalcStartTime   = new DateTime(2024, 1, 1),
                SpecCalcEndTime     = new DateTime(2024, 3, 31)
            };
            long goodLotId = _goodLotRepo.Insert(goodLot);
            Console.WriteLine($"  [Insert] GoodLot ID = {goodLotId}");

            var getGoodLot = _goodLotRepo.GetById(goodLotId);
            Console.WriteLine($"  [GetById] LotsInfoId={getGoodLot?.LotsInfoId}, MethodId={getGoodLot?.DetectionMethodId}");

            var byLots = _goodLotRepo.GetByLotsInfoId(10007);
            int cnt = 0; foreach (var x in byLots) cnt++;
            Console.WriteLine($"  [GetByLotsInfoId] 找到 {cnt} 筆");

            if (getGoodLot != null)
            {
                getGoodLot.SpecUpperLimit = 0.999m;
                Console.WriteLine($"  [Update] 結果: {_goodLotRepo.Update(getGoodLot)}");
            }

            Console.WriteLine($"  [Delete] 結果: {_goodLotRepo.Delete(goodLotId)}");
        }

        // ─────────────────────────────────────────────────────────────────────
        private static void PrintSection(string title)
        {
            Console.WriteLine($"\n--- {title} ---");
        }
    }
}

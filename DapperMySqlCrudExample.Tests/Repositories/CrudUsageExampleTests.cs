using System;
using System.Data;
using System.Linq;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;
using DapperMySqlCrudExample.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DapperMySqlCrudExample.Tests.Repositories
{
    /// <summary>
    /// 跨 Repository 的一般 CRUD 使用範例測試。
    /// <para>
    /// 標記 <c>[TestCategory("Unit")]</c> 的測試驗證模型與建構行為，不需資料庫。<br/>
    /// 標記 <c>[TestCategory("Integration")]</c> 的測試需要真實 MySQL 連線；
    /// CI 階段請以 <c>--filter "TestCategory!=Integration"</c> 篩選排除。
    /// </para>
    /// </summary>
    [TestClass]
    public class CrudUsageExampleTests
    {
        // ──────────────────────────────────────────────────────────────────
        // 單元測試：模型行為 / 建構驗證
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// 驗證 AnomalyLot 模型屬性可正確讀寫。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void AnomalyLot_PropertyAssignment_RoundTrips()
        {
            var now = new DateTime(2024, 3, 15);
            var model = new AnomalyLot
            {
                Id = 100L,
                LotsInfoId = 5,
                DetectionMethodId = 1,
                SpecUpperLimit = 10.5m,
                SpecLowerLimit = 5.5m,
                SpecCalcStartTime = now,
                SpecCalcEndTime = now.AddDays(1),
                CreatedAt = now,
                UpdatedAt = now,
            };

            model.Id.Should().Be(100L);
            model.LotsInfoId.Should().Be(5);
            model.DetectionMethodId.Should().Be(1);
            model.SpecUpperLimit.Should().Be(10.5m);
            model.SpecLowerLimit.Should().Be(5.5m);
            model.CreatedAt.Should().Be(now);
        }

        /// <summary>
        /// 驗證 GoodLot 模型屬性可正確讀寫。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void GoodLot_PropertyAssignment_RoundTrips()
        {
            var now = new DateTime(2024, 5, 20);
            var model = new GoodLot
            {
                Id = 50L,
                LotsInfoId = 3,
                DetectionMethodId = 2,
                SpecUpperLimit = 20.0m,
                SpecLowerLimit = 10.0m,
                SpecCalcStartTime = now,
                CreatedAt = now,
                UpdatedAt = now,
            };

            model.Id.Should().Be(50L);
            model.LotsInfoId.Should().Be(3);
            model.DetectionMethodId.Should().Be(2);
            model.SpecUpperLimit.Should().Be(20.0m);
            model.CreatedAt.Should().Be(now);
        }

        /// <summary>
        /// 驗證所有 Repository 均可透過 <see cref="MockDbConnectionFactory"/> 正常建構，
        /// 不需資料庫連線。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void AllRepositories_CanBeConstructed_WithMockFactory()
        {
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);

            Action[] constructions = new Action[]
            {
                () => new DetectionMethodRepository(factory),
                () => new DetectionSpecRepository(factory),
                () => new SiteTestStatisticRepository(factory),
                () => new GoodLotRepository(factory),
                () => new AnomalyLotRepository(factory),
                () => new AnomalyLotProcessMappingRepository(factory),
                () => new AnomalyUnitRepository(factory),
                () => new AnomalyUnitProcessMappingRepository(factory),
                () => new AnomalyTestItemRepository(factory),
            };

            foreach (var construction in constructions)
                construction.Should().NotThrow();
        }

        /// <summary>
        /// 驗證所有 Repository 在傳入 null factory 時都會 fail fast。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void AllRepositories_WithNullFactory_ThrowArgumentNullException()
        {
            Action[] constructions = new Action[]
            {
                () => new DetectionMethodRepository(null),
                () => new DetectionSpecRepository(null),
                () => new SiteTestStatisticRepository(null),
                () => new GoodLotRepository(null),
                () => new AnomalyLotRepository(null),
                () => new AnomalyLotProcessMappingRepository(null),
                () => new AnomalyUnitRepository(null),
                () => new AnomalyUnitProcessMappingRepository(null),
                () => new AnomalyTestItemRepository(null),
            };

            foreach (var construction in constructions)
                construction.Should()
                    .Throw<ArgumentNullException>()
                    .Which.ParamName.Should()
                    .Be("factory");
        }

        /// <summary>
        /// 驗證各 Repository 介面均有正確實作型別（介面-實作對應健全性）。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Repositories_ImplementCorrectInterfaces()
        {
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);

            ((IDetectionMethodRepository)new DetectionMethodRepository(factory))
                .Should()
                .NotBeNull();
            ((IDetectionSpecRepository)new DetectionSpecRepository(factory)).Should().NotBeNull();
            ((ISiteTestStatisticRepository)new SiteTestStatisticRepository(factory))
                .Should()
                .NotBeNull();
            ((IGoodLotRepository)new GoodLotRepository(factory)).Should().NotBeNull();
            ((IAnomalyLotRepository)new AnomalyLotRepository(factory)).Should().NotBeNull();
            ((IAnomalyLotProcessMappingRepository)new AnomalyLotProcessMappingRepository(factory))
                .Should()
                .NotBeNull();
            ((IAnomalyUnitRepository)new AnomalyUnitRepository(factory)).Should().NotBeNull();
            ((IAnomalyUnitProcessMappingRepository)new AnomalyUnitProcessMappingRepository(factory))
                .Should()
                .NotBeNull();
            ((IAnomalyTestItemRepository)new AnomalyTestItemRepository(factory))
                .Should()
                .NotBeNull();
        }

        // ──────────────────────────────────────────────────────────────────
        // 整合測試：需要真實 MySQL 連線（CI 排除，本機驗證）
        // ──────────────────────────────────────────────────────────────────

        private static IDbConnectionFactory CreateLiveFactory()
        {
            var connStr = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException(
                    "整合測試需設定環境變數 MYSQL_CONNECTION_STRING，格式："
                        + "Server=localhost;Database=your_db;Uid=root;Pwd=yourpw;"
                );

            return new LiveDbConnectionFactory(connStr);
        }

        /// <summary>
        /// 整合測試：AnomalyLot 完整 CRUD 流程。
        /// Insert → GetById → Update → Exists → GetCount → GetPaged → Delete。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void AnomalyLot_FullCrud_Integration()
        {
            var repo = new AnomalyLotRepository(CreateLiveFactory());

            // LotsInfoId 外鍵需指向實際存在的 lots_info 記錄
            var entity = new AnomalyLot
            {
                LotsInfoId = 1,
                DetectionMethodId = 1,
                SpecUpperLimit = 15.0m,
                SpecLowerLimit = 5.0m,
            };

            long newId = repo.Insert(entity);
            newId.Should().BeGreaterThan(0, "Insert 應回傳新 PK");

            try
            {
                // GetById
                var fetched = repo.GetById(newId);
                fetched.Should().NotBeNull();
                fetched.LotsInfoId.Should().Be(1);
                fetched.SpecUpperLimit.Should().Be(15.0m);

                // Update
                fetched.SpecUpperLimit = 20.0m;
                bool updated = repo.Update(fetched);
                updated.Should().BeTrue();
                repo.GetById(newId).SpecUpperLimit.Should().Be(20.0m);

                // Exists
                repo.Exists(newId).Should().BeTrue();

                // GetCount
                repo.GetCount().Should().BeGreaterOrEqualTo(1);

                // GetPaged
                var page = repo.GetPaged(0, 50).ToList();
                page.Should().Contain(x => x.Id == newId);
            }
            finally
            {
                repo.Delete(newId).Should().BeTrue();
                repo.Exists(newId).Should().BeFalse();
            }
        }

        /// <summary>
        /// 整合測試：GoodLot 完整 CRUD 流程。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void GoodLot_FullCrud_Integration()
        {
            var repo = new GoodLotRepository(CreateLiveFactory());

            // LotsInfoId 外鍵需指向實際存在的 lots_info 記錄
            var entity = new GoodLot
            {
                LotsInfoId = 1,
                DetectionMethodId = 1,
                SpecUpperLimit = 30.0m,
                SpecLowerLimit = 10.0m,
            };

            long newId = repo.Insert(entity);
            newId.Should().BeGreaterThan(0);

            try
            {
                var fetched = repo.GetById(newId);
                fetched.Should().NotBeNull();
                fetched.SpecUpperLimit.Should().Be(30.0m);

                fetched.SpecUpperLimit = 35.0m;
                repo.Update(fetched).Should().BeTrue();
                repo.GetById(newId).SpecUpperLimit.Should().Be(35.0m);

                repo.Exists(newId).Should().BeTrue();
            }
            finally
            {
                repo.Delete(newId).Should().BeTrue();
            }
        }

        /// <summary>
        /// 整合測試：Transaction 交易寫入後回滾，驗證資料不存在。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void Transaction_Rollback_LeavesNoData()
        {
            var factory = CreateLiveFactory();
            var goodRepo = new GoodLotRepository(factory);

            using (var conn = factory.Create())
            using (var tx = conn.BeginTransaction())
            {
                // 在交易內直接使用原始 Dapper 呼叫插入一筆 GoodLot
                var entity = new GoodLot
                {
                    LotsInfoId = 1,
                    DetectionMethodId = 1,
                    SpecUpperLimit = 99.9m,
                };

                long newId = conn.ExecuteDapper(entity, tx);
                newId.Should().BeGreaterThan(0);

                // 不 commit，直接 rollback
                tx.Rollback();

                // 交易外確認資料不存在
                goodRepo.Exists(newId).Should().BeFalse("回滾後記錄不應存在");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // 整合測試：SiteTestStatistic 含 start_time 欄位
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// 整合測試：SiteTestStatistic 含可空 StartTime 欄位的 CRUD。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void SiteTestStatistic_WithStartTime_FullCrud_Integration()
        {
            var repo = new SiteTestStatisticRepository(CreateLiveFactory());

            var startTime = new DateTime(2024, 1, 1);
            var entity = new SiteTestStatistic
            {
                LotsInfoId = 1,
                Program = "TEST_PROG",
                SiteId = 1,
                TestItemName = "VCC",
                MeanValue = 3.3m,
                StartTime = startTime,
            };

            long newId = repo.Insert(entity);
            newId.Should().BeGreaterThan(0);

            try
            {
                var fetched = repo.GetById(newId);
                fetched.Should().NotBeNull();
                fetched.StartTime.Should().Be(startTime);
                fetched.MeanValue.Should().Be(3.3m);
            }
            finally
            {
                repo.Delete(newId);
            }
        }
    }

    /// <summary>
    /// 測試輔助用內部靜態類別（避免污染主流程）。
    /// 由於 Dapper 擴充方法難以透過 Moq 攔截，此處提供一個輔助方法
    /// 讓 Transaction Rollback 整合測試可以在不依賴完整 DapperExtensions 的情況下插入資料。
    /// </summary>
    internal static class TestDapperHelper
    {
        /// <summary>
        /// 於指定交易內插入一筆 GoodLot，使用原始 Dapper 呼叫，回傳新 PK。
        /// </summary>
        internal static long ExecuteDapper(
            this IDbConnection conn,
            GoodLot entity,
            IDbTransaction transaction
        )
        {
            const string sql =
                @"
                INSERT INTO good_lots
                    (lots_info_id, detection_method_id, spec_upper_limit, spec_lower_limit)
                VALUES
                    (@LotsInfoId, @DetectionMethodId, @SpecUpperLimit, @SpecLowerLimit);
                SELECT LAST_INSERT_ID();";

            return Dapper.SqlMapper.ExecuteScalar<long>(conn, sql, entity, transaction);
        }
    }
}

using System;
using System.Collections.Generic;
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
    /// <see cref="DetectionMethodRepository"/> 的單元 / 整合測試。
    /// <para>
    /// 標記 <c>[TestCategory("Unit")]</c> 的測試不需要資料庫連線，可在 CI 中直接執行。<br/>
    /// 標記 <c>[TestCategory("Integration")]</c> 的測試需要真實 MySQL 連線，本機驗證用；
    /// CI 階段請以 <c>--filter "TestCategory!=Integration"</c> 篩選排除。
    /// </para>
    /// </summary>
    [TestClass]
    public class DetectionMethodRepositoryTests
    {
        // ──────────────────────────────────────────────────────────────────
        // 單元測試：不需資料庫
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// 驗證 Repository 可透過標準建構子正確初始化。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithValidFactory_DoesNotThrow()
        {
            // Arrange
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);

            // Act
            Action act = () => new DetectionMethodRepository(factory);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// 驗證 Repository 實作 <see cref="IDetectionMethodRepository"/> 介面。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Repository_ImplementsInterface()
        {
            // Arrange
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);

            // Act
            var repo = new DetectionMethodRepository(factory);

            // Assert
            repo.Should().BeAssignableTo<IDetectionMethodRepository>();
        }

        /// <summary>
        /// 驗證 DetectionMethod Model 屬性預設值與型別符合預期。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void DetectionMethod_DefaultValues_AreCorrect()
        {
            // Act
            var model = new DetectionMethod();

            // Assert
            model.Id.Should().Be(0);
            model.MethodCode.Should().BeNull();
            model.MethodName.Should().BeNull();
            model.HasTestItem.Should().BeFalse();
            model.HasUnitLevel.Should().BeFalse();
            model.CreatedAt.Should().Be(default(DateTime));
            model.UpdatedAt.Should().Be(default(DateTime));
        }

        /// <summary>
        /// 驗證 DetectionMethod Model 屬性賦值正確。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void DetectionMethod_PropertyAssignment_RoundTrips()
        {
            // Arrange
            var now = new DateTime(2024, 6, 1, 12, 0, 0);
            var model = new DetectionMethod
            {
                Id = 3,
                MethodCode = "SITE_MEAN",
                MethodName = "Site 平均法",
                HasTestItem = true,
                HasUnitLevel = false,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // Assert
            model.Id.Should().Be(3);
            model.MethodCode.Should().Be("SITE_MEAN");
            model.MethodName.Should().Be("Site 平均法");
            model.HasTestItem.Should().BeTrue();
            model.HasUnitLevel.Should().BeFalse();
            model.CreatedAt.Should().Be(now);
            model.UpdatedAt.Should().Be(now);
        }

        /// <summary>
        /// 驗證建立 Repository 時傳入 null factory 的行為是否明確（防呆）。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new DetectionMethodRepository(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("factory");
        }

        /// <summary>
        /// 驗證 Insert 在 entity 為 null 時會主動拋出明確例外。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Insert_WithNullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);
            var repo = new DetectionMethodRepository(factory);

            // Act
            Action act = () => repo.Insert(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("entity");
        }

        /// <summary>
        /// 驗證 Update 在 entity 為 null 時會主動拋出明確例外。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Update_WithNullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);
            var repo = new DetectionMethodRepository(factory);

            // Act
            Action act = () => repo.Update(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("entity");
        }

        /// <summary>
        /// 驗證分頁參數不合法時會及早失敗，避免送出無效 SQL。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void GetPaged_WithInvalidArguments_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var mockConn = new Mock<IDbConnection>();
            var factory = new MockDbConnectionFactory(mockConn.Object);
            var repo = new DetectionMethodRepository(factory);

            // Act
            Action negativeOffset = () => repo.GetPaged(-1, 10).ToList();
            Action nonPositiveLimit = () => repo.GetPaged(0, 0).ToList();

            // Assert
            negativeOffset.Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which.ParamName.Should()
                .Be("offset");
            nonPositiveLimit.Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which.ParamName.Should()
                .Be("limit");
        }

        // ──────────────────────────────────────────────────────────────────
        // 整合測試：需要真實 MySQL 連線（CI 排除，本機驗證）
        // ──────────────────────────────────────────────────────────────────

        private static IDbConnectionFactory CreateLiveFactory()
        {
            // 優先讀環境變數，否則採用 App.config DefaultConnection。
            // App.config 的連線字串需在測試專案的 App.config 中設定。
            var connStr = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException(
                    "整合測試需設定環境變數 MYSQL_CONNECTION_STRING，格式："
                        + "Server=localhost;Database=test_db;Uid=root;Pwd=yourpw;"
                );

            return new LiveDbConnectionFactory(connStr);
        }

        /// <summary>
        /// 整合測試：GetAll 應回傳非 null 集合（資料表可包含零筆）。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void GetAll_WithLiveDb_ReturnsNonNullCollection()
        {
            var repo = new DetectionMethodRepository(CreateLiveFactory());
            var result = repo.GetAll();
            result.Should().NotBeNull();
        }

        /// <summary>
        /// 整合測試：完整 CRUD 流程（Insert → GetById → Update → Exists → Delete → 驗消失）。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void CRUD_FullCycle_WithLiveDb()
        {
            var repo = new DetectionMethodRepository(CreateLiveFactory());

            // Insert
            var entity = new DetectionMethod
            {
                MethodCode = $"TEST_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                MethodName = "整合測試用方法",
                HasTestItem = true,
                HasUnitLevel = false,
            };
            byte newId = repo.Insert(entity);
            newId.Should().BeGreaterThan(0, "Insert 應回傳新 PK");

            try
            {
                // GetById
                var fetched = repo.GetById(newId);
                fetched.Should().NotBeNull();
                fetched.MethodCode.Should().Be(entity.MethodCode);
                fetched.HasTestItem.Should().BeTrue();

                // Update
                fetched.MethodName = "更新後名稱";
                bool updated = repo.Update(fetched);
                updated.Should().BeTrue();

                var afterUpdate = repo.GetById(newId);
                afterUpdate.MethodName.Should().Be("更新後名稱");

                // Exists
                repo.Exists(newId).Should().BeTrue();

                // GetCount
                repo.GetCount().Should().BeGreaterOrEqualTo(1);

                // GetPaged
                var paged = repo.GetPaged(0, 10).ToList();
                paged.Should().NotBeNull();
                paged.Count.Should().BeGreaterOrEqualTo(1);
            }
            finally
            {
                // Delete（確保清理）
                bool deleted = repo.Delete(newId);
                deleted.Should().BeTrue();

                // 確認消失
                repo.Exists(newId).Should().BeFalse();
                repo.GetById(newId).Should().BeNull();
            }
        }

        /// <summary>
        /// 整合測試：GetByCode 依方法代碼查詢。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void GetByCode_WithLiveDb_ReturnsMatchingEntity()
        {
            var repo = new DetectionMethodRepository(CreateLiveFactory());

            // 先插入，再查詢
            var code = $"CODE_{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            var entity = new DetectionMethod
            {
                MethodCode = code,
                MethodName = "GetByCode 測試",
                HasTestItem = false,
                HasUnitLevel = true,
            };
            byte newId = repo.Insert(entity);

            try
            {
                var fetched = repo.GetByCode(code);
                fetched.Should().NotBeNull();
                fetched.MethodCode.Should().Be(code);
                fetched.HasUnitLevel.Should().BeTrue();
            }
            finally
            {
                repo.Delete(newId);
            }
        }

        /// <summary>
        /// 整合測試：GetPaged 分頁偏移正確。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void GetPaged_WithOffsetBeyondCount_ReturnsEmpty()
        {
            var repo = new DetectionMethodRepository(CreateLiveFactory());
            var result = repo.GetPaged(99999, 10);
            result.Should().NotBeNull().And.BeEmpty("偏移超過記錄數時應回傳空集合");
        }

        /// <summary>
        /// 整合測試：Delete 不存在的 ID 應回傳 false。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void Delete_NonExistentId_ReturnsFalse()
        {
            var repo = new DetectionMethodRepository(CreateLiveFactory());
            bool result = repo.Delete(byte.MaxValue); // 255，假設不存在
            result.Should().BeFalse("刪除不存在記錄應回傳 false");
        }
    }
}

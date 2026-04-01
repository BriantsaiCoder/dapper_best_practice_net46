using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Repositories;
using DapperMySqlCrudExample.Services;
using DapperMySqlCrudExample.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DapperMySqlCrudExample.Tests.Services
{
    /// <summary>
    /// <see cref="DetectionSpecService"/> 的單元 / 整合測試。
    /// </summary>
    [TestClass]
    public class DetectionSpecServiceTests
    {
        /// <summary>
        /// 驗證建構子在 factory 為 null 時主動拋出明確例外。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var repository = new Mock<IDetectionSpecRepository>();

            // Act
            Action act = () => new DetectionSpecService(null, repository.Object);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be("factory");
        }

        /// <summary>
        /// 驗證建構子在 repository 為 null 時主動拋出明確例外。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Arrange
            var factory = new Mock<IDbConnectionFactory>();

            // Act
            Action act = () => new DetectionSpecService(factory.Object, null);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be("specRepo");
        }

        /// <summary>
        /// 驗證 programName 為空白時會在進入資料庫前被拒絕。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ComputeAndInsertSiteMeanSpec_WithBlankProgramName_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();

            // Act
            Action act = () => service.ComputeAndInsertSiteMeanSpec("   ", 1, "IDDQ");

            // Assert
            act.Should()
                .Throw<ArgumentException>()
                .Which.ParamName.Should()
                .Be("programName");
        }

        /// <summary>
        /// 驗證 testItemName 為空白時會在進入資料庫前被拒絕。
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ComputeAndInsertSiteMeanSpec_WithBlankTestItemName_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();

            // Act
            Action act = () => service.ComputeAndInsertSiteMeanSpec("FT_PROG", 1, "");

            // Assert
            act.Should()
                .Throw<ArgumentException>()
                .Which.ParamName.Should()
                .Be("testItemName");
        }

        /// <summary>
        /// 整合測試：驗證 service 會依歷史資料計算 Mean / Std，並寫入 detection_specs。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void ComputeAndInsertSiteMeanSpec_WithLiveDb_ComputesAndPersistsExpectedValues()
        {
            var factory = CreateLiveFactory();
            var repository = new DetectionSpecRepository(factory);
            var service = new DetectionSpecService(factory, repository);
            var context = CreateIntegrationContext(factory);

            try
            {
                long specId = service.ComputeAndInsertSiteMeanSpec(
                    context.ProgramName,
                    context.SiteId,
                    context.TestItemName
                );

                var created = repository.GetById(specId);
                created.Should().NotBeNull();
                created.Program.Should().Be(context.ProgramName);
                created.TestItemName.Should().Be(context.TestItemName);
                created.SiteId.Should().Be(context.SiteId);
                created.SpecCalcMean.Should().Be(15m);
                created.SpecCalcStartTime.Should().Be(context.FirstStartTime);
                created.SpecCalcEndTime.Should().Be(context.SecondStartTime);

                var expectedStd = Math.Sqrt(50d);
                Convert.ToDouble(created.SpecCalcStd.Value).Should().BeApproximately(expectedStd, 0.000000001d);
                Convert.ToDouble(created.SpecUpperLimit.Value)
                    .Should()
                    .BeApproximately(15d + 6d * expectedStd, 0.000000001d);
                Convert.ToDouble(created.SpecLowerLimit.Value)
                    .Should()
                    .BeApproximately(15d - 6d * expectedStd, 0.000000001d);
            }
            finally
            {
                CleanupIntegrationContext(factory, context);
            }
        }

        /// <summary>
        /// 整合測試：若 Repository 在交易中插入後拋錯，service 應回滾 detection_specs 寫入。
        /// </summary>
        [TestMethod]
        [TestCategory("Integration")]
        [Ignore("需要真實 MySQL 連線；請設定 MYSQL_CONNECTION_STRING 環境變數後手動執行。")]
        public void ComputeAndInsertSiteMeanSpec_WhenRepositoryInsertFails_RollsBackInsertedSpec()
        {
            var factory = CreateLiveFactory();
            var context = CreateIntegrationContext(factory);
            var service = new DetectionSpecService(factory, new ThrowingInsertDetectionSpecRepository());

            try
            {
                Action act = () => service.ComputeAndInsertSiteMeanSpec(
                    context.ProgramName,
                    context.SiteId,
                    context.TestItemName
                );

                act.Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("模擬 DetectionSpecRepository.Insert 失敗。");

                CountDetectionSpecs(
                    factory,
                    context.ProgramName,
                    context.SiteId,
                    context.TestItemName
                ).Should().Be(0);
            }
            finally
            {
                CleanupIntegrationContext(factory, context);
            }
        }

        private static DetectionSpecService CreateService()
        {
            var factory = new Mock<IDbConnectionFactory>();
            var repository = new Mock<IDetectionSpecRepository>();
            return new DetectionSpecService(factory.Object, repository.Object);
        }

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

        private static DetectionSpecIntegrationContext CreateIntegrationContext(
            IDbConnectionFactory factory
        )
        {
            var context = new DetectionSpecIntegrationContext
            {
                ProgramName = $"SPEC_ITG_{Guid.NewGuid():N}".Substring(0, 17).ToUpperInvariant(),
                TestItemName = $"ITEM_{Guid.NewGuid():N}".Substring(0, 13).ToUpperInvariant(),
                SiteId = 7,
                FirstStartTime = new DateTime(2024, 1, 1, 8, 0, 0),
                SecondStartTime = new DateTime(2024, 1, 2, 8, 0, 0),
            };

            using (var connection = factory.Create())
            {
                context.FirstLotId = InsertLotsInfo(connection, context.ProgramName);
                context.SecondLotId = InsertLotsInfo(connection, context.ProgramName);

                InsertSiteTestStatistic(
                    connection,
                    context.FirstLotId,
                    context.ProgramName,
                    context.SiteId,
                    context.TestItemName,
                    10m,
                    context.FirstStartTime
                );
                InsertSiteTestStatistic(
                    connection,
                    context.SecondLotId,
                    context.ProgramName,
                    context.SiteId,
                    context.TestItemName,
                    20m,
                    context.SecondStartTime
                );
            }

            return context;
        }

        private static int InsertLotsInfo(IDbConnection connection, string programName)
        {
            const string sql =
                @"INSERT INTO lots_info (file_name, program)
                  VALUES (@FileName, @ProgramName);
                  SELECT LAST_INSERT_ID();";

            return connection.ExecuteScalar<int>(
                sql,
                new
                {
                    FileName = $"spec-itg-{Guid.NewGuid():N}.csv",
                    ProgramName = programName,
                }
            );
        }

        private static void InsertSiteTestStatistic(
            IDbConnection connection,
            int lotsInfoId,
            string programName,
            uint siteId,
            string testItemName,
            decimal meanValue,
            DateTime startTime
        )
        {
            const string sql =
                @"INSERT INTO site_test_statistics
                      (lots_info_id, program, site_id, test_item_name, mean_value, start_time, end_time)
                  VALUES
                      (@LotsInfoId, @ProgramName, @SiteId, @TestItemName, @MeanValue, @StartTime, @EndTime);";

            connection.Execute(
                sql,
                new
                {
                    LotsInfoId = lotsInfoId,
                    ProgramName = programName,
                    SiteId = siteId,
                    TestItemName = testItemName,
                    MeanValue = meanValue,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(1),
                }
            );
        }

        private static int CountDetectionSpecs(
            IDbConnectionFactory factory,
            string programName,
            uint siteId,
            string testItemName
        )
        {
            const string sql =
                @"SELECT COUNT(1)
                  FROM detection_specs
                  WHERE program = @ProgramName
                    AND site_id = @SiteId
                    AND test_item_name = @TestItemName";

            using (var connection = factory.Create())
                return connection.ExecuteScalar<int>(
                    sql,
                    new
                    {
                        ProgramName = programName,
                        SiteId = siteId,
                        TestItemName = testItemName,
                    }
                );
        }

        private static void CleanupIntegrationContext(
            IDbConnectionFactory factory,
            DetectionSpecIntegrationContext context
        )
        {
            using (var connection = factory.Create())
            {
                connection.Execute(
                    @"DELETE FROM detection_specs
                      WHERE program = @ProgramName
                        AND site_id = @SiteId
                        AND test_item_name = @TestItemName",
                    new
                    {
                        ProgramName = context.ProgramName,
                        SiteId = context.SiteId,
                        TestItemName = context.TestItemName,
                    }
                );

                connection.Execute(
                    "DELETE FROM lots_info WHERE id IN @Ids",
                    new { Ids = new[] { context.FirstLotId, context.SecondLotId } }
                );
            }
        }

        private sealed class DetectionSpecIntegrationContext
        {
            public int FirstLotId { get; set; }

            public int SecondLotId { get; set; }

            public string ProgramName { get; set; }

            public uint SiteId { get; set; }

            public string TestItemName { get; set; }

            public DateTime FirstStartTime { get; set; }

            public DateTime SecondStartTime { get; set; }
        }

        private sealed class ThrowingInsertDetectionSpecRepository : IDetectionSpecRepository
        {
            public IEnumerable<DetectionSpec> GetAll()
            {
                throw new NotSupportedException();
            }

            public DetectionSpec GetById(long id)
            {
                throw new NotSupportedException();
            }

            public IEnumerable<DetectionSpec> GetByProgramAndMethod(
                string program,
                byte detectionMethodId
            )
            {
                throw new NotSupportedException();
            }

            public IEnumerable<DetectionSpec> GetRecentByProgramAndMethodName(
                string program,
                string detectionMethodName
            )
            {
                throw new NotSupportedException();
            }

            public DetectionSpec GetLatestByProgramAndMethodName(
                string program,
                string detectionMethodName
            )
            {
                throw new NotSupportedException();
            }

            public long Insert(DetectionSpec entity, IDbTransaction transaction = null)
            {
                if (transaction == null)
                    throw new InvalidOperationException("測試預期 transaction 不可為 null。");

                var repository = new DetectionSpecRepository(
                    new MockDbConnectionFactory(transaction.Connection)
                );
                repository.Insert(entity, transaction);

                throw new InvalidOperationException("模擬 DetectionSpecRepository.Insert 失敗。");
            }

            public bool Update(DetectionSpec entity, IDbTransaction transaction = null)
            {
                throw new NotSupportedException();
            }

            public bool Delete(long id, IDbTransaction transaction = null)
            {
                throw new NotSupportedException();
            }

            public bool Exists(long id)
            {
                throw new NotSupportedException();
            }

            public int GetCount()
            {
                throw new NotSupportedException();
            }

            public IEnumerable<DetectionSpec> GetPaged(int offset, int limit)
            {
                throw new NotSupportedException();
            }
        }
    }
}

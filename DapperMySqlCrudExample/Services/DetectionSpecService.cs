using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DapperMySqlCrudExample.Infrastructure;
using DapperMySqlCrudExample.Models;
using DapperMySqlCrudExample.Models.QueryModels;
using DapperMySqlCrudExample.Repositories;
using MathNet.Numerics.Statistics;
using NLog;

namespace DapperMySqlCrudExample.Services
{
    /// <summary>
    /// DetectionSpec 業務邏輯服務。
    /// 負責 SITE_MEAN 規格的統計計算與寫入編排，
    /// Repository 僅負責 SQL CRUD，計算邏輯集中於此。
    /// </summary>
    /// <remarks>
    /// 【新手導讀】Repository vs Service 的職責分工：
    /// - Repository：純粹的資料存取層，只負責單一資料表的 CRUD，不包含業務邏輯。
    /// - Service：業務邏輯層，負責編排（orchestrate）多個 Repository 的操作、管理交易、執行計算。
    /// 例如本類別需要同時操作 SiteTestStatisticRepo、DetectionMethodRepo、DetectionSpecRepo，
    /// 並在同一交易中完成「查詢→計算→寫入」的完整流程，這種跨 Repository 的協作就是 Service 的職責。
    /// </remarks>
    public sealed class DetectionSpecService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly DbConnectionFactory _factory;
        private readonly DetectionSpecRepository _detectionSpecRepo;
        private readonly SiteTestStatisticRepository _siteTestStatRepo;
        private readonly DetectionMethodRepository _detectionMethodRepo;

        private const string SiteMeanMethodKey = "SITE_MEAN";
        private const int SiteMeanHistoryMonths = 1;

        /// <summary>SITE_MEAN 計算所需的最小樣本數。樣本過少時統計量不穩定，±nσ 管制限易失真。</summary>
        private const int MinimumSampleCount = 30;

        /// <summary>管制上下限預設的標準差倍數（±6σ）。可由呼叫端依測項或客戶需求覆寫（例如 3σ、4.5σ）。</summary>
        public const double DefaultSigmaMultiplier = 6.0;

        public DetectionSpecService(
            DbConnectionFactory factory,
            DetectionSpecRepository detectionSpecRepo,
            SiteTestStatisticRepository siteTestStatRepo,
            DetectionMethodRepository detectionMethodRepo
        )
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _detectionSpecRepo =
                detectionSpecRepo ?? throw new ArgumentNullException(nameof(detectionSpecRepo));
            _siteTestStatRepo =
                siteTestStatRepo ?? throw new ArgumentNullException(nameof(siteTestStatRepo));
            _detectionMethodRepo =
                detectionMethodRepo ?? throw new ArgumentNullException(nameof(detectionMethodRepo));
        }

        /// <summary>
        /// 依歷史 site_test_statistics 資料計算 SITE_MEAN 規格並寫入 detection_specs，回傳規格主鍵。
        /// 取樣策略為取前一個月內（以 <see cref="DateTime.Now"/> 為基準）的有效資料進行統計。
        /// </summary>
        /// <param name="programName">程式名稱。</param>
        /// <param name="siteId">Site 編號。</param>
        /// <param name="testItemName">測項名稱。</param>
        /// <param name="sigmaMultiplier">
        /// 管制上下限使用的標準差倍數，預設 <see cref="DefaultSigmaMultiplier"/>（±6σ）。
        /// 不同測項或客戶要求可傳入 3、4.5 等值。
        /// </param>
        /// <remarks>
        /// 【交易範圍設計】
        /// 歷史統計查詢與計算（可能掃描大量列）刻意放在交易之外，避免長時間持有讀鎖；
        /// 僅在「重複規格檢查 + 寫入」這段短暫的區間才開啟交易，確保
        /// 「查既有規格 → 決定 Insert 或 Update」不會與其他同時執行的計算互相插隊。
        ///
        /// 【重複規格處理策略】
        /// 相同 (program, test_item_name, site_id, detection_method_id) 且計算區間
        /// (spec_calc_start_time, spec_calc_end_time) 完全相同者，視為同一份規格，
        /// 改以 Update 覆寫既有列並回傳其既有 Id，不再新增重複資料。
        ///
        /// 【Rollback 機制說明】
        /// 本方法不使用顯式 tx.Rollback()，而是依賴 using 區塊的隱式 Rollback：
        /// 若在 tx.Commit() 之前的任何一步發生例外，程式流程會跳過 Commit()，
        /// 離開 using 區塊時 tx.Dispose() 會自動 Rollback 此交易中所有未提交的操作。
        /// </remarks>
        public long ComputeAndInsertSiteMeanSpec(
            string programName,
            uint siteId,
            string testItemName,
            double sigmaMultiplier = DefaultSigmaMultiplier
        )
        {
            if (string.IsNullOrWhiteSpace(programName))
            {
                throw new ArgumentException("參數不可為 null、空字串或空白。", nameof(programName));
            }

            if (string.IsNullOrWhiteSpace(testItemName))
            {
                throw new ArgumentException(
                    "參數不可為 null、空字串或空白。",
                    nameof(testItemName)
                );
            }

            if (double.IsNaN(sigmaMultiplier) || double.IsInfinity(sigmaMultiplier) || sigmaMultiplier <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sigmaMultiplier),
                    sigmaMultiplier,
                    "標準差倍數必須為大於 0 的有限數值。"
                );
            }

            // ── 第一階段：查詢與計算（不開交易，避免長時間持有讀鎖）──────────────
            var sinceTime = DateTime.Now.AddMonths(-SiteMeanHistoryMonths);
            var rows = _siteTestStatRepo.QuerySiteMeanRows(
                programName,
                siteId,
                testItemName,
                sinceTime
            );

            if (rows.Count < MinimumSampleCount)
            {
                throw new InvalidOperationException(
                    $"site_test_statistics 中前 {SiteMeanHistoryMonths} 個月內符合條件的資料筆數不足（需要 {MinimumSampleCount} 筆，實際 {rows.Count} 筆；"
                        + $"program={programName}, siteId={siteId}, testItem={testItemName}, sinceTime={sinceTime:yyyy-MM-dd HH:mm:ss}）。"
                );
            }

            var (mean, std) = CalculateMeanAndStd(rows);
            var (ucl, lcl) = CalculateControlLimits(mean, std, sigmaMultiplier);
            var (specCalcStart, specCalcEnd) = ExtractTimeRange(rows);
            byte methodId = GetRequiredSiteMeanMethodId();

            _logger.Info(
                "SITE_MEAN 計算完成 | Program={Program}, SiteId={SiteId}, TestItem={TestItem}, SampleCount={SampleCount}, "
                    + "Mean={Mean}, Std={Std}, Sigma={Sigma}, UCL={Ucl}, LCL={Lcl}, CalcStart={CalcStart}, CalcEnd={CalcEnd}",
                programName,
                siteId,
                testItemName,
                rows.Count,
                mean,
                std,
                sigmaMultiplier,
                ucl,
                lcl,
                specCalcStart,
                specCalcEnd
            );

            var spec = BuildDetectionSpec(
                programName,
                siteId,
                testItemName,
                methodId,
                ucl,
                lcl,
                specCalcStart,
                specCalcEnd,
                mean,
                std
            );

            // ── 第二階段：寫入（短交易，涵蓋「重複檢查 → Insert/Update」）─────────
            // 【新手導讀】雙層 using 管理連線與交易的生命週期：
            // 外層 using 管理連線（conn），內層 using 管理交易（tx）。
            // 離開區塊時會依反序 Dispose：先 tx（自動 Rollback 未 Commit 的交易），再 conn（歸還連線池）。
            using (var conn = _factory.Create())
            {
                // 【新手導讀】BeginTransaction() 要求連線已開啟，因此交易場景需手動 Open()。
                conn.Open();
                using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
                {
                    try
                    {
                        var existing = _detectionSpecRepo.GetByKeyAndCalcRange(
                            programName,
                            testItemName,
                            siteId,
                            methodId,
                            specCalcStart,
                            specCalcEnd,
                            tx
                        );

                        long specId;
                        if (existing != null)
                        {
                            spec.Id = existing.Id;
                            _detectionSpecRepo.Update(spec, tx);
                            specId = existing.Id;
                            _logger.Info(
                                "已存在相同計算區間的 SITE_MEAN 規格，改為更新 | Id={Id}",
                                specId
                            );
                        }
                        else
                        {
                            specId = _detectionSpecRepo.Insert(spec, tx);
                            _logger.Info("新增 SITE_MEAN 規格 | Id={Id}", specId);
                        }

                        // ★ 只有成功走到此行，資料才會真正寫入資料庫。
                        tx.Commit();
                        return specId;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            ex,
                            "ComputeAndInsertSiteMeanSpec 寫入交易失敗（將自動 Rollback） | Program={Program}, SiteId={SiteId}, TestItem={TestItem}",
                            programName,
                            siteId,
                            testItemName
                        );

                        throw;
                    }
                }
            }
        }

        // ── 私有輔助方法 ────────────────────────────────────────────

        /// <summary>
        /// 計算樣本平均值與標準差。
        /// 呼叫前須確保 rows.Count &gt;= <see cref="MinimumSampleCount"/>。
        /// </summary>
        private static (double mean, double std) CalculateMeanAndStd(
            IReadOnlyList<SiteMeanRow> rows
        )
        {
            // MathNet.Numerics Statistics API 接受 double；
            // DECIMAL(18,9) 最多 18 位有效數字，double 可精確表達 15-16 位，
            // 在本專案的量測數值範圍內不會造成精度遺失。
            var values = rows.Select(r => (double)r.MeanValue).ToList();
            return (Statistics.Mean(values), Statistics.StandardDeviation(values));
        }

        /// <summary>
        /// 計算管制上下限（UCL/LCL）。使用 ±<paramref name="sigmaMultiplier"/>σ 規則，預設 6σ。
        /// double → decimal 轉換同 <see cref="CalculateMeanAndStd"/> 的精度說明。
        /// </summary>
        private static (decimal ucl, decimal lcl) CalculateControlLimits(
            double mean,
            double std,
            double sigmaMultiplier = DefaultSigmaMultiplier
        )
        {
            var ucl = (decimal)(mean + sigmaMultiplier * std);
            var lcl = (decimal)(mean - sigmaMultiplier * std);
            return (ucl, lcl);
        }

        /// <summary>
        /// 從歷史資料中提取時間範圍（計算起迄時間）。
        /// QuerySiteMeanRows 已篩選 start_time IS NOT NULL，此處直接取 Min/Max。
        /// </summary>
        private static (DateTime start, DateTime end) ExtractTimeRange(
            IReadOnlyList<SiteMeanRow> rows
        )
        {
            var times = rows.Select(r => r.StartTime).ToList();
            return (times.Min(), times.Max());
        }

        private byte GetRequiredSiteMeanMethodId(IDbTransaction tx = null)
        {
            var methodId = _detectionMethodRepo.GetIdByKey(SiteMeanMethodKey, tx);
            if (!methodId.HasValue)
            {
                throw new InvalidOperationException(
                    "detection_methods 中找不到 method_key = 'SITE_MEAN' 的設定，無法建立 DetectionSpec。"
                );
            }

            return methodId.Value;
        }

        /// <summary>
        /// 建立 DetectionSpec 實體。
        /// </summary>
        private static DetectionSpec BuildDetectionSpec(
            string programName,
            uint siteId,
            string testItemName,
            byte methodId,
            decimal ucl,
            decimal lcl,
            DateTime specCalcStart,
            DateTime specCalcEnd,
            double mean,
            double std
        )
        {
            return new DetectionSpec
            {
                Program = programName,
                TestItemName = testItemName,
                SiteId = siteId,
                DetectionMethodId = methodId,
                SpecUpperLimit = ucl,
                SpecLowerLimit = lcl,
                SpecCalcStartTime = specCalcStart,
                SpecCalcEndTime = specCalcEnd,
                SpecCalcMean = (decimal)mean,
                SpecCalcStd = (decimal)std,
            };
        }
    }
}

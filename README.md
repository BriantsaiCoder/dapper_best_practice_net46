# dapper_best_practice_net46

> .NET Framework 4.6.1 + Dapper + MySQL 的正式生產環境基底專案

## 專案定位

本專案提供一套可直接延伸到正式環境的資料存取基底，重點放在：

- 明確可維護的 SQL 與 Repository 邊界
- 可由外部工作流程協調的交易式資料寫入
- 短生命週期連線與安全的連線字串管理
- 啟動檢查、結構化日誌與失敗時的可觀測性
- 避免不必要的抽象層，維持可讀性與可擴充性

目前方案名稱、組件名稱與命名空間仍沿用既有的
`DapperMySqlCrudExample`，以維持既有相容性；但此倉庫的定位已是
**正式生產環境基底**，而非教學展示或假資料展示專案。

## 核心能力

- 以 Dapper 實作 9 組核心資料表的 Repository 與介面
- `Insert` / `Update` / `Delete` 皆支援選用 `IDbTransaction`，可參與外部交易
- `DbConnectionFactory` 優先讀取 `MYSQL_CONNECTION_STRING` 環境變數
- 啟動入口僅進行基礎設施初始化與資料庫連線檢查，不會預設寫入任何業務資料
- NLog 提供主控台與檔案雙通道日誌輸出
- `DetectionSpecRepository.ComputeAndInsertSiteMeanSpec()` 提供 SITE_MEAN 規格計算與落庫能力

## 技術棧

| 類別 | 技術 / 套件 | 版本 | 說明 |
| --- | --- | --- | --- |
| Runtime | .NET Framework | 4.6.1 | 既有正式環境常見版本 |
| Language | C# | 7.3 | 專案語言版本上限 |
| Micro-ORM | Dapper | 2.1.35 | 明確 SQL、低額外負擔 |
| MySQL Driver | MySql.Data | 8.0.33 | 相容 MySQL 5.6 / 5.7 / 8.0+ |
| Logging | NLog | 5.3.4 | 主控台 + 檔案輸出 |
| Statistics | MathNet.Numerics | 5.0.0 | 平均值 / 標準差計算 |

> `MySql.Data` 9.x 已移除 MySQL 5.x 支援；若正式環境仍有 MySQL 5.6 或 5.7，
> 請維持 8.0.x 線。

## 架構概覽

```text
Program.cs
  └─ 啟動檢查 / composition root
       └─ DbConnectionFactory
            └─ Repository（由應用工作流程手動組裝或交由 DI 容器管理）
                 └─ Dapper SQL
                      └─ MySQL
```

### 目錄結構

```text
DapperMySqlCrudExample/
├── App.config                    # 連線字串後備設定
├── NLog.config                   # 日誌規則與輸出目標
├── Program.cs                    # 啟動檢查與連線驗證
├── Infrastructure/               # 連線工廠與基礎設施
├── Models/                       # POCO 模型
├── Repositories/                 # Repository 介面與實作
└── Sql/
    └── schema.sql                # 完整資料庫 schema
```

## Schema 範圍

`Sql/schema.sql` 目前定義 **26 張資料表**，可分成兩類：

1. **既有整合 / 上游資料表（17 張）**
   - 例如 `lots_info`、`lots_result`、`lots_statistic`、`ieda_title`、
     `tester_status` 等
   - 用於承接既有製造、測試、分析或彙整資料
2. **異常檢測核心資料表（9 張）**
   - 目前已由 Repository 層完整覆蓋

### 已覆蓋的核心資料表

| 資料表 | 責任 |
| --- | --- |
| `detection_methods` | 偵測方法主檔 |
| `anomaly_lots` | 異常批號主表 |
| `anomaly_test_items` | 異常測項明細 |
| `anomaly_units` | 異常 Unit 明細 |
| `anomaly_lot_process_mapping` | 批號製程映射 |
| `anomaly_unit_process_mapping` | Unit 製程映射 |
| `detection_specs` | 規格與統計結果 |
| `site_test_statistics` | Site 測項統計值 |
| `good_lots` | 正常批號記錄 |

## 啟動流程

目前 `Program.cs` 的責任刻意保持精簡：

1. 初始化 `DbConnectionFactory`
2. 透過 `SELECT 1` 驗證資料庫連線可用性
3. 記錄啟動成功或失敗日誌
4. 以程序結束碼回傳結果

這個入口不會在啟動時自動新增、更新或刪除業務資料，也不會注入任何硬編碼假資料。

## 連線設定

### 正式環境建議

- **優先使用** `MYSQL_CONNECTION_STRING` 環境變數
- `App.config` 的 `DefaultConnection` 僅作為本機維護或部署前驗證的後備設定
- 不要把正式密碼直接提交到版本控制

### `App.config` 後備範本

```xml
<add name="DefaultConnection"
     connectionString="Server=localhost;Port=3306;Database=app_db;Uid=app_service;Pwd=replace-with-secure-secret;CharSet=utf8mb4;AllowUserVariables=true;SslMode=Required;MinimumPoolSize=5;MaximumPoolSize=100;ConnectionLifeTime=300;DefaultCommandTimeout=30;"
     providerName="MySql.Data.MySqlClient" />
```

### 主要連線參數

| 參數 | 建議 |
| --- | --- |
| `SslMode` | MySQL 8.0+ 建議 `Required`；MySQL 5.x 視伺服器 SSL 設定調整 |
| `MinimumPoolSize` | 依正式環境冷啟動需求設定 |
| `MaximumPoolSize` | 依主機規格與併發量評估 |
| `ConnectionLifeTime` | 避免長時間持有過舊連線 |
| `DefaultCommandTimeout` | 依查詢複雜度與 SLA 調整 |

## 交易整合模式

本專案的交易模式採用 **外部連線 + 外部交易協調**：

- `IDbConnectionFactory.Create()` 建立並開啟連線
- 呼叫端在連線上開啟交易
- Repository 的寫入方法接受選用 `IDbTransaction`

```csharp
using (var connection = factory.Create())
using (var transaction = connection.BeginTransaction())
{
    anomalyLotRepository.Insert(anomalyLot, transaction);
    anomalyTestItemRepository.Insert(anomalyTestItem, transaction);
    anomalyUnitRepository.Insert(anomalyUnit, transaction);

    transaction.Commit();
}
```

這種模式的優點是：

- 不需要額外引入 Unit of Work 類別
- 多個 Repository 可在同一筆交易中協作
- 交易邊界由真正的應用工作流程決定，而不是被資料層硬綁定

## 結構化日誌

NLog 已預設整合完成：

- 主控台：`Info` 以上
- 檔案：`Warn` 以上
- 檔案位置：`${basedir}/logs/app.log`
- 滾動策略：每日歸檔
- 保留數量：30 份封存檔

目前已覆蓋的日誌場景：

- MySQL 連線開啟失敗
- 啟動階段未處理例外

正式專案整合時，建議額外補上：

- 關鍵工作流程的成功 / 失敗事件
- 重要交易失敗的上下文資訊
- 長時間查詢或批次作業的效能觀測點

## 主要工程決策

### 為何使用 Dapper

- 需要對 SQL 保有完全掌控
- 適合既有 MySQL schema 與明確查詢需求
- 相對於重量級 ORM，更容易做效能調校與資料庫相依調整

### 為何維持 Manual DI

- 目前是資料存取基底專案，啟動邏輯非常薄
- 在沒有複雜應用層與宿主框架之前，手動組裝更直接且更容易追蹤
- 若後續需要整合 Web API、Worker Service 或排程主機，再引入 DI 容器即可

### 為何不額外建立 Service 層

- 目前 Repository 已足以承接資料存取責任
- 交易邊界與業務流程應由真正的應用層決定
- 在基底專案階段先保持最小必要抽象，避免形成沒有價值的中介層

## Repository 擴充規範

新增資料表或資料存取能力時，建議遵循以下流程：

1. 在 `Sql/schema.sql` 新增或調整資料表定義
2. 在 `Models/` 新增對應 POCO
3. 在 `Repositories/` 新增 `IRepository` 與實作類別
4. 以 `private const string SelectColumns` 維持欄位映射集中管理
5. `Insert` / `Update` / `Delete` 保持支援 `IDbTransaction transaction = null`
6. 在真正的應用工作流程中整合新 Repository，而不是把資料操作硬塞回 `Program.cs`

## 建構與執行

### 建構

```bash
dotnet build dapper_best_practice_net46.sln
```

### 執行啟動檢查

```bash
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj
```

### 直接執行輸出檔

```bash
DapperMySqlCrudExample/bin/Debug/net461/DapperMySqlCrudExample.exe
```

> `net461` 在 Windows 上可直接執行；在 macOS / Linux 上請確認已有相容的
> Mono 或其他可執行 .NET Framework 4.6.1 應用程式的執行環境。

## 驗證清單

在部署或延伸此基底前，建議至少確認：

- `MYSQL_CONNECTION_STRING` 或 `DefaultConnection` 已正確設定
- 目標資料庫可連線，且已套用 `Sql/schema.sql`
- `detection_methods` 基礎資料已存在（schema 內已提供 seed）
- `dotnet build dapper_best_practice_net46.sln` 可成功完成
- 啟動檢查可正常回傳成功結束碼

## 後續整合建議

此基底適合用於以下延伸方向：

- 建立 ASP.NET Core Web API 或內部服務層
- 建立批次匯入 / 匯出工作流程
- 建立排程式資料計算或統計任務
- 與既有製造、測試、分析平台進行資料整合

若後續要擴充成正式應用，建議把業務流程放在新的應用層或服務層中，
讓本專案持續專注在 **資料存取模式、交易協調與基礎設施一致性**。

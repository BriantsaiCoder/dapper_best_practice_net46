# Dapper .NET Framework 4.6.1 最佳實踐示範

> .NET Framework 4.6.1 + Dapper + MySQL 的生產等級資料存取基底

---

## 目錄

- [專案定位](#專案定位)
- [5 分鐘快速導覽](#5-分鐘快速導覽)
- [技術棧](#技術棧)
- [專案結構](#專案結構)
- [核心設計模式](#核心設計模式)
- [交易使用情境](#交易使用情境)
- [快速開始](#快速開始)
- [資料庫設定](#資料庫設定)
- [連線設定](#連線設定)
- [Repository 擴充規範](#repository-擴充規範)
- [主要工程決策](#主要工程決策)
- [設計分析 — 不過度設計 × Best Practice](#設計分析--不過度設計--best-practice)
- [驗證清單](#驗證清單)

---

## 專案定位

本專案提供一套可直接延伸至正式環境的 **Dapper + MySQL 資料存取基底**，重點放在：

- 明確可維護的 SQL 與 Repository 邊界
- 可被外部工作流程協調的交易式資料寫入
- 短生命週期連線與安全的連線字串管理
- 啟動檢查、結構化日誌與失敗可觀測性
- 最小必要抽象層，維持可讀性與延伸性
- 最少抽象層，直接使用 Dapper 原生 API，降低學習曲線

---

## 5 分鐘快速導覽

> 給剛加入團隊的工程師：依照以下 4 步驟，30 分鐘內掌握整個專案。

### Step 1 — 先跑起來（5 min）

```bash
dotnet build dapper_best_practice_net46.sln          # 建構
# 設定連線字串（擇一）
export MYSQL_CONNECTION_STRING="Server=localhost;Database=dapper_demo;Uid=root;Pwd=your_password;"
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj            # 啟動檢查
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj -- --demo  # CRUD 展示
```

觀察 Console 輸出，理解完整 CRUD 流程。

### Step 2 — 讀最簡單的 Repository（10 min）

打開 `DetectionMethodRepository.cs`（7 個欄位，最精簡）。
重點掌握三個核心模式：

| 模式 | 位置 | 用途 |
|------|------|------|
| `SelectColumns` 常數 | 類別頂部 | DRY 欄位對應（`snake_case` → `PascalCase`） |
| `using (var conn = _factory.Create())` | 每個方法 | 短生命週期連線管理 |
| `if (transaction != null)` 分支 | Insert / Update / Delete | 交易複用既有連線 vs. 自建連線 |

### Step 3 — 讀示範方法（10 min）

打開 `Demos/CrudDemoRunner.cs`，查看三個展示方法：

| 方法 | 展示情境 |
|------|----------|
| `RunNonTransactionExample()` | 單筆 CRUD，不使用交易 |
| `RunTransactionExample()` | 多個 Repository 在同一筆交易中協作 + Rollback |
| `RunComputeSiteMeanSpecExample()` | 業務計算（查詢 → 統計 → 寫入）搭配交易 |

### Step 4 — 動手加一張新表（5 min）

跟著下方 [Repository 擴充規範](#repository-擴充規範) 的 3 步流程（DDL → Model → Repository），仿照 `DetectionMethodRepository` 實作一次，即完成上手。

---

## 技術棧

| 類別 | 套件 | 版本 | 說明 |
|------|------|------|------|
| Runtime | .NET Framework | 4.6.1 | 目標框架 |
| Language | C# | 7.3 | 語言版本上限（不用 C# 8+ 語法） |
| Micro-ORM | Dapper | 2.1.35 | 明確 SQL、低額外負擔 |
| MySQL Driver | MySql.Data | 8.0.33 | 相容 MySQL 5.6–8.0+ |
| Logging | NLog | 5.3.4 | 主控台 + 檔案輪替輸出 |
| Statistics | MathNet.Numerics | 5.0.0 | 平均值 / 標準差計算 |

> `MySql.Data` 9.x 已移除 MySQL 5.x 支援；若正式環境仍有 MySQL 5.6/5.7，請維持 8.0.x。

---

## 專案結構

```text
dapper_best_practice_net46.sln
    └── DapperMySqlCrudExample/                  # 主專案（net461）
        ├── Infrastructure/
        │   └── DbConnectionFactory.cs           # 讀取環境變數 / App.config
        ├── Models/                              # Dapper 對應 POCO（無 ORM Attribute）
        │   ├── DetectionMethod.cs
        │   ├── DetectionSpec.cs
        │   ├── SiteTestStatistic.cs
        │   ├── AnomalyLot.cs
        │   ├── GoodLot.cs
        │   ├── AnomalyLotProcessMapping.cs
        │   ├── AnomalyUnit.cs
        │   ├── AnomalyUnitProcessMapping.cs
        │   └── AnomalyTestItem.cs
        ├── Repositories/                        # 具體類別（共 9 個）
        │   ├── DetectionMethodRepository.cs
        │   ├── DetectionSpecRepository.cs       # 含 SITE_MEAN 規格計算邏輯
        │   └── ...                              # 其餘 7 個 Repository
        ├── Demos/                               # 示範程式（非必要）
        │   └── CrudDemoRunner.cs                # CRUD + 交易 + 規格計算示範
        ├── Sql/
        │   ├── schema.sql                       # 核心 9 張表 DDL
        │   └── schema-legacy.sql                # 既有系統整合表格 DDL
        ├── App.config                           # 連線字串後備設定
        ├── NLog.config                          # 日誌設定
        └── Program.cs                           # Composition Root / 啟動檢查
```

---

## 核心設計模式

### 1. DbConnectionFactory — 連線生命週期

```csharp
// 每個方法各自管理連線，不共享連線物件
public IEnumerable<DetectionMethod> GetAll()
{
    using (var conn = _factory.Create())   // Open → Query → Close
        return conn.Query<DetectionMethod>($"SELECT {SelectColumns} FROM detection_methods");
}
```

- `DbConnectionFactory` 統一封裝連線字串取得與開啟連線。
- 每個方法使用 `using` 管理短生命週期連線。

### 2. 交易處理 — 直接使用 Dapper API

```csharp
// 有交易時複用既有連線，無交易時自建短生命週期連線
public long Insert(DetectionSpec entity, IDbTransaction transaction = null)
{
    if (transaction != null)
        return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

    using (var conn = _factory.Create())
        return conn.ExecuteScalar<long>(sql, entity);
}
```

- `Update` / `Delete` 統一回傳 `bool`（`影響行數 > 0`）。
- `Insert` 使用 `ExecuteScalar<T>` 搭配 `SELECT LAST_INSERT_ID()`，不做二次 SELECT。

### 3. SelectColumns 常數 — DRY 欄位對應

```csharp
private const string SelectColumns = @"
    id              AS Id,
    method_code     AS MethodCode,
    method_name     AS MethodName,
    has_test_item   AS HasTestItem";
```

- 所有查詢方法共用同一欄位清單，修改只需更新一處。
- DB 欄位為 `snake_case`，C# 屬性為 `PascalCase`，透過 `AS` 別名對齊，**不需 ORM Attribute**。

### 4. 標準延伸方法

每個 Repository 均包含：

```csharp
bool Exists(long id);
int GetCount();
IEnumerable<T> GetPaged(int offset, int limit);
```

分頁使用 MySQL 偏移優先語法：`LIMIT @Offset, @Limit`。

### 5. SITE_MEAN 規格計算 — 業務邏輯 + 交易

```csharp
public long ComputeAndInsertSiteMeanSpec(string programName, uint siteId, string testItemName)
{
    using (var conn = _factory.Create())
    using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
    {
        // 1. 查詢 site_test_statistics 歷史資料（雙策略：1個月內 ≥30 筆，或最新 30 筆）
        // 2. 以 MathNet.Numerics 計算 Mean / StdDev
        // 3. 計算 UCL = Mean + 6σ, LCL = Mean - 6σ
        // 4. 寫入 detection_specs 後 Commit
    }
}
```

- 使用 `IsolationLevel.RepeatableRead` 確保讀取一致性。
- 業務邏輯直接在 Repository 中實作，避免不必要的分層。

### 6. 交易整合模式

```csharp
using (var conn = factory.Create())
using (var tx = conn.BeginTransaction())
{
    anomalyLotRepository.Insert(anomalyLot, tx);
    anomalyUnitRepository.Insert(anomalyUnit, tx);
    tx.Commit();
}
```

- 不需引入 Unit of Work 類別。
- 多個 Repository 可在同一筆交易中協作。
- 交易邊界由應用工作流程決定，而非被資料層硬綁定。

---

## 交易使用情境

資料庫交易確保一組操作全部成功或全部回滾，是資料一致性的基礎。以下整理**常見需要交易的通用情境**，並以本專案的實際程式碼作為範例。

### 常見需要交易的情境

#### 1. 多表寫入（父子表 / 關聯表）

**通用場景**：訂單 + 訂單明細、使用者 + 角色指派、發票 + 發票項目等，任何「一次業務動作寫入多張表」的場景。部分成功會導致孤立資料或破壞外鍵完整性。

> **本專案範例**：異常批號建立 — 同時寫入 `anomaly_lots` → `anomaly_test_items` → `anomaly_units` → 對應的 process mapping 表。

```csharp
// Program.cs — RunTransactionExample()
using (var conn = factory.Create())
using (var tx = conn.BeginTransaction())
{
    anomalyLotRepository.Insert(anomalyLot, tx);
    anomalyUnitRepository.Insert(anomalyUnit, tx);
    tx.Commit();   // 全部成功才提交
}
```

#### 2. 先讀後寫（Read-then-Write）

**通用場景**：庫存扣減（讀取庫存 → 計算 → 更新）、銀行轉帳（讀取餘額 → 驗證 → 扣款/入帳）、報表快照計算等。讀取與寫入之間若有其他交易修改了同一筆資料，會導致計算結果錯誤。通常需搭配較高的隔離層級（如 `RepeatableRead`）。

> **本專案範例**：SITE_MEAN 規格計算 — 先從 `site_test_statistics` 讀取歷史資料計算平均值與標準差，再寫入 `detection_specs`。

```csharp
// DetectionSpecRepository.cs — ComputeAndInsertSiteMeanSpec()
using (var conn = _factory.Create())
using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
{
    var rows = QuerySiteMeanRows(conn, tx, ...);  // 1. 讀取
    var mean = rows.Mean();                        // 2. 計算
    Insert(newSpec, tx);                           // 3. 寫入
    tx.Commit();
}
```

#### 3. 批量操作（Batch Insert / Update / Delete）

**通用場景**：批次匯入 CSV 資料、批次刪除過期記錄、批次更新價格等。中途失敗時需要全部回滾，避免只處理了一半的資料。

> **本專案範例**：批量刪除父子資料 — 先刪子表 `anomaly_units`，再刪父表 `anomaly_lots`，中途失敗需全部回滾。

#### 4. 跨實體狀態同步

**通用場景**：同一個業務事件需要同步更新多個實體的狀態，例如訂單完成時同步更新庫存、會員點數與物流狀態。任何一個更新失敗都應回滾全部。

> **本專案範例**：好批 + 異常批同步建立 — 同一批號的 `good_lots` 與 `anomaly_lots` 判定需原子性寫入，避免出現只有一邊有紀錄的不一致狀態。

#### 5. 補償 / Saga 的局部步驟

**通用場景**：在微服務或分散式架構中，單一服務內部的多步操作仍需要本地交易。例如「扣款服務」內部的帳務記錄 + 餘額更新，即使整體流程透過 Saga 協調，每個參與者內部仍以交易確保一致性。

### 不需要交易的情境

| 情境 | 說明 |
|------|------|
| 單筆 CRUD | 單一 `INSERT` / `UPDATE` / `DELETE` 天生為原子操作（本專案：`RunNonTransactionExample()`） |
| 純查詢 | `GetAll` / `GetById` / `GetPaged` 等唯讀操作，不改變資料 |
| 冪等操作 | 重複執行結果不變的操作（如 `UPSERT`），即使中斷也可安全重試 |

### 判斷原則

> **何時該加交易？**
> 1. 一個業務動作需要寫入 **兩張以上** 的資料表
> 2. 需要 **先讀後寫**，且讀取結果會影響寫入內容
> 3. 一組操作必須 **全部成功或全部失敗**（原子性需求）
>
> 只要符合以上任一條件，就應使用交易。

### 隔離層級選擇

| 隔離層級 | 適用場景 | 本專案範例 |
|----------|---------|-----------|
| `ReadCommitted`（預設） | 多表寫入、一般批量操作 | `RunTransactionExample()` — 多表 Commit / Rollback |
| `RepeatableRead` | 先讀後寫，需防止幻讀 | `ComputeAndInsertSiteMeanSpec()` — 統計計算 |
| `Serializable` | 最高一致性要求（少用，效能代價高） | — |

對應 Program.cs 的三個展示方法：

- `RunNonTransactionExample()` — 單筆 CRUD，不使用交易
- `RunTransactionExample()` — 多表寫入的 Commit / Rollback 示範
- `RunComputeSiteMeanSpecExample()` — 先讀後寫的 `RepeatableRead` 交易示範

---

## 快速開始

### 前置需求

- .NET SDK（任意版本，支援 `dotnet CLI`）
- MySQL Server 8.x
- macOS / Linux / Windows

### 建構

```bash
git clone <repo-url>
cd dapper_best_practice_net46
dotnet build dapper_best_practice_net46.sln
```

預期輸出：`建置 成功`，0 錯誤、0 警告。

### 執行啟動檢查（預設安全模式）

```bash
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj
```

此模式只驗證資料庫連線，不會執行任何新增 / 更新 / 刪除示範。

### 執行 CRUD 展示

```bash
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj -- --demo
```

帶入 `--demo` 後，才會依序執行 Repository CRUD、交易示範與 SITE_MEAN 規格計算範例。

---

## 資料庫設定

```bash
# 建立資料庫
mysql -u root -p -e "CREATE DATABASE dapper_demo CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# 若為全新環境，先建立既有系統整合資料表（含 lots_info 等外鍵依賴）
mysql -u root -p dapper_demo < DapperMySqlCrudExample/Sql/schema-legacy.sql

# 套用核心 9 張資料表 DDL
mysql -u root -p dapper_demo < DapperMySqlCrudExample/Sql/schema.sql
```

> ⚠️ **外部依賴**：`anomaly_lots`、`site_test_statistics`、`good_lots` 含 `lots_info(id)` 外鍵，  
> 此資料表須在執行 `schema.sql` **前**存在。若環境中已有 `lots_info`，可跳過 `schema-legacy.sql`。

---

## 連線設定

### 方式 A：環境變數（建議）

```bash
export MYSQL_CONNECTION_STRING="Server=localhost;Database=dapper_demo;Uid=root;Pwd=your_password;"
```

### 方式 B：App.config（本機後備）

```xml
<connectionStrings>
  <add name="DefaultConnection"
       connectionString="Server=localhost;Port=3306;Database=dapper_demo;Uid=root;Pwd=replace_me;CharSet=utf8mb4;SslMode=Required;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

> 不要把正式密碼直接提交到版本控制。

---

## Repository 擴充規範

新增資料表時，請遵循以下流程：

1. **`Sql/schema.sql`** — 新增 DDL（`CREATE TABLE`、索引、外鍵）
2. **`Models/Foo.cs`** — 建立 POCO，屬性名稱與 SQL `AS` 別名一致（`PascalCase`）
3. **`Repositories/FooRepository.cs`** — 實作，含 `SelectColumns` 常數與 `using (var conn = _factory.Create())` 模式

```csharp
// 標準 Repository 骨架
public sealed class FooRepository
{
    private readonly DbConnectionFactory _factory;

    public FooRepository(DbConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    private const string SelectColumns = @"
        id          AS Id,
        foo_name    AS FooName,
        created_at  AS CreatedAt";

    public IEnumerable<Foo> GetAll()
    {
        using (var conn = _factory.Create())
            return conn.Query<Foo>($"SELECT {SelectColumns} FROM foos ORDER BY id");
    }

    public long Insert(Foo entity, IDbTransaction transaction = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        const string sql = "INSERT INTO foos (foo_name) VALUES (@FooName); SELECT LAST_INSERT_ID();";

        if (transaction != null)
            return transaction.Connection.ExecuteScalar<long>(sql, entity, transaction);

        using (var conn = _factory.Create())
            return conn.ExecuteScalar<long>(sql, entity);
    }

    public bool Update(Foo entity, IDbTransaction transaction = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        const string sql = "UPDATE foos SET foo_name = @FooName WHERE id = @Id";

        if (transaction != null)
            return transaction.Connection.Execute(sql, entity, transaction) > 0;

        using (var conn = _factory.Create())
            return conn.Execute(sql, entity) > 0;
    }

    public bool Delete(long id, IDbTransaction transaction = null)
    {
        const string sql = "DELETE FROM foos WHERE id = @Id";

        if (transaction != null)
            return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

        using (var conn = _factory.Create())
            return conn.Execute(sql, new { Id = id }) > 0;
    }

    public bool Exists(long id)
    {
        const string sql = "SELECT COUNT(1) FROM foos WHERE id = @Id";
        using (var conn = _factory.Create())
            return conn.ExecuteScalar<int>(sql, new { Id = id }) > 0;
    }
}
```

---

## 主要工程決策

| 決策 | 選擇 | 原因 |
|------|------|------|
| `Execute` 回傳型別 | `bool` | 消除呼叫端對 `row count > 0` 的重複判斷 |
| 欄位對應方式 | SQL `AS` 別名 | 不污染 POCO，保持模型純淨 |
| SITE_MEAN 計算位置 | Repository 內 | 避免不必要的 Service 層；CRUD 保持薄 Repository |
| 交易管理位置 | 呼叫端 `using (var tx = ...)` | Repository 保持無狀態，交易邊界由業務邏輯決定 |
| C# 語言版本 | 7.3 | .NET Framework 4.6.1 不支援 C# 8+ 功能 |
| 分頁語法 | `LIMIT @Offset, @Limit` | MySQL 偏移優先語法，明確高效 |
| DetectionMethod PK | `byte`（TINYINT） | 對應 schema 實際型別，避免隱式轉換 |
| DI 方式 | Manual DI | 基底專案啟動邏輯薄，不需引入 DI 容器複雜度 |
| 不使用 DapperExtensions | 直接呼叫 Dapper | 新工程師可直接看到 Dapper 原生 API，降低學習曲線 |

---

## 設計分析 — 不過度設計 × Best Practice

> 本專案作為**正式生產環境的基底範本**提供給新工程師使用。
> 以下分析以「不過度設計（YAGNI）」為原則，評估每項設計決策是否同時滿足：
> 1. 生產環境的穩健性需求
> 2. 新工程師上手的低門檻
> 3. 真正需要才引入的最小抽象層

---

### ✅ 已達成的 Best Practice（恰到好處，不過度）

| 面向 | 做法 | 為什麼這就夠了（作為範本的理由） |
|------|------|------|
| **SQL 安全** | 全面 `@ParamName` 參數化查詢 | 零 SQL Injection 風險；新工程師照抄即安全 |
| **連線管理** | `using (var conn = _factory.Create())` | 短生命週期確保連線回收至連線池；最直覺的 pattern |
| **DRY 欄位對應** | `private const string SelectColumns` | 欄位修改只需改一處；新工程師不會遺漏別名同步 |
| **交易支援** | `IDbTransaction transaction = null` 可選參數 | 支援交易內/外雙模式，不強制交易；呼叫端可自由決定 |
| **PK 取回** | `SELECT LAST_INSERT_ID()` + `ExecuteScalar<T>` | 不做二次 SELECT，原子性取回新 ID |
| **型別精確** | `DetectionMethod` PK 為 `byte` 對應 `TINYINT` | 避免隱式轉換，C# 型別與 schema 一致 |
| **防禦性程式** | 建構子 + 寫入方法的 `ArgumentNullException` | 快速失敗，錯誤訊息明確 |
| **密碼管理** | 環境變數優先 → App.config 後備 | 不將密碼硬編碼到版本控制；部署靈活 |
| **日誌** | NLog：連線失敗 Warn + 頂層例外 Error；檔案輪替 30 天 | 生產必要的可觀測性，不過度記錄 |
| **啟動安全** | 預設安全模式，`--demo` 才觸發資料修改 | 防止意外執行；新工程師不會誤改線上資料 |
| **一致性** | 9 個 Repository 結構完全統一 | 新工程師照抄任一 Repository 即可新增 |
| **類別密封** | 所有 Repository 與 DbConnectionFactory 為 `sealed` | 防止不當繼承，明確表達設計意圖 |
| **Schema 分離** | `schema.sql`（核心 9 表）+ `schema-legacy.sql`（既有系統） | 新舊系統職責分明，部署順序明確 |
| **連線池** | App.config 已設定 `MinimumPoolSize=5; MaximumPoolSize=100` | 生產環境連線池配置到位 |
| **Demo 分離** | `Demos/CrudDemoRunner.cs` 獨立於生產程式碼 | 範本可直接拿掉 Demos 資料夾投入生產 |

---

### ❌ 刻意不做的事（YAGNI 決策與理由）

以下每項都是業界常見的「進階」做法，但在本專案的定位下屬於過度設計。

| 被省略的模式 | 為什麼不做 | 何時才需要引入 |
|------|------|------|
| **Repository Interface** | 無測試專案、無 DI 容器、無多型需求；加 interface 純為「可能要 mock」是典型 YAGNI | 引入單元測試且需要 mock DB 存取時 |
| **DI Container** | `Program.cs` 僅 3 個 `new`；手動組裝比容器更清楚、更好 debug | 依賴數量超過 10+ 或切換為 ASP.NET 時 |
| **Service Layer** | `ComputeAndInsertSiteMeanSpec()` 的「查→算→寫」內聚於 Repository，資料來源與寫入目標相同 | 業務邏輯跨多個 Repository 且需協調多種計算時 |
| **Async/Await** | 同步主控台應用，無並發請求處理需求；加 async 只增加每個方法的複雜度 | 切換至 Web API 或需要並行 I/O 時 |
| **RepositoryBase 基底** | 重複僅建構子 3 行（`_factory` 欄位 + null check）；繼承耦合的代價大於省下的幾行程式碼 | Repository 數量極多（30+）且共用邏輯超過 10 行時 |
| **Generic Repository** | 犧牲型別安全與可讀性；每張表的查詢需求不同，泛型無法涵蓋 | 幾乎不存在合理場景 |
| **DapperExtensions Helper** | 直接使用 Dapper 原生 API（`Query`, `Execute`, `ExecuteScalar`），新工程師看到的就是 Dapper 本身 | 不引入；保持零封裝 |
| **Unit of Work 類別** | `using (var tx = conn.BeginTransaction())` 已足夠；交易邊界由呼叫端決定 | 需要自動追蹤 dirty entities 或延遲提交時 |
| **每方法 try-catch + 日誌** | 例外自然往上拋，頂層 `Program.cs` 已有全域 catch；逐方法 catch 會吞掉有用的 stack trace | 僅在需要特定錯誤處理或降級策略的方法中加 |
| **CQRS 讀寫分離** | 讀寫量皆低，單一 Repository 同時處理讀寫即可 | 讀寫負載差異極大或需要獨立擴展時 |

---

### 🏫 作為新工程師範本的教學評估

#### 漸進學習路徑

```text
Step 1: DetectionMethodRepository（最簡單，7 欄位，無 JOIN）
  ↓
Step 2: AnomalyLotRepository（相同結構，多一個 GetByLotsInfoId 查詢）
  ↓
Step 3: DetectionSpecRepository（JOIN 查詢 + SITE_MEAN 業務邏輯 + 交易）
  ↓
Step 4: CrudDemoRunner（三種情境的完整示範）
```

#### 範本可複製性評估

| 評估項目 | 結果 | 說明 |
|------|------|------|
| **新增一張表的步驟** | 3 步 | DDL → Model → Repository，有明確規範 |
| **需要理解的核心 pattern** | 3 個 | `SelectColumns` + `using conn` + `transaction null check` |
| **需要修改 Program.cs** | 否 | 新 Repository 由業務流程決定何時實例化 |
| **需要額外設定** | 否 | 無 DI 註冊、無 Mapper 設定、無 Attribute |
| **出錯時的診斷難度** | 低 | 無抽象層、無反射、SQL 直接可見 |

#### 新工程師常見疑問與本範本的回答

| 常見疑問 | 本範本的回答 |
|------|------|
| 「為什麼不用 Entity Framework？」 | SQL 完全可控、效能可預測、無 migration 黑盒 |
| 「為什麼不加 Interface？」 | 目前無測試需求；需要時再加，加的成本很低 |
| 「為什麼 Repository 沒有 base class？」 | 重複只有 3 行，繼承帶來的耦合風險更高 |
| 「為什麼不用 async？」 | 同步主控台應用；切換至 Web API 時再加 |
| 「Dapper 的 `AS` 別名會不會很煩？」 | 只需在 `SelectColumns` 定義一次，所有查詢方法共用 |
| 「交易怎麼用？」 | 看 `CrudDemoRunner.RunTransactionExample()`，5 分鐘即懂 |

---

### 📊 設計達成度評分

> 評分基準：在「不過度設計」前提下，是否達到生產環境基底範本的標準。

| 維度 | 評等 | 說明 |
|------|------|------|
| **SQL 安全性** | ⭐⭐⭐⭐⭐ | 全面參數化，零漏洞 |
| **連線管理** | ⭐⭐⭐⭐⭐ | `using` + 工廠模式 + 連線池配置 |
| **欄位對應** | ⭐⭐⭐⭐⭐ | `AS` 別名 + `SelectColumns` DRY |
| **交易處理** | ⭐⭐⭐⭐⭐ | 可選交易參數 + RepeatableRead + Demo 展示 |
| **架構適當性** | ⭐⭐⭐⭐⭐ | 不過度分層、不過度抽象，恰到好處 |
| **可觀測性** | ⭐⭐⭐⭐☆ | 連線失敗 + 頂層例外有日誌；業務邏輯無日誌（非此範本目標） |
| **新手友善度** | ⭐⭐⭐⭐⭐ | 3 個核心 pattern、一致的結構、漸進學習路徑 |
| **生產就緒度** | ⭐⭐⭐⭐⭐ | 連線池、密碼管理、啟動檢查、Schema 分離皆到位 |

**結論**：本專案在「不過度設計」的前提下，作為正式生產環境基底範本的完成度非常高。
9 個 Repository 的統一結構讓新工程師能快速照抄，三種 Demo 情境涵蓋了日常資料存取的主要場景，
而刻意省略的抽象層（Interface、DI Container、Service Layer）降低了入門門檻，
同時在 README 中明確記錄了「何時才需要引入」的判斷原則，
使得本範本不只是可用的程式碼，更是一份可演進的架構決策參考。

---

## 驗證清單

部署或延伸此基底前，請確認：

- [ ] `MYSQL_CONNECTION_STRING` 或 `DefaultConnection` 已正確設定
- [ ] 目標資料庫可連線，DDL 已套用
- [ ] `detection_methods` 基礎資料已存在
- [ ] `dotnet build dapper_best_practice_net46.sln` 建構成功（0 錯誤）

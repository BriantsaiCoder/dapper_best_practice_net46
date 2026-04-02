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

打開 `DetectionMethodRepository.cs`（7 個欄位，最精簡），搭配介面 `IDetectionMethodRepository.cs` 一起看。
重點掌握三個核心模式：

| 模式 | 位置 | 用途 |
|------|------|------|
| `SelectColumns` 常數 | 類別頂部 | DRY 欄位對應（`snake_case` → `PascalCase`） |
| `using (var conn = _factory.Create())` | 每個方法 | 短生命週期連線管理 |
| `if (transaction != null)` 分支 | Insert / Update / Delete | 交易複用既有連線 vs. 自建連線 |

### Step 3 — 讀 Program.cs 的三個展示方法（10 min）

| 方法 | 展示情境 |
|------|----------|
| `RunNonTransactionExample()` | 單筆 CRUD，不使用交易 |
| `RunTransactionExample()` | 多個 Repository 在同一筆交易中協作 + Rollback |
| `RunComputeSiteMeanSpecExample()` | 業務計算（查詢 → 統計 → 寫入）搭配交易 |

### Step 4 — 動手加一張新表（5 min）

跟著下方 [Repository 擴充規範](#repository-擴充規範) 的 4 步流程（DDL → Model → Interface → Implementation），仿照 `DetectionMethodRepository` 實作一次，即完成上手。

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
    │   ├── IDbConnectionFactory.cs          # 連線工廠介面
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
    ├── Repositories/                        # 介面 + 實作（共 9 組）
    │   ├── IDetectionMethodRepository.cs
    │   ├── DetectionMethodRepository.cs
    │   ├── IDetectionSpecRepository.cs      # 含 ComputeAndInsertSiteMeanSpec
    │   ├── DetectionSpecRepository.cs       # 含 SITE_MEAN 規格計算邏輯
    │   └── ...                              # 其餘 7 組 Repository
    ├── Sql/
    │   └── schema.sql                       # 完整 DDL
    ├── App.config                           # 連線字串後備設定
    ├── NLog.config                          # 日誌設定
    └── Program.cs                           # Composition Root / CRUD + 規格計算展示
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

- `IDbConnectionFactory` 介面讓 Repository 與具體驅動解耦。
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

### 4. 標準延伸介面

每個 Repository 介面均包含：

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

# 套用資料表 DDL（需先建立外部依賴的 lots_info 資料表）
mysql -u root -p dapper_demo < DapperMySqlCrudExample/Sql/schema.sql
```

> ⚠️ **外部依賴**：`anomaly_lots` 與 `good_lots` 含 `lots_info(id)` 外鍵，  
> 此資料表為外部系統提供，需在執行 DDL **前**自行建立。

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
3. **`Repositories/IFooRepository.cs`** — 宣告介面（含 `Exists` / `GetCount` / `GetPaged`）
4. **`Repositories/FooRepository.cs`** — 實作，含 `SelectColumns` 常數與 `using (var conn = _factory.Create())` 模式

```csharp
// 標準 Repository 骨架
public class FooRepository : IFooRepository
{
    private readonly IDbConnectionFactory _factory;

    public FooRepository(IDbConnectionFactory factory)
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

## 驗證清單

部署或延伸此基底前，請確認：

- [ ] `MYSQL_CONNECTION_STRING` 或 `DefaultConnection` 已正確設定
- [ ] 目標資料庫可連線，DDL 已套用
- [ ] `detection_methods` 基礎資料已存在
- [ ] `dotnet build dapper_best_practice_net46.sln` 建構成功（0 錯誤）

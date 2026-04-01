# Dapper .NET Framework 4.6.1 最佳實踐示範

> .NET Framework 4.6.1 + Dapper + MySQL 的生產等級資料存取基底

---

## 目錄

- [專案定位](#專案定位)
- [技術棧](#技術棧)
- [專案結構](#專案結構)
- [核心設計模式](#核心設計模式)
- [快速開始](#快速開始)
- [資料庫設定](#資料庫設定)
- [連線設定](#連線設定)
- [執行測試](#執行測試)
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
- 完整單元測試覆蓋（不需 DB），整合測試可選執行

---

## 技術棧

| 類別 | 套件 | 版本 | 說明 |
|------|------|------|------|
| Runtime | .NET Framework | 4.6.1 / 4.6.2 | 主/測試專案目標框架 |
| Language | C# | 7.3 | 語言版本上限（不用 C# 8+ 語法） |
| Micro-ORM | Dapper | 2.1.35 | 明確 SQL、低額外負擔 |
| MySQL Driver | MySql.Data | 8.0.33 | 相容 MySQL 5.6–8.0+ |
| Logging | NLog | 5.3.4 | 主控台 + 檔案輪替輸出 |
| Statistics | MathNet.Numerics | 5.0.0 | 平均值 / 標準差計算 |
| Testing | MSTest | 3.3.1 | 單元 / 整合測試框架 |
| Assertion | FluentAssertions | 6.12.0 | 流式斷言語法 |
| Mock | Moq | 4.20.70 | 測試替身 |

> `MySql.Data` 9.x 已移除 MySQL 5.x 支援；若正式環境仍有 MySQL 5.6/5.7，請維持 8.0.x。

---

## 專案結構

```text
dapper_best_practice_net46.sln
├── DapperMySqlCrudExample/                  # 主專案（net461）
│   ├── Infrastructure/
│   │   ├── IDbConnectionFactory.cs          # 連線工廠介面（含交易支援）
│   │   ├── DbConnectionFactory.cs           # 讀取環境變數 / App.config
│   │   └── DapperExtensions.cs              # Execute→bool / ExecuteScalar<T>
│   ├── Models/                              # Dapper 對應 POCO（無 ORM Attribute）
│   │   ├── DetectionMethod.cs
│   │   ├── DetectionSpec.cs
│   │   ├── SiteTestStatistic.cs
│   │   ├── AnomalyLot.cs
│   │   ├── GoodLot.cs
│   │   ├── AnomalyLotProcessMapping.cs
│   │   ├── AnomalyUnit.cs
│   │   ├── AnomalyUnitProcessMapping.cs
│   │   └── AnomalyTestItem.cs
│   ├── Repositories/                        # 介面 + 實作（共 9 組）
│   │   ├── IDetectionMethodRepository.cs
│   │   ├── DetectionMethodRepository.cs
│   │   └── ...
│   ├── Services/
│   │   ├── IDetectionSpecService.cs
│   │   └── DetectionSpecService.cs          # 統計計算服務（含交易）
│   ├── Sql/
│   │   └── schema.sql                       # 完整 DDL
│   ├── App.config                           # 連線字串後備設定
│   ├── NLog.config                          # 日誌設定
│   └── Program.cs                           # Composition Root / CRUD 展示
│
└── DapperMySqlCrudExample.Tests/            # 測試專案（net462）
    ├── Infrastructure/
    │   ├── MockDbConnectionFactory.cs        # 注入 Mock IDbConnection（單元測試用）
    │   └── LiveDbConnectionFactory.cs        # 讀取環境變數，真實 MySQL 連線（整合測試用）
    └── Repositories/
        ├── DetectionMethodRepositoryTests.cs # DetectionMethod 單元 / 整合測試
        └── CrudUsageExampleTests.cs          # 跨 Repository 模型驗證 + CRUD 整合範例
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

- `IDbConnectionFactory` 介面讓 Repository 與具體驅動解耦，測試可注入 Mock。
- `BeginTransaction()` 回傳已 Open 連線上的 `IDbTransaction`，由呼叫端以 `using` 管理生命週期。

### 2. DapperExtensions — 統一回傳語意

```csharp
// Execute 回傳 bool，消除呼叫端對 row count 的重複判斷
internal static bool Execute(this IDbConnectionFactory factory,
    string sql, object param = null, IDbTransaction transaction = null)
{
    if (transaction != null)
        return transaction.Connection.Execute(sql, param, transaction) > 0;
    using (var conn = factory.Create()) return conn.Execute(sql, param) > 0;
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

### 5. DetectionSpecService — 業務計算 + 交易

```csharp
public void ComputeAndInsertDetectionSpec(byte detectionMethodId)
{
    using (var tx = _factory.BeginTransaction())
    {
        // 透過 MathNet.Numerics 計算統計量
        // INSERT detection_specs
        tx.Commit();   // 例外則 using 區塊自動 Rollback
    }
}
```

- 使用 `IsolationLevel.RepeatableRead` 確保讀取一致性。

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

### 執行 CRUD 展示

```bash
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj
```

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

## 執行測試

### 單元測試（不需 MySQL）

```bash
dotnet test DapperMySqlCrudExample.Tests/DapperMySqlCrudExample.Tests.csproj \
    --filter "TestCategory!=Integration"
```

預期：**9/9 全部通過**。

### 整合測試（需要真實 MySQL）

整合測試標記有 `[Ignore]`，需先設定連線字串再執行：

```bash
export MYSQL_CONNECTION_STRING="Server=localhost;Database=dapper_demo;Uid=root;Pwd=your_password;"

dotnet test DapperMySqlCrudExample.Tests/DapperMySqlCrudExample.Tests.csproj \
    --filter "TestCategory=Integration"
```

### CI 建議設定

```yaml
# GitHub Actions 範例
- name: Run Unit Tests
  run: |
    dotnet test DapperMySqlCrudExample.Tests/DapperMySqlCrudExample.Tests.csproj \
      --filter "TestCategory!=Integration" \
      --logger "trx;LogFileName=test-results.trx"
```

---

## Repository 擴充規範

新增資料表時，請遵循以下流程：

1. **`Sql/schema.sql`** — 新增 DDL（`CREATE TABLE`、索引、外鍵）
2. **`Models/Foo.cs`** — 建立 POCO，屬性名稱與 SQL `AS` 別名一致（`PascalCase`）
3. **`Repositories/IFooRepository.cs`** — 宣告介面（含 `Exists` / `GetCount` / `GetPaged`）
4. **`Repositories/FooRepository.cs`** — 實作，含 `SelectColumns` 常數與 `using (var conn = _factory.Create())` 模式
5. **測試** — 在 `Tests/Repositories/` 新增對應測試類別

```csharp
// 標準 Repository 骨架
public class FooRepository : IFooRepository
{
    private readonly IDbConnectionFactory _factory;
    public FooRepository(IDbConnectionFactory factory) { _factory = factory; }

    private const string SelectColumns = @"
        id          AS Id,
        foo_name    AS FooName,
        created_at  AS CreatedAt";

    public IEnumerable<Foo> GetAll()
    {
        using (var conn = _factory.Create())
            return conn.Query<Foo>($"SELECT {SelectColumns} FROM foos ORDER BY id");
    }

    public long Insert(Foo entity)
    {
        const string sql = "INSERT INTO foos (foo_name) VALUES (@FooName); SELECT LAST_INSERT_ID();";
        return _factory.ExecuteScalar<long>(sql, entity);
    }

    public bool Update(Foo entity) =>
        _factory.Execute("UPDATE foos SET foo_name = @FooName WHERE id = @Id", entity);

    public bool Delete(long id) =>
        _factory.Execute("DELETE FROM foos WHERE id = @Id", new { Id = id });

    public bool Exists(long id) =>
        _factory.ExecuteScalar<int>("SELECT COUNT(1) FROM foos WHERE id = @Id", new { Id = id }) > 0;
}
```

---

## 主要工程決策

| 決策 | 選擇 | 原因 |
|------|------|------|
| `Execute` 回傳型別 | `bool` | 消除呼叫端對 `row count > 0` 的重複判斷 |
| 欄位對應方式 | SQL `AS` 別名 | 不污染 POCO，保持模型純淨 |
| Service 層範圍 | 僅 DetectionSpec | 複雜統計才需分層；CRUD 保持薄 Repository |
| 交易管理位置 | 呼叫端 `using (var tx = ...)` | Repository 保持無狀態，交易邊界由業務邏輯決定 |
| C# 語言版本 | 7.3 | .NET Framework 4.6.1 不支援 C# 8+ 功能 |
| 分頁語法 | `LIMIT @Offset, @Limit` | MySQL 偏移優先語法，明確高效 |
| DetectionMethod PK | `byte`（TINYINT） | 對應 schema 實際型別，避免隱式轉換 |
| DI 方式 | Manual DI | 基底專案啟動邏輯薄，不需引入 DI 容器複雜度 |

---

## 驗證清單

部署或延伸此基底前，請確認：

- [ ] `MYSQL_CONNECTION_STRING` 或 `DefaultConnection` 已正確設定
- [ ] 目標資料庫可連線，DDL 已套用
- [ ] `detection_methods` 基礎資料已存在
- [ ] `dotnet build dapper_best_practice_net46.sln` 建構成功（0 錯誤）
- [ ] 單元測試全數通過：`--filter "TestCategory!=Integration"`

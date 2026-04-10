# GitHub Copilot 工作區指引

> **回覆語言**：一律使用**繁體中文（zh-TW）**回覆，包含程式碼註解、commit message、PR 描述與對話內容。

> 本專案為 **.NET Framework 4.6.1 + Dapper + MySQL** 的正式生產環境基底專案。
> 詳細專案目的與技術說明請參閱 [`README.md`](../README.md)。

---

## 建構與執行

```bash
# 傳統（非 SDK-style）.csproj，需使用 NuGet + MSBuild 建置（不支援 dotnet build / dotnet run）

# 還原 NuGet 套件
nuget restore dapper_best_practice_net46.sln

# 建置
msbuild dapper_best_practice_net46.sln /p:Configuration=Debug

# 執行（啟動檢查 + 資料存取示範，確認 App.config 或環境變數已設定連線字串）
DapperMySqlCrudExample\bin\Debug\DapperMySqlCrudExample.exe
```

**前置需求**：

- 目標框架：`net461`（.NET Framework 4.6.1）；C# 語言版本鎖定 `7.0`
- 專案格式：傳統（非 SDK-style）.csproj + `packages.config`；建置需要 Visual Studio 或 MSBuild（不支援 `dotnet build`）
- 執行前需有可連線的 MySQL 執行個體，並透過 `MYSQL_CONNECTION_STRING` 或 `DapperMySqlCrudExample/App.config` 的 `DefaultConnection` 提供連線字串
- 資料庫 DDL 位於 [`DapperMySqlCrudExample/Sql/schema.sql`](../DapperMySqlCrudExample/Sql/schema.sql)

---

## 架構總覽

```text
Program.cs
   └─ 啟動檢查 / composition root
       ├─ DbConnectionFactory.Create() → MySqlConnection（已 Open）
       │   └─ Dapper 驗證連線 → MySQL DB
       └─ CrudSampleRunner（啟動後自動執行，僅接收 DbConnectionFactory）
           ├─ XxxRepository（內部自行建構，sealed 具體類別）
           │   └─ Dapper 查詢 / 寫入 → MySQL DB
           └─ XxxService（內部自行建構，sealed 具體類別）
               └─ 業務邏輯編排（交易、計算、跨 Repository 協調）
```

| 層次     | 目錄              | 職責                                                     |
| -------- | ----------------- | -------------------------------------------------------- |
| 進入點   | `Program.cs`      | 啟動檢查與 composition root，驗證連線後執行資料存取示範   |
| 基礎建設 | `Infrastructure/` | `DbConnectionFactory`（sealed concrete class，無介面）    |
| 模型     | `Models/`         | Dapper 對應 POCO（無 ORM Attribute）+ sealed DTO          |
| 資料存取 | `Repositories/`   | sealed 具體類別，共 9 個，純 CRUD                         |
| 業務邏輯 | `Services/`       | sealed 具體類別，跨 Repository 業務編排（交易、計算）      |
| 展示     | `Samples/`        | `CrudSampleRunner`，3 個 Sample 方法（非必要）            |
| 資料庫   | `Sql/`            | 核心 9 張表 DDL + 既有系統整合表 DDL                      |

---

## 程式碼慣例

### 新增 Repository 時，務必遵循以下模式

#### 1. 實作：使用 `SelectColumns` 常數 + `using` 管理連線 + 可選交易

```csharp
// Repositories/FooRepository.cs
public sealed class FooRepository
{
    private readonly DbConnectionFactory _factory;

    public FooRepository(DbConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    // 必須：提取共用欄位 AS 別名為常數，遵循 DRY 原則
    private const string SelectColumns = @"
        id          AS Id,
        foo_name    AS FooName,
        created_at  AS CreatedAt,
        updated_at  AS UpdatedAt";

    // 此範本刻意不預設提供 GetAll() 與 GetCount()，避免被複製後造成大表全表掃描。
    // 僅在低筆數主檔表（如 DetectionMethodRepository）才保留 GetAll() 與 GetCount()。

    /// <summary>
    /// 依主鍵查詢單筆資料。
    /// </summary>
    public Foo GetById(long id)
    {
        const string sql = "SELECT " + SelectColumns + " FROM foos WHERE id = @Id";
        using (var conn = _factory.Create())
        {
            return conn.QueryFirstOrDefault<Foo>(sql, new { Id = id });
        }
    }

    /// <summary>
    /// 新增一筆資料並回傳自動遞增主鍵。
    /// </summary>
    public long Insert(Foo entity, IDbTransaction transaction = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        const string insertSql = @"
            INSERT INTO foos (foo_name) VALUES (@FooName)";

        const string identitySql = "SELECT LAST_INSERT_ID()";

        if (transaction != null)
        {
            transaction.Connection.Execute(insertSql, entity, transaction);
            return transaction.Connection.ExecuteScalar<long>(identitySql, transaction: transaction);
        }

        using (var conn = _factory.Create())
        {
            conn.Execute(insertSql, entity);
            return conn.ExecuteScalar<long>(identitySql);
        }
    }

    /// <summary>
    /// 更新一筆資料。
    /// </summary>
    public bool Update(Foo entity, IDbTransaction transaction = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        const string sql = @"UPDATE foos SET foo_name = @FooName WHERE id = @Id";

        if (transaction != null)
            return transaction.Connection.Execute(sql, entity, transaction) > 0;

        using (var conn = _factory.Create())
        {
            return conn.Execute(sql, entity) > 0;
        }
    }

    /// <summary>
    /// 依主鍵刪除一筆資料。
    /// </summary>
    public bool Delete(long id, IDbTransaction transaction = null)
    {
        const string sql = "DELETE FROM foos WHERE id = @Id";

        if (transaction != null)
            return transaction.Connection.Execute(sql, new { Id = id }, transaction) > 0;

        using (var conn = _factory.Create())
        {
            return conn.Execute(sql, new { Id = id }) > 0;
        }
    }

    /// <summary>
    /// 檢查指定主鍵的資料是否存在。
    /// </summary>
    public bool Exists(long id)
    {
        const string sql = "SELECT 1 FROM foos WHERE id = @Id LIMIT 1";
        using (var conn = _factory.Create())
        {
            return conn.QueryFirstOrDefault<int?>(sql, new { Id = id }).HasValue;
        }
    }

    /// <summary>
    /// 依外鍵查詢多筆資料。
    /// </summary>
    public IReadOnlyList<Foo> GetByBarId(long barId)
    {
        const string sql = "SELECT " + SelectColumns + " FROM foos WHERE bar_id = @BarId ORDER BY id";
        using (var conn = _factory.Create())
        {
            return conn.Query<Foo>(sql, new { BarId = barId }).ToList();
        }
    }
}
```

#### 2. Model：純 POCO，不加 ORM Attribute

```csharp
// Models/Foo.cs
public sealed class Foo
{
    public long Id { get; set; }
    public string FooName { get; set; }   // 屬性名稱必須與 SQL AS 別名一致
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Models/FooCalcParams.cs — 唯讀 DTO 用 sealed class
public sealed class FooCalcParams
{
    public long Id { get; set; }
    public string Name { get; set; }
}
```

### 新增 Service 時，務必遵循以下模式

```csharp
// Services/FooService.cs
public sealed class FooService
{
    private readonly DbConnectionFactory _factory;
    private readonly FooRepository _fooRepo;
    private readonly BarRepository _barRepo;

    public FooService(DbConnectionFactory factory, FooRepository fooRepo, BarRepository barRepo)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _fooRepo = fooRepo ?? throw new ArgumentNullException(nameof(fooRepo));
        _barRepo = barRepo ?? throw new ArgumentNullException(nameof(barRepo));
    }

    // Service 負責：交易邊界、業務計算、跨 Repository 編排
    public long DoBusinessLogic(string param)
    {
        using (var conn = _factory.Create())
        using (var tx = conn.BeginTransaction(IsolationLevel.RepeatableRead))
        {
            var foo = _fooRepo.GetByKey(param);
            // 業務計算…
            var bar = new Bar { FooId = foo.Id };
            var barId = _barRepo.Insert(bar, tx);
            tx.Commit();
            return barId;
        }
    }
}
```

---

## 關鍵規則

| 規則 | 說明 |
| ---- | ---- |
| **`sealed` 類別** | 所有 Repository、Service、DbConnectionFactory 皆為 `sealed`，不使用介面 |
| **`private const string SelectColumns`** | 每個 Repository 必須有此常數，避免每個方法重複欄位 AS 清單 |
| **`using (var conn = _factory.Create())`** | 每個方法自己管理連線生命週期，不共享連線；`using` 區塊一律使用大括號 `{ }` 包覆，不使用單行省略寫法 |
| **`IDbTransaction transaction = null`** | CUD 方法皆接受可選交易參數，有交易時複用既有連線 |
| **`SELECT LAST_INSERT_ID()`** | Insert 先 `Execute` 再於同一連線 `ExecuteScalar` 取自動遞增 PK（MySql.Data 6.x 不支援多語句批次 ExecuteScalar） |
| **`QueryFirstOrDefault`** | 查單筆時使用，不拋例外，由呼叫端處理 null |
| **Parameterized Query** | 所有 SQL 參數使用 `@ParamName`，對應 Dapper 匿名物件或 entity 屬性 |
| **欄位名稱轉換** | DB 欄位為 `snake_case`，C# 屬性為 `PascalCase`，透過 `AS` 對齊 |
| **無 ORM Attribute** | 不使用 `[Column]`、`[Table]`，依賴 SQL 別名對應 |
| **C# 版本限制** | 語言版本為 `7.0`，不用 C# 7.1+ 語法（如 `async Main`、`default` 字面值、tuple name inference）及 C# 8+ 語法（如 `??=`、record、switch 運算式） |
| **Nullable 未啟用** | C# 7.0 + net461 不支援可空參考型別分析，`string` 可能為 null |
| **Repository = 純 CRUD** | Repository 不含業務邏輯，計算與編排由 Service 層負責 |
| **Service = 業務邏輯** | 跨 Repository 協調、交易邊界、統計計算皆放在 Service |
| **讀取查詢用 `const string` 串接** | 使用 `SelectColumns` 的查詢必須用 `const string sql = "SELECT " + SelectColumns + " FROM ...";`，不可用 `var sql = $"SELECT {SelectColumns} ..."`（`$` 插值無法在編譯期常數折疊） |
| **`GetCount()` 僅限低筆數 lookup table** | 僅 `DetectionMethodRepository` 保留 `GetCount()`，其他 Repository 不提供此方法，避免全表掃描被新工程師誤用 |
| **`Exists()` 使用 `LIMIT 1`** | `SELECT 1 FROM table WHERE id = @Id LIMIT 1` + `QueryFirstOrDefault<int?>().HasValue`，不使用 `COUNT(1)` |
| **時間過濾參數化** | 不使用 SQL 端 `DATE_SUB(NOW(), ...)`，改在 C# 端以 `var sinceTime = DateTime.Now.AddMonths(-1)` 計算後傳入 `@SinceTime` 參數（目前 Repository 尚無使用此模式的方法，但新增時間範圍查詢時必須遵循） |
| **多筆查詢加 `ORDER BY id`** | 所有回傳多筆結果的方法必須加 `ORDER BY id`，確保結果順序可預測，避免隱性依賴數據庫內部存儲順序 |
| **多筆查詢回傳 `IReadOnlyList<T>`** | 用 `.ToList()` 立即具體化，回傳 `IReadOnlyList<T>`；避免 Dapper 的 `IEnumerable<T>` 延遲列舉在連線 `Dispose` 後才存取，需加 `using System.Linq` |

---

## 新增表格 / Repository 的標準流程

1. **`Sql/schema.sql`** — 新增 DDL（`CREATE TABLE`、索引、外鍵）
2. **`Models/Foo.cs`** — 建立 POCO，屬性對應 Schema 欄位（可空欄位用 `?`）
3. **`Repositories/FooRepository.cs`** — sealed 實作，含 `SelectColumns` 常數，CUD 支援可選 `IDbTransaction`
4. **`Services/FooService.cs`**（選擇性） — 若涉及跨 Repository 業務邏輯或交易編排，新增 Service 層
5. **應用工作流程** — 在 `Program.cs` 的 composition root 進行手動 DI 組裝（`CrudSampleRunner` 為展示用途，內部自行建構相依，不經由 `Program.cs` 注入）

---

## 常見陷阱

- **Dapper 屬性對應區分大小寫**：`AS CreatedAt` 須與 `public DateTime CreatedAt` 完全一致，拼寫不同將靜默對應失敗（欄位值為預設值）。
- **MySql.Data 6.x 多語句批次 ExecuteScalar 回傳 0**：`ExecuteScalar("INSERT ...; SELECT LAST_INSERT_ID()")` 會回傳第一個語句的結果（0），而非 LAST_INSERT_ID() 的值。必須拆為 `Execute` + `ExecuteScalar` 兩步驟。
- **`DbConnectionFactory()` 讀 App.config**：執行時若 `DefaultConnection` 未設定，會拋出 `InvalidOperationException` 並附帶明確說明；確認連線字串正確後再執行。
- **交易與連線生命週期**：有交易時必須使用 `transaction.Connection`，不可自建新連線。無交易時使用 `using (var conn = _factory.Create())`。

---

## 相依套件版本

| 套件                | 版本   | 備註                                             |
| ------------------- | ------ | ------------------------------------------------ |
| `Dapper`            | 2.1.35 | 不升至 3.x（API 有異動）                         |
| `MySql.Data`        | 6.10.9 | Oracle 更改授權前最後穩定版；純 managed assembly，net461 + VS2017 可直接編譯 |
| `MathNet.Numerics`  | 5.0.0  | 用於 SITE_MEAN 統計計算（平均值 / 標準差）        |
| `NLog`              | 5.3.4  | 結構化日誌（主控台 + 檔案輪替）                    |

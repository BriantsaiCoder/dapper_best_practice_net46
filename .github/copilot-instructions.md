# GitHub Copilot 工作區指引

> 本專案為 **.NET Framework 4.6.1 + Dapper + MySQL** 的 CRUD 最佳實務展示。
> 詳細專案目的與技術說明請參閱 [`README.md`](../README.md)。

---

## 建構與執行

```bash
# SDK-style 專案，支援 dotnet CLI（需安裝任意 .NET SDK）
dotnet build dapper_best_practice_net46.sln

# 執行（確認 App.config 的連線字串已設定）
dotnet run --project DapperMySqlCrudExample/DapperMySqlCrudExample.csproj

# 亦可用 MSBuild
msbuild dapper_best_practice_net46.sln /p:Configuration=Debug

# 執行編譯後的輸出
DapperMySqlCrudExample/bin/Debug/net461/DapperMySqlCrudExample.exe
```

**前置需求**：

- 目標框架：`net461`（.NET Framework 4.6.1）；C# 語言版本鎖定 `7.3`
- 執行前需有可連線的 MySQL 執行個體，並在 `DapperMySqlCrudExample/App.config` 設定 `DefaultConnection` 連線字串
- 資料庫 DDL 位於 [`DapperMySqlCrudExample/Sql/schema.sql`](../DapperMySqlCrudExample/Sql/schema.sql)

---

## 架構總覽

```
Program.cs
   └─ 建構子注入 IDbConnectionFactory，直接實例化各 Repository
IXxxRepository（抽象介面）
   └─ XxxRepository（實作，接收 IDbConnectionFactory）
         └─ DbConnectionFactory.Create() → MySqlConnection（已 Open）
               └─ Dapper 查詢 → MySQL DB
```

| 層次     | 目錄              | 職責                                           |
| -------- | ----------------- | ---------------------------------------------- |
| 進入點   | `Program.cs`      | 示範所有表格 CRUD，無 DI 容器（手動組裝）      |
| 基礎建設 | `Infrastructure/` | `IDbConnectionFactory` & `DbConnectionFactory` |
| 模型     | `Models/`         | Dapper 對應 POCO，無 ORM Attribute             |
| 資料存取 | `Repositories/`   | 介面 + 實作，每張表格一組                      |
| 資料庫   | `Sql/schema.sql`  | 9 張表格的 DDL，含外鍵與索引                   |

---

## 程式碼慣例

### 新增 Repository 時，務必遵循以下模式

#### 1. 介面：宣告標準 CRUD 方法

```csharp
// Repositories/IFooRepository.cs
public interface IFooRepository
{
    IEnumerable<Foo> GetAll();
    Foo GetById(long id);
    long Insert(Foo entity);
    bool Update(Foo entity);
    bool Delete(long id);
}
```

#### 2. 實作：使用 `SelectColumns` 常數 + `using` 管理連線

```csharp
// Repositories/FooRepository.cs
public class FooRepository : IFooRepository
{
    private readonly IDbConnectionFactory _factory;

    public FooRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    // 必須：提取共用欄位 AS 別名為常數，遵循 DRY 原則
    private const string SelectColumns = @"
        id          AS Id,
        foo_name    AS FooName,
        created_at  AS CreatedAt,
        updated_at  AS UpdatedAt";

    public IEnumerable<Foo> GetAll()
    {
        var sql = $"SELECT {SelectColumns} FROM foos ORDER BY id";
        using (var conn = _factory.Create())
            return conn.Query<Foo>(sql);
    }

    public long Insert(Foo entity)
    {
        const string sql = @"
            INSERT INTO foos (foo_name) VALUES (@FooName);
            SELECT LAST_INSERT_ID();";
        using (var conn = _factory.Create())
            return conn.ExecuteScalar<long>(sql, entity);
    }

    public bool Update(Foo entity)
    {
        const string sql = @"
            UPDATE foos SET foo_name = @FooName WHERE id = @Id";
        using (var conn = _factory.Create())
            return conn.Execute(sql, entity) > 0;
    }

    public bool Delete(long id)
    {
        const string sql = "DELETE FROM foos WHERE id = @Id";
        using (var conn = _factory.Create())
            return conn.Execute(sql, new { Id = id }) > 0;
    }
}
```

#### 3. Model：純 POCO，不加 ORM Attribute

```csharp
// Models/Foo.cs
public class Foo
{
    public long Id { get; set; }
    public string FooName { get; set; }   // 屬性名稱必須與 SQL AS 別名一致
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## 關鍵規則

| 規則                                       | 說明                                                                    |
| ------------------------------------------ | ----------------------------------------------------------------------- |
| **`private const string SelectColumns`**   | 每個 Repository 必須有此常數，避免每個方法重複欄位 AS 清單              |
| **`using (var conn = _factory.Create())`** | 每個方法自己管理連線生命週期，不共享連線                                |
| **`SELECT LAST_INSERT_ID()`**              | Insert 後不做二次 SELECT，直接取取自動遞增 PK                           |
| **`QueryFirstOrDefault`**                  | 查單筆時使用，不拋例外，由呼叫端處理 null                               |
| **Parameterized Query**                    | 所有 SQL 參數使用 `@ParamName`，對應 Dapper 匿名物件或 entity 屬性      |
| **欄位名稱轉換**                           | DB 欄位為 `snake_case`，C# 屬性為 `PascalCase`，透過 `AS` 對齊          |
| **無 ORM Attribute**                       | 不使用 `[Column]`、`[Table]`，依賴 SQL 別名對應                         |
| **C# 版本限制**                            | 語言版本為 `7.3`，不用 C# 8+ 語法（如 `??=`、record、switch 運算式）    |
| **Nullable 停用**                          | 專案不使用可空參考型別分析（`Nullable: disable`），`string` 可能為 null |

---

## 新增表格 / Repository 的標準流程

1. **`Sql/schema.sql`** — 新增 DDL（`CREATE TABLE`、索引、外鍵）
2. **`Models/Foo.cs`** — 建立 POCO，屬性對應 Schema 欄位（可空欄位用 `?`）
3. **`Repositories/IFooRepository.cs`** — 宣告介面
4. **`Repositories/FooRepository.cs`** — 實作，含 `SelectColumns` 常數
5. **`Program.cs`** — 實例化並呼叫 CRUD Demo

---

## 常見陷阱

- **Dapper 屬性對應區分大小寫**：`AS CreatedAt` 須與 `public DateTime CreatedAt` 完全一致，拼寫不同將靜默對應失敗（欄位值為預設值）。
- **MySql.Data 9.x 移除 MySQL 5.x 支援**：升級前確認 MySQL Server 版本。目前鎖定 8.0.33。
- **net461 + MySql.Data 8.x 警告**：非功能性警告，已以 `SuppressTfmSupportBuildWarnings` 抑制，不需處理。
- **`DbConnectionFactory()` 讀 App.config**：執行時若 `DefaultConnection` 未設定，會拋出 `InvalidOperationException` 並附帶明確說明；確認連線字串正確後再執行。

---

## 相依套件版本

| 套件         | 版本   | 備註                                             |
| ------------ | ------ | ------------------------------------------------ |
| `Dapper`     | 2.1.35 | 不升至 3.x（API 有異動）                         |
| `MySql.Data` | 8.0.33 | 9.x 移除 MySQL 5.x；若升級需同步確認 Server 版本 |

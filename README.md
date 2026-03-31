# dapper_best_practice_net46

> .NET Framework 4.6.1 + Dapper + MySQL — 生產就緒 CRUD 最佳實務

## 專案說明

本專案以 **Console Application** 形式，在 .NET Framework 4.6.1 環境下，
使用 [Dapper](https://github.com/DapperLib/Dapper) 搭配 MySQL
進行 CRUD（新增、查詢、更新、刪除）操作，並遵循業界最佳實務。
專案已包含交易支援、結構化日誌、連線安全防護等生產級功能。

---

## 技術選型

| 項目 | 套件 | 版本 |
|------|------|------|
| ORM（Micro-ORM）| Dapper | 2.1.35 |
| MySQL Driver | MySql.Data | 8.0.33 |
| 日誌框架 | NLog | 5.2.8 |
| 數值統計 | MathNet.Numerics | 5.0.0 |
| Target Framework | .NET Framework | 4.6.1 |
| 語言版本 | C# | 7.3 |

> **MySql.Data 版本說明**：MySql.Data 9.x 起已移除 MySQL 5.x 支援；本專案使用 8.0.33 以同時相容 MySQL 5.6/5.7 及 8.0+。MySql.Data 8.x 的部分間接依賴（System.Text.Json 等）宣告建議 net462+，但本專案使用的 API 範圍在 net461 執行時完全正常，已透過 `SuppressTfmSupportBuildWarnings` 抑制非功能性警告。

---

## 專案結構

```
DapperMySqlCrudExample/
├── DapperMySqlCrudExample.csproj   # SDK-style 專案檔
├── App.config                       # 連線字串設定
├── NLog.config                      # NLog 日誌組態（Console + File 滾動封存）
├── Program.cs                       # 主程式（示範所有表格 CRUD）
│
├── Infrastructure/
│   ├── IDbConnectionFactory.cs      # 連線工廠介面（方便 Mock 測試）
│   └── DbConnectionFactory.cs       # 連線工廠實作（讀取 App.config）
│
├── Models/                          # 資料庫資料表對應 Model
│   ├── DetectionMethod.cs           # 偵測方法主表
│   ├── AnomalyLot.cs                # 異常批號主表
│   ├── AnomalyTestItem.cs           # 異常測項明細表
│   ├── AnomalyUnit.cs               # 異常 Unit 明細表
│   ├── AnomalyLotProcessMapping.cs  # 批號製程站點 Mapping
│   ├── AnomalyUnitProcessMapping.cs # Unit 製程 Boat Mapping
│   ├── DetectionSpec.cs             # Spec 規格表
│   ├── SiteTestStatistic.cs         # Site 測項統計值表
│   └── GoodLot.cs                   # 好批批號記錄表
│
├── Repositories/                    # Repository 介面 + 實作
│   ├── IDetectionMethodRepository.cs / DetectionMethodRepository.cs
│   ├── IAnomalyLotRepository.cs / AnomalyLotRepository.cs
│   ├── IAnomalyTestItemRepository.cs / AnomalyTestItemRepository.cs
│   ├── IAnomalyUnitRepository.cs / AnomalyUnitRepository.cs
│   ├── IAnomalyLotProcessMappingRepository.cs / AnomalyLotProcessMappingRepository.cs
│   ├── IAnomalyUnitProcessMappingRepository.cs / AnomalyUnitProcessMappingRepository.cs
│   ├── IDetectionSpecRepository.cs / DetectionSpecRepository.cs
│   ├── ISiteTestStatisticRepository.cs / SiteTestStatisticRepository.cs
│   └── IGoodLotRepository.cs / GoodLotRepository.cs
│
└── Sql/
    └── schema.sql                   # 完整建表 DDL 腳本
```

---

## 最佳實務重點

1. **Repository Pattern** — 每張資料表對應一個 `IXxxRepository` 介面與實作，降低耦合、方便替換與測試。
2. **IDbConnectionFactory** — 以介面抽象連線建立，可在單元測試中替換為假連線（Mock）。支援 `BeginTransaction()` 建立跨操作交易。
3. **`using` 管理連線生命週期** — 每個 Repository 方法內以 `using` 確保連線用完即關，避免連線池耗盡。
4. **Parameterized Query** — 所有 SQL 均使用 Dapper 匿名物件參數，杜絕 SQL Injection。
5. **Column AS 別名對應** — SQL 使用 `column_name AS PropertyName` 對應 C# 屬性名稱，無需額外設定 Column Attribute。
6. **`SELECT LAST_INSERT_ID()`** — Insert 後直接取回自動產生的 PK，避免額外一次 SELECT。
7. **`QueryFirstOrDefault`** — 查詢單筆時使用，不拋例外，由呼叫端判斷 null。
8. **`private const string SelectColumns`** — 提取共用欄位清單為常數，遵循 DRY 原則，欄位變動只需改一處。
9. **防呆與清晰例外訊息** — `DbConnectionFactory` 對 null / 空白連線字串丟出具明確說明的例外，方便偵錯。
10. **連線安全防護** — `Create()` 開啟連線失敗時主動 `Dispose()`，避免資源洩漏。
11. **環境變數優先** — `DbConnectionFactory` 優先讀取 `MYSQL_CONNECTION_STRING` 環境變數，方便容器化或 CI/CD 部署。
12. **交易支援** — `Update` / `Delete` 方法接受選用的 `IDbTransaction` 參數，可在外部交易內執行。
13. **GetAll 安全上限** — 所有 `GetAll()` 附帶 `LIMIT 10000`，防止全表掃描造成記憶體暴漲。
14. **結構化日誌** — 整合 NLog，Console + 滾動檔案（每日 / 10 MB 上限 / 保留 14 天），連線失敗與未處理例外皆記錄。

---

## 架構設計說明

### 整體分層

```
Program.cs（呼叫端）
   │  依賴介面
   ▼
IXxxRepository（抽象層）
   │  建構子注入
   ▼
XxxRepository（資料存取層）
   │  透過工廠取得連線
   ▼
IDbConnectionFactory → DbConnectionFactory → MySqlConnection → MySQL DB
```

### 各設計決策的用意

#### 1. Interface + 具體實作分離

**為何這樣設計？**
- **依賴反轉原則（DIP）**：上層呼叫端只依賴介面，不依賴具體資料庫實作。日後換資料庫（如 PostgreSQL）只需替換實作類別，呼叫端不用動。
- **可測試性**：單元測試可注入 Mock/Stub 取代真實資料庫，完全隔離 I/O 副作用。
- **職責清晰**：介面定義「能做什麼」，實作類別定義「怎麼做」，各司其職。

#### 2. IDbConnectionFactory 連線工廠

**為何不直接在 Repository 內 `new MySqlConnection()`？**
- **集中管理連線字串**：連線字串只在工廠一處讀取，不散落在各 Repository。
- **可測試性**：測試時注入 `FakeDbConnectionFactory`，讓 Repository 接到記憶體假連線，不需真實 MySQL。
- **生命週期明確**：工廠每次 `Create()` 回傳已開啟的連線，呼叫端以 `using` 管理，確保連線用完即釋放。

#### 3. 每個方法用 `using` 短連線

**為何不持有長連線？**
- **防止連線洩漏**：`using` 保證 `Dispose()` 一定被呼叫，即使拋出例外也不例外。
- **連線池效率**：MySql.Data 內建連線池，短連線用完歸還池，下次請求重用。
- **無狀態 Repository**：天然執行緒安全，可作單例或多次實例化使用。

#### 4. Parameterized Query

**為何不字串拼接 SQL？**
- **防止 SQL Injection**：Dapper 將參數值與 SQL 分離傳送，攻擊者無法透過輸入值破壞 SQL 結構。
- **效能**：資料庫可對相同結構 SQL 快取執行計畫（Query Plan Cache）。

#### 5. `private const string SelectColumns`

**為何提取欄位清單為常數？**
- **DRY 原則**：欄位清單只定義一次，新增/刪除欄位時只改一處。
- **可維護性**：閱讀各查詢方法時，不需逐行比對欄位是否一致。
- **零執行時開銷**：`const string` 在編譯期合併，不產生執行時字串拼接。

---

## 過度設計分析

本專案已具備生產就緒的架構，各設計層次皆有對應的工程理由，**整體架構並未過度設計**。以下為評估細節：

| 設計元素 | 評估結果 | 說明 |
|---------|---------|------|
| Repository Pattern | ✅ 合理 | 9 張表各自獨立，每個 Repository 職責單一，並非為了抽象而抽象 |
| `IXxxRepository` 介面 | ✅ 合理 | 支援 Mock 測試；若後續升級至 DI 容器（如 Autofac）可直接使用 |
| `IDbConnectionFactory` | ✅ 合理 | 隔離基礎設施，讓測試不需真實資料庫 |
| `SelectColumns` 常數 | ✅ 合理 | DRY 原則，減少維護成本 |
| `using` 短連線模式 | ✅ 合理 | 防洩漏、利用連線池，最佳實務標準做法 |
| `Program.cs` 手動組裝 | ✅ 合理 | Console 應用不需 DI 框架，手動 `new` 更易讀、更易維護 |
| 個別 `const SelectColumns`（各 Repo）| ⚠️ 可接受 | 若欄位重複率高，可考慮提取至 base class，但目前各表欄位差異大，分開維護反而清晰 |

> **結論**：本專案的架構設計與其「可測試、可替換、職責分離」的目標一致，不存在為複雜而複雜的設計，適合作為中小型生產專案的架構基礎。

---

## 使用方式

### 1. 建立資料庫與資料表

```sql
CREATE DATABASE your_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE your_db;
-- 執行 DapperMySqlCrudExample/Sql/schema.sql
```

### 2. 修改連線字串

開啟 `DapperMySqlCrudExample/App.config`，將連線字串改為您的環境：

```xml
<add name="DefaultConnection"
     connectionString="Server=localhost;Port=3306;Database=your_db;Uid=your_user;Pwd=your_password;CharSet=utf8mb4;AllowUserVariables=true;SslMode=Required;MinimumPoolSize=5;MaximumPoolSize=100;ConnectionLifeTime=300;DefaultCommandTimeout=30;"
     providerName="MySql.Data.MySqlClient" />
```

| 連線參數 | 說明 |
|---------|------|
| `SslMode=Required` | 強制 TLS 加密傳輸（MySQL 5.x 可改為 `Preferred`） |
| `MinimumPoolSize=5` | 預熱 5 條連線，減少冷啟動延遲 |
| `MaximumPoolSize=100` | 連線池上限，依主機規格調整 |
| `ConnectionLifeTime=300` | 連線存活 300 秒後回收，避免長期佔用 |
| `DefaultCommandTimeout=30` | SQL 指令逾時 30 秒 |

> ⚠️ 生產環境請勿將密碼硬寫在設定檔。可設定環境變數 `MYSQL_CONNECTION_STRING`，`DbConnectionFactory` 會優先使用環境變數。

### 3. 建置與執行

```bash
cd DapperMySqlCrudExample
dotnet restore
dotnet run
```

> 需 .NET Framework 4.6.1 執行環境（Windows）或安裝相容的 Mono（Linux/macOS）。

---

## 資料表說明

| # | 資料表名稱 | 中文說明 |
|---|-----------|---------|
| 1 | `detection_methods` | 偵測方法主表 |
| 2 | `anomaly_lots` | 異常批號主表 |
| 3 | `anomaly_test_items` | 異常測項明細表 |
| 4 | `anomaly_units` | 異常 Unit 明細表 |
| 5 | `anomaly_lot_process_mapping` | 批號製程站點 Mapping |
| 6 | `anomaly_unit_process_mapping` | Unit 製程 Boat XY Mapping |
| 7 | `detection_specs` | Spec 規格表 |
| 8 | `site_test_statistics` | Site 測項統計值表 |
| 9 | `good_lots` | 好批批號記錄表 |

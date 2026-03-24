# dapper_best_practice_net46

> .NET Framework 4.6 + Dapper + MySQL — 業界最佳實務 CRUD Console 範例

## 專案說明

本專案以 **Console Application** 示範在 .NET 4.6 環境下，
如何使用 [Dapper](https://github.com/DapperLib/Dapper) 搭配 MySQL
進行 CRUD（新增、查詢、更新、刪除）操作，並遵循業界最佳實務。

---

## 技術選型

| 項目 | 套件 | 版本 |
|------|------|------|
| ORM（Micro-ORM）| Dapper | 2.1.35 |
| MySQL Driver | MySql.Data | 9.3.0 |
| Target Framework | .NET Framework | 4.6 |
| 語言版本 | C# | 7.3 |

---

## 專案結構

```
DapperMySqlCrudExample/
├── DapperMySqlCrudExample.csproj   # SDK-style 專案檔
├── App.config                       # 連線字串設定
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
2. **IDbConnectionFactory** — 以介面抽象連線建立，可在單元測試中替換為假連線（Mock）。
3. **`using` 管理連線生命週期** — 每個 Repository 方法內以 `using` 確保連線用完即關，避免連線池耗盡。
4. **Parameterized Query** — 所有 SQL 均使用 Dapper 匿名物件參數，杜絕 SQL Injection。
5. **Column AS 別名對應** — SQL 使用 `column_name AS PropertyName` 對應 C# 屬性名稱，無需額外設定 Column Attribute。
6. **`SELECT LAST_INSERT_ID()`** — Insert 後直接取回自動產生的 PK，避免額外一次 SELECT。
7. **`QueryFirstOrDefault`** — 查詢單筆時使用，不拋例外，由呼叫端判斷 null。

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
     connectionString="Server=localhost;Port=3306;Database=your_db;Uid=your_user;Pwd=your_password;CharSet=utf8mb4;"
     providerName="MySql.Data.MySqlClient" />
```

> ⚠️ 生產環境請勿將密碼硬寫在設定檔，建議透過環境變數或密鑰管理服務注入。

### 3. 建置與執行

```bash
cd DapperMySqlCrudExample
dotnet restore
dotnet run
```

> 需 .NET Framework 4.6 執行環境（Windows）或安裝 Mono（Linux/macOS）。

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
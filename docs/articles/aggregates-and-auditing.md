# 聚合與審計

## 聚合根

DomainKit 採真 Code First。聚合根即 EF 對映型別。`AggregateRootBase` 將鍵固定為 `Guid`，底層為泛型 `AggregateRoot<TKey>`。聚合以充血方式封裝行為，狀態變更在記憶體內完成，由 `IUnitOfWork` 一次提交。

`EntityBase` 與 `AggregateRootBase` 預設使用 `Guid.CreateVersion7()` 建立識別碼。需要其他主鍵型別時，直接繼承 `Entity<TKey>` 或 `AggregateRoot<TKey>`。

資料庫主鍵與對外 Domain Key 不同時，實作 `IHasExternalKey<TKey>`。Repository 與 keyed QueryService 會優先使用 `ExternalKey` 查詢。

## 實體相等性

`Entity<TKey>` 覆寫 `Equals` 與 `GetHashCode`，以型別加識別碼判定相等，不使用參考相等。兩個執行個體只要是同一型別且 `Id` 相同就視為同一個實體，即使分別由不同查詢載入。

識別碼仍為 `TKey` 預設值的實體視為 transient。transient 實體只與自身的參考相等，與另一個同樣 transient 的執行個體不相等，`GetHashCode` 也退回參考雜湊。這代表 transient 實體在取得識別碼後雜湊碼會改變，先放進 `HashSet` 或當作 `Dictionary` 鍵再指派識別碼，之後就查不回來。`AggregateRootBase` 與 `EntityBase` 在建構時就以 `Guid.CreateVersion7()` 產生識別碼，不會處於 transient 狀態。

比較前會還原 Castle 動態代理的真實型別，延遲載入代理與原始實體可正確判定相等。判定 transient 的 `IsTransient` 為 `protected virtual`，若識別碼的「未指派」語意並非該型別的預設值，可由衍生類別覆寫。

## 審計標記介面

在聚合上實作下列介面，提交時由 `SaveChanges` 攔截器自動填值：

| 介面 | 填入欄位 |
| --- | --- |
| `IHasCreatedTime` | `CreatedTime` |
| `IHasCreator` | `CreatorId` |
| `IHasUpdatedTime` | `UpdatedTime` |
| `IHasUpdater` | `UpdaterId` |
| `IHasDeletedTime` / `IHasDeleter` | 軟刪除時間與操作者 |

同步 `SaveChanges()` 與非同步 `SaveChangesAsync()` 經過相同的審計填值、RowVersion 更新、領域事件派發與 Outbox 寫入。同步路徑會同步等待領域事件處理器與廣播通道完成，具有 `SynchronizationContext` 的宿主應使用非同步版本，避免同步等待造成死結。

依需要的審計層級，在 `OnModelCreating` 套用對應的設定擴充（位於 `CloudyWing.DomainKit.EntityFrameworkCore.Modeling`）：

| 設定擴充 | 套用範圍 |
| --- | --- |
| `ConfigureEntityBase` | 只設定 `Id` 為主鍵 |
| `ConfigureCreationAuditedEntity` | `CreatedTime` |
| `ConfigureCreationUserAuditedEntity` | `CreatedTime`、`CreatorId` |
| `ConfigureAuditedEntity` | `CreatedTime`、`UpdatedTime` |
| `ConfigureUserAuditedEntity` | 再加 `CreatorId`、`UpdaterId` |
| `ConfigureFullAuditedEntity` | 再加軟刪除欄位與軟刪除全域查詢篩選 |

實體須實作對應的審計標記介面；`ConfigureFullAuditedEntity` 需含 `IHasDeletedTime` / `IHasDeleter`。各審計設定內部都會呼叫 `ConfigureEntityBase`，不需另外呼叫。沒有審計欄位的實體才單獨使用 `ConfigureEntityBase`。

## 狀態標記介面

`IHasDisabledState` 與 `IHasDisplayOrder` 表達停用狀態與顯示順序，供跨聚合統一屬性名稱與語意。

| 介面 | 屬性 |
| --- | --- |
| `IHasDisabledState` | `IsDisabled` |
| `IHasDisplayOrder` | `DisplayOrder` |

兩者與審計標記介面不同。DomainKit 不對其填值、不加查詢篩選、不建索引，實作後的行為完全由應用程式決定。停用狀態以 `IsDisabled` 表達，查詢啟用中的資料寫成 `!entity.IsDisabled`。

## 操作者來源

審計操作者 `CreatorId` 與 `UpdaterId` 的取得有兩段優先序：

1. `IUnitOfWork.SaveChangesAsync(actorId, ...)` 明確傳入的 `actorId`。
2. 未傳時退回 DI 注入的 `IAuditUser.UserId`（預設 `NullAuditUser` 回傳 `null`，宿主可覆寫為登入使用者）。

非 Guid 的對外身分以 `IHasExternalKey<TKey>` 表達，例如以帳號字串對外辨識，內部識別仍可維持 `Guid`。

## 軟刪除

Repository 依 `IHasDeletedTime` 判斷刪除策略。實作此介面的實體會寫入 `DeletedTime`；未實作時才會執行實體刪除。若實體同時實作 `IHasDeleter`，審計攔截器會在軟刪除時寫入 `DeleterId`。

審計時間與 `DeletedTime` 皆取自 `TimeProvider`。審計攔截器使用 DI 註冊的 `TimeProvider`，`AddDomainKit` 未發現既有註冊時才補上 `TimeProvider.System`。Repository 優先使用建構子傳入的 `TimeProvider`；使用不含 `TimeProvider` 的建構子時，改取 DbContext 所屬應用程式服務容器中的註冊，兩者皆無時使用 `TimeProvider.System`。

`ConfigureFullAuditedEntity` 加入 `DeletedTime is null` 的全域 query filter，因此一般 Repository 與 QueryService 查詢不會取得已刪除資料。管理流程若略過 filter，必須同時評估租戶 filter 的影響。詳見[多租戶與並行控制](tenancy-and-concurrency.md)。

## 時間儲存與時區

DomainKit 提供全域 convention 接點，讓宿主選擇 `DateTimeOffset` 與 `DateTimeOffset?` 的儲存方式。需要正規化為 UTC offset 0 時，傳入 `UtcDateTimeOffsetConverter`：

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
    base.ConfigureConventions(configurationBuilder);
    configurationBuilder.ApplyDomainKitConventions(new UtcDateTimeOffsetConverter());
}
```

未傳入 converter 時，DomainKit 不改變 EF Core 的 provider 預設對映。若採 UTC 儲存，取到應用層後再依需求轉換為呈現時區。

## 雙軌事件

聚合分別收集兩類事件：

- **Domain event**。提交前派發，讓 handler 對 tracked entity 的變更納入同一次 `SaveChanges`。
- **Integration event**。設定 Outbox 時與業務資料同交易保存，之後由背景 dispatcher 交給 handler；提交成功後也會發布至 best-effort 廣播通道。

Handler 由 `AddDomainKit` 的 `handlerAssemblies` 掃描註冊。完整的派發、重試、判重與廣播語意見[事件、Outbox 與廣播](events-outbox-and-broadcast.md)。

## 延伸閱讀

- [持久化與模型設定](persistence-and-modeling.md)
- [事件、Outbox 與廣播](events-outbox-and-broadcast.md)
- [多租戶與並行控制](tenancy-and-concurrency.md)

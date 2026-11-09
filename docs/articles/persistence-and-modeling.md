# 持久化與模型設定

DomainKit 的 EF Core 套件提供 `Repository`、`IUnitOfWork`、聚合導覽載入與共用模型設定。Repository 負責載入及追蹤聚合，查詢投影則交由 `QueryService`，避免寫入模型與讀取模型混用。

## Repository

自訂 Repository 繼承 `Repository<TDbContext, TEntity, TKey>`。`TKey` 代表應用程式用來尋找聚合的 Domain Key，不一定等於資料庫主鍵。

```csharp
public sealed class CustomerRepository
    : Repository<AppDbContext, Customer, string> {
    public CustomerRepository(AppDbContext dbContext, IUnitOfWork unitOfWork)
        : base(dbContext, unitOfWork) {
    }
}
```

鍵值判定依下列順序執行：

1. 實體實作 `IHasExternalKey<TKey>` 時，使用 `ExternalKey`。
2. 其餘實體使用 EF Core model 定義的單一主鍵。
3. 複合主鍵須由 Repository 覆寫 `CreateKeyPredicate`，預設實作不支援。

`FindAsync` 找不到資料時回傳 `null`；`GetAsync`、`UpdateAsync` 與 `DeleteAsync` 找不到資料時拋出 `EntityNotFoundException`。

`CreateAsync`、`UpdateAsync` 與 `DeleteAsync` 都回傳受影響的實體，`UpdateAsync` 與 `DeleteAsync` 回傳的是已被追蹤的執行個體，可直接接續讀取更新後的狀態。`UpdateAsync` 另有接受 `Func<TEntity, CancellationToken, Task>` 的多載，供更新過程需要 await 其他服務時使用。

## 延後提交

`CreateAsync`、`UpdateAsync` 與 `DeleteAsync` 的 `autoCommit` 預設為 `false`。多個聚合操作可累積在同一個 `DbContext`，最後由 `IUnitOfWork` 提交一次。

```csharp
await customerRepository.CreateAsync(customer, cancellationToken: ct).ConfigureAwait(false);
await addressRepository.CreateAsync(address, cancellationToken: ct).ConfigureAwait(false);
await unitOfWork.SaveChangesAsync(actorId, ct).ConfigureAwait(false);
```

只有確定單一操作需要立即落地時才使用 `autoCommit: true`。此模式會在 Repository 方法內呼叫 `SaveChangesAsync`。Repository 方法的 `actorId` 參數會一併傳給該次提交，因此只在 `autoCommit: true` 時有作用；延後提交的情境由 `IUnitOfWork.SaveChangesAsync` 指定操作者。

`DiscardChanges` 會清除 Change Tracker，以及目前追蹤聚合尚未派發的兩類事件。它適合在應用層確定放棄整個工作單元時使用。

`SaveChangesAsync` 過程中若拋出 `BusinessException`（包含由 domain event handler 拋出的），會先自動執行一次 `DiscardChanges` 再重新拋出。業務規則擋下的提交不會留下半套的追蹤狀態，但同一個 `DbContext` 上尚未提交的其他變更也會一併失去，捕捉此例外後需重新載入聚合。

## 明確交易

`UseTransactionAsync` 在目前沒有交易時建立交易，成功時提交，失敗時回復。若 `DbContext` 已有交易，方法會重用既有交易。執行委派仍須自行呼叫 `SaveChangesAsync`。

```csharp
await unitOfWork.UseTransactionAsync(
    async cancellationToken => {
        await customerRepository.CreateAsync(customer, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(actorId, cancellationToken).ConfigureAwait(false);
    },
    ct
).ConfigureAwait(false);
```

需要在交易範圍內取得回傳值，或需要直接操作交易物件時，改用泛型多載。委派會收到目前的 `IDbContextTransaction`，可用於建立 savepoint 或把交易交給其他需要它的 API。

```csharp
Guid customerId = await unitOfWork.UseTransactionAsync(
    async (transaction, cancellationToken) => {
        await customerRepository.CreateAsync(customer, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(actorId, cancellationToken).ConfigureAwait(false);

        return customer.Id;
    },
    ct
).ConfigureAwait(false);
```

重用既有交易時，方法不會提交也不會回復該交易，交由原本開啟交易的那一層負責。回復一律以 `CancellationToken.None` 執行，取消權杖已觸發時仍會完成回復。回復成功後重新拋出委派原本的例外。回復本身拋出例外時（例如連線已中斷），該例外會取代委派原本的例外。

## 可重試的交易

資料庫 provider 啟用具重試能力的 execution strategy（例如 SQL Server 的 `EnableRetryOnFailure`）時，自行開啟的交易必須包在 execution strategy 內。`UnitOfWorkExtensions.UseResilientTransactionAsync` 以 `CreateExecutionStrategy().ExecuteAsync` 包住 `UseTransactionAsync`，暫時性失敗時重新執行整個交易。

```csharp
await unitOfWork.UseResilientTransactionAsync(
    async cancellationToken => {
        Customer customer = await customerRepository.GetAsync(customerId, cancellationToken)
            .ConfigureAwait(false);
        customer.Rename(name, timeProvider);
        await unitOfWork.SaveChangesAsync(actorId, cancellationToken).ConfigureAwait(false);
    },
    ct
).ConfigureAwait(false);
```

需要在交易範圍內取得回傳值，或需要直接操作交易物件時，使用泛型多載。委派簽章與 `UseTransactionAsync` 的泛型多載相同，會收到目前的 `IDbContextTransaction`。重試時整個委派會重新執行，因此新增的聚合也應在委派內建立。

```csharp
Guid customerId = await unitOfWork.UseResilientTransactionAsync(
    async (transaction, cancellationToken) => {
        Customer customer = Customer.Create(name);
        await customerRepository.CreateAsync(customer, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(actorId, cancellationToken).ConfigureAwait(false);

        return customer.Id;
    },
    ct
).ConfigureAwait(false);
```

使用前確認下列條件：

- 委派在重試時會完整重新執行，應在委派內載入與修改資料，並可安全重複執行。
- 重試前會呼叫 `DiscardChanges`，清除 DbContext 的所有追蹤狀態與待派發事件，包含呼叫前已追蹤的實體。
- 呼叫前 DbContext 不可有未儲存的變更。execution strategy 具重試能力（`RetriesOnFailure` 為 `true`）且 `ChangeTracker.HasChanges()` 為 `true` 時，方法會在開啟交易、呼叫委派前擲出 `InvalidOperationException`，避免重試清除追蹤狀態時丟失這些變更。請先呼叫 `SaveChangesAsync` 儲存，或把修改移入委派。execution strategy 不具重試能力時不做此檢查。
- 只能在尚未開啟交易時呼叫。已有交易時，具重試能力的 execution strategy 會拒絕執行。
- 擴充方法需要 DomainKit 的 `UnitOfWork<TDbContext>` 實作，其他 `IUnitOfWork` 實作會拋出 `NotSupportedException`。

## 軟刪除判定

`DeleteAsync` 依實體能力決定刪除方式：

- 實作 `IHasDeletedTime` 的實體會寫入 `DeletedTime`，並保持資料列存在。
- 未實作 `IHasDeletedTime` 的實體會由 `DbSet.Remove` 實體刪除。

完整審計實體應搭配 `ConfigureFullAuditedEntity`。該設定會加入 `DeletedTime is null` 的全域查詢篩選。詳細行為見[聚合與審計](aggregates-and-auditing.md)。

## 聚合導覽載入

Repository 可從 EF Core model annotation 取得聚合邊界內必須載入的導覽路徑。使用 `HasAggregateIncludes` 集中宣告，避免每個 Repository 重複撰寫 `Include`。

```csharp
modelBuilder.Entity<SalesOrder>(builder => {
    builder.HasAggregateIncludes(includes => includes
        .Include(order => order.Lines, lines => lines
            .Include(line => line.Adjustments))
        .Include(order => order.BillingAddress));
});
```

`FindAsync`、`GetAsync`、`UpdateAsync` 與 `DeleteAsync` 都會套用這些路徑。Repository 仍可覆寫 `IncludeQuery` 加入無法由共用設定表達的查詢內容。

聚合 Include 應只涵蓋維護聚合一致性所需的導覽。清單與報表查詢使用投影，不應為了顯示欄位而擴張聚合載入範圍。

## 全域型別轉換

`ApplyDomainKitConventions` 提供 `DateTimeOffset` converter 的宿主接點。DomainKit 不依資料庫 provider 自動選擇儲存型別。

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
    base.ConfigureConventions(configurationBuilder);
    configurationBuilder.ApplyDomainKitConventions(new UtcDateTimeOffsetConverter());
}
```

`UtcDateTimeOffsetConverter` 將值正規化為 offset 0 並維持 `DateTimeOffset` 儲存型別。`UtcDateTimeConverter` 將 `DateTimeOffset` 轉成 UTC `DateTime`，適用於需要以 `DateTime` 儲存的 provider。未傳入 converter 時，DomainKit 不修改 EF Core 的預設對映。

## Enumeration 對映

採用 `CloudyWing.Enumeration` 的專案可在 `ConfigureConventions` 掃描包含 Enumeration 型別的組件。

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
    base.ConfigureConventions(configurationBuilder);
    configurationBuilder.ApplyDomainKitConventions(new UtcDateTimeOffsetConverter());
    configurationBuilder.ApplyEnumerationConventions(typeof(OrderStatus).Assembly);
}
```

每個具體 `EnumerationBase<TEnum, TValue>` 型別會使用其 `Value` 型別儲存。顯示名稱本地化見[錯誤契約與本地化](errors-and-localization.md)。

## 延伸閱讀

- [入門指南](getting-started.md)
- [聚合與審計](aggregates-and-auditing.md)
- [查詢與規約](querying-and-specifications.md)
- [多租戶與並行控制](tenancy-and-concurrency.md)
- [錯誤契約與本地化](errors-and-localization.md)

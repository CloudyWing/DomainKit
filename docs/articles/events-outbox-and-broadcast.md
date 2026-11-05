# 事件、Outbox 與廣播

DomainKit 將同交易業務副作用、可靠整合事件與執行實例通知分成不同通道。三者的交付保證與失敗處理不同，不應互換。

| 通道 | 發生時機 | 用途 | 交付特性 |
| --- | --- | --- | --- |
| Domain event | `SaveChanges` 寫入前 | 同一工作單元內的業務連動 | Handler 失敗會中止本次提交 |
| Integration event Outbox | 與業務資料同一次 `SaveChanges` | 可重試的跨模組或外部整合 | 持久化、租約認領、重試、handler 判重 |
| Event broadcast | 提交成功後 | 單一執行實例內的快取失效或畫面通知 | Best-effort，不持久化，訂閱者失敗不回拋 |

Integration event 會同時流向後兩個通道。除了寫入 Outbox，提交成功後也會自動發布至 Event broadcast，兩者的處理職責見「執行實例內廣播」。

## 定義事件

兩類事件都包含 `OccurredOn`。

```csharp
public sealed record CustomerRenamedDomainEvent(
    Guid CustomerId,
    DateTimeOffset OccurredOn
) : IDomainEvent;

public sealed record CustomerRenamedIntegrationEvent(
    Guid CustomerId,
    string Name,
    DateTimeOffset OccurredOn
) : IIntegrationEvent;
```

聚合行為透過受保護方法加入事件。

```csharp
public void Rename(string name, TimeProvider timeProvider) {
    Name = name;
    DateTimeOffset occurredOn = timeProvider.GetUtcNow();
    AddDomainEvent(new CustomerRenamedDomainEvent(Id, occurredOn));
    AddIntegrationEvent(new CustomerRenamedIntegrationEvent(Id, Name, occurredOn));
}
```

## Domain event

實作 `IDomainEventHandler<TEvent>`，並將 handler 所在組件交給 `AddDomainKit` 掃描。

```csharp
public sealed class CustomerRenamedDomainEventHandler
    : IDomainEventHandler<CustomerRenamedDomainEvent> {
    public Task HandleAsync(
        CustomerRenamedDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    ) {
        return Task.CompletedTask;
    }
}
```

Domain event 在 EF Core 寫入前派發。Handler 對同一個 `DbContext` 追蹤實體所做的變更會進入同次 `SaveChanges`。Handler 新增的 Domain event 會繼續派發，最多迭代 32 輪；未收斂時提交會失敗。

事件集合只在提交成功後清除。提交失敗時 Integration event 仍保留於聚合，供呼叫端決定是否重試或放棄工作單元。

## 啟用 Outbox

Outbox 需要模型、背景 dispatcher 與可由 DI 解析的 `IDbContextFactory<TDbContext>`。

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.ConfigureOutbox();
    modelBuilder.ApplyModuleConfigurations();
}
```

建立 migration 後，資料庫會包含 `OutboxMessages` 與 `ProcessedEvents`。

```csharp
services.AddDbContextFactory<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped
);

services.AddDomainKit<AppDbContext>(
    options => {
        options.EnableOutboxDispatcher = true;
        options.Outbox.BatchSize = 100;
        options.Outbox.PollingInterval = TimeSpan.FromSeconds(2);
    },
    handlerAssemblies: [typeof(CustomerRenamedIntegrationEventHandler).Assembly]
);
```

只設定 `EnableOutboxDispatcher` 不會自動建立資料表。只呼叫 `ConfigureOutbox` 但未啟動 dispatcher 時，事件會保留為 `Pending`，可在服務恢復後繼續派發。Outbox 由審計與事件攔截器建立訊息，因此啟用 Outbox 時不可將 `EnableAuditAndDomainEventInterceptor` 設成 `false`。

同一個 DbContext 啟用租戶 convention 時，應從 `ApplyTenantConventions` 排除 `OutboxMessage` 與 `ProcessedEvent`。Outbox 本身已保存租戶欄位，dispatcher 也需要跨租戶掃描待處理訊息。

```csharp
modelBuilder.ApplyTenantConventions(
    this,
    typeof(OutboxMessage),
    typeof(ProcessedEvent)
);
```

## Outbox 處理流程

1. `SavingChanges` 將聚合的 Integration event 序列化成 `OutboxMessage`，與業務資料同一次提交。
2. 背景服務依 `Id` 排序取得可處理訊息，再以條件式更新取得租約。
3. 派發前依訊息保存的 `TenantId` 還原租戶脈絡。
4. 每個 handler 成功處理後新增 `ProcessedEvent`，以 handler 型別與事件識別碼判重。
5. 全部 handler 與資料變更儲存成功後，訊息標成 `Processed`。

Outbox 提供至少一次處理語意，不提供全域嚴格順序。Handler 應維持冪等；若 handler 在資料庫提交前已呼叫外部系統，後續儲存失敗仍可能造成外部副作用重複。

## 重試與保留

| 選項 | 預設值 | 說明 |
| --- | ---: | --- |
| `PollingInterval` | 2 秒 | 沒有可處理資料或 dispatcher 發生錯誤時的等待時間 |
| `BatchSize` | 50 | 每輪最多認領與清理筆數 |
| `LockDuration` | 1 分鐘 | 訊息租約期限 |
| `BaseRetryDelay` | 5 秒 | 指數退避的基礎時間 |
| `MaxRetryDelay` | 15 分鐘 | 單次重試等待上限 |
| `MaxRetryCount` | 8 | 超過此次數後改為 `DeadLettered` |
| `RetentionPeriod` | 7 天 | 已處理訊息與判重資料的保留期間 |

事件型別以 assembly-qualified name 儲存。仍有待處理訊息時重新命名事件型別、移動組件或移除相容的建構資料，可能使反序列化失敗並進入重試或 Dead Letter 流程。部署前應先完成事件版本相容策略。

## Integration event handler

```csharp
public sealed class CustomerRenamedIntegrationEventHandler
    : IIntegrationEventHandler<CustomerRenamedIntegrationEvent> {
    private readonly IIntegrationEventContext eventContext;

    public CustomerRenamedIntegrationEventHandler(IIntegrationEventContext eventContext) {
        this.eventContext = eventContext;
    }

    public Task HandleAsync(
        CustomerRenamedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    ) {
        Guid eventId = eventContext.EventId;
        return Task.CompletedTask;
    }
}
```

`IIntegrationEventContext.EventId` 是 Outbox message 的識別碼，可作為外部冪等鍵。它只在 Outbox dispatcher 執行 handler 的 scope 內有意義。

## 執行實例內廣播

`IEventBroadcast` 預設由 `InProcessEventBroadcast` 實作。它只通知目前行程內的訂閱者，不會寫入 Outbox，也不會跨節點傳遞。

聚合的 Integration event 在提交成功後由審計與事件攔截器自動發布至此通道，宿主不需另行呼叫 `PublishAsync`。同一個 Integration event 因此會流向兩處，Outbox handler 承擔可靠的業務處理，此通道的訂閱者承擔快取失效這類可重複執行的通知。在訂閱者內重複實作 Outbox handler 的業務副作用，會使該副作用執行兩次。

```csharp
IDisposable subscription = eventBroadcast.Subscribe<CustomerCacheInvalidated>(
    (notification, cancellationToken) => cache.RemoveAsync(
        notification.CustomerId,
        cancellationToken
    )
);

await eventBroadcast.PublishAsync(
    new CustomerCacheInvalidated(customerId),
    ct
).ConfigureAwait(false);
```

應在訂閱者生命週期結束時釋放 `subscription`。單一訂閱者拋出的非取消例外不會回拋給發布端，其他訂閱者仍會繼續收到通知。`AddDomainKit` 註冊的執行個體會以 `ILogger<InProcessEventBroadcast>` 將該例外記錄為 Warning；自行以無參數建構子建立時不記錄日誌，需要記錄時改用接受 `ILogger<InProcessEventBroadcast>` 的建構子。取消例外會往外拋，發布端可據此中止。

訂閱與發布以通知的執行階段型別配對。`Subscribe<TNotification>` 只會收到型別完全相同的通知，訂閱基底型別或介面不會收到衍生型別的實例，通知型別應直接是要發布的具體型別。發布端只持有 `object` 參考時，可使用接受 `object` 的 `PublishAsync` 多載，配對規則相同，仍以實際型別查找訂閱者。沒有對應訂閱者的通知會被安靜捨棄，不視為錯誤。

跨節點廣播可由宿主替換 `IEventBroadcast`。需要攜帶租戶脈絡時，使用 `PublishEnvelopeAsync` 與 `SubscribeEnvelope`；接收端會在執行 handler 前暫時還原封套中的租戶。

## 診斷資料

Outbox 使用 `CloudyWing.DomainKit` 作為 `ActivitySource` 與 `Meter` 名稱，提供下列指標：

- `domainkit.outbox.dispatch.duration`
- `domainkit.outbox.deadletter`
- `domainkit.outbox.pending`

宿主可透過 OpenTelemetry 訂閱相同名稱的 ActivitySource 與 Meter。

## 延伸閱讀

- [聚合與審計](aggregates-and-auditing.md)
- [多租戶與並行控制](tenancy-and-concurrency.md)
- [錯誤契約與本地化](errors-and-localization.md)

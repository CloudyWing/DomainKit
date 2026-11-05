# 多租戶與並行控制

DomainKit 提供以 `AsyncLocal` 傳遞租戶脈絡的 `AmbientCurrentTenant`、EF Core 租戶 convention、寫入時的租戶填值攔截器，以及 provider 中立的 RowVersion 設定。

## 租戶脈絡

`ICurrentTenant` 暴露目前的 `TenantId`，並以 `Change` 暫時切換租戶。`AmbientCurrentTenant` 支援巢狀 scope，釋放 scope 後會還原上一層租戶。

```csharp
services.AddSingleton<ICurrentTenant, AmbientCurrentTenant>();
```

```csharp
using IDisposable scope = currentTenant.Change(tenantId);
await applicationService.ExecuteAsync(ct).ConfigureAwait(false);
```

`AmbientCurrentTenant` 應共用同一個服務執行個體，才能讓 Middleware、DbContext 與背景工作看到相同的非同步脈絡。

## DbContext 契約

啟用租戶篩選的 `DbContext` 實作 `ITenantDbContext`，將目前租戶提供給 EF Core query filter。

```csharp
public sealed class AppDbContext : DbContext, ITenantDbContext {
    private readonly ICurrentTenant currentTenant;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentTenant currentTenant
    ) : base(options) {
        this.currentTenant = currentTenant;
    }

    public Guid? CurrentTenantId => currentTenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyModuleConfigurations();
        modelBuilder.ApplyTenantConventions(
            this,
            typeof(GlobalPermission),
            typeof(OutboxMessage),
            typeof(ProcessedEvent)
        );
    }
}
```

`ApplyTenantConventions` 對所有具主鍵、非 owned、非衍生且未排除的實體執行下列設定：

- 加入必要的 `TenantId` shadow property。
- 加入目前租戶的全域查詢篩選。
- 將 `TenantId` 放到既有索引的第一欄。
- 保留先前設定的軟刪除 query filter，並與租戶條件合併。

此 convention 不以 `IMustHaveTenant` 判定是否套用。`IMustHaveTenant` 用來表達領域模型的租戶語意；不屬於租戶資料的型別必須透過 `excludedEntityTypes` 明確排除。啟用 Outbox 時應排除 `OutboxMessage` 與 `ProcessedEvent`，讓 dispatcher 能跨租戶掃描並依訊息內容還原租戶。

## 租戶範圍業務主鍵

同一個業務鍵可在不同租戶重複時，先使用 `HasTenantPrimaryKey` 標記業務鍵，再套用租戶 convention。

```csharp
modelBuilder.HasTenantPrimaryKey<NumberSequence>(nameof(NumberSequence.Key));
modelBuilder.ApplyTenantConventions(this);
```

最後的主鍵會由 `TenantId` 與指定的業務鍵屬性組成。

## 寫入租戶欄位

`TenantStampInterceptor` 會在新增資料前，把目前租戶寫入具有 `TenantId` shadow property 的實體。只要本次提交包含租戶實體，缺少租戶脈絡就會拋出 `InvalidOperationException`。

新增實體的 `TenantId` 一律以目前租戶覆寫，呼叫端先行指定的值不會保留。這是刻意的隔離設計，寫入的資料只會落在目前租戶範圍。需要代替其他租戶寫入時，以 `ICurrentTenant.Change` 切換租戶脈絡後再提交。

```csharp
services.AddScoped<TenantStampInterceptor>();
services.AddDbContextFactory<AppDbContext>(
    (serviceProvider, options) => options
        .UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<TenantStampInterceptor>()),
    ServiceLifetime.Scoped
);
```

`AddDomainKit<AppDbContext>` 會另外加入審計、事件與 RowVersion 攔截器，不會自動註冊 `ICurrentTenant` 或 `TenantStampInterceptor`。

## 背景工作與跨租戶處理

背景工作必須在建立 DbContext 或呼叫應用服務前切換租戶，並在每筆租戶工作完成後釋放 scope。

```csharp
foreach (Guid tenantId in tenantIds) {
    using IDisposable scope = currentTenant.Change(tenantId);
    await processor.ProcessAsync(tenantId, ct).ConfigureAwait(false);
}
```

Outbox 會保存事件產生時的 `TenantId`，派發前再還原租戶脈絡。詳見[事件、Outbox 與廣播](events-outbox-and-broadcast.md)。

## RowVersion

需要樂觀鎖的實體實作 `IHasRowVersion`，並在 model 設定呼叫 `ConfigureRowVersion`。

```csharp
public sealed class Customer : AggregateRootBase, IHasRowVersion {
    public byte[] RowVersion { get; private set; } = [];
}
```

```csharp
modelBuilder.Entity<Customer>(builder => {
    builder.ConfigureFullAuditedEntity();
    builder.ConfigureRowVersion();
});
```

預設設定為 provider 中立的 concurrency token。`RowVersionStampInterceptor` 會在新增與修改時產生新的 token。

SQL Server 應在 provider 專屬模型設定中覆寫為原生 `rowversion`：

```csharp
modelBuilder.Entity<Customer>()
    .Property(customer => customer.RowVersion)
    .IsRowVersion();
```

EF Core 將屬性標成 `ValueGenerated.OnAddOrUpdate` 後，DomainKit 攔截器會略過應用程式填值，由 SQL Server 產生版本值。其他 provider 可維持 app-managed token，或由宿主提供相應的 provider 設定。

## 並行衝突

`IUnitOfWork.SaveChangesAsync` 會把 `DbUpdateConcurrencyException` 轉成 `ConcurrencyConflictException`。應用層可依操作語意選擇重新載入、提示使用者重新整理，或放棄目前工作單元。DomainKit 不自動重試業務寫入。

## 查詢篩選注意事項

一般查詢會同時套用租戶與軟刪除篩選。`QueryService.IgnoreSoftDeletedFilter = true` 目前透過 EF Core `IgnoreQueryFilters()` 實作，因此會略過該實體的所有全域 query filter，包含租戶篩選。這個開關只能用於受控的系統管理流程，且查詢必須自行加入明確的租戶條件。

## 延伸閱讀

- [持久化與模型設定](persistence-and-modeling.md)
- [查詢與規約](querying-and-specifications.md)
- [事件、Outbox 與廣播](events-outbox-and-broadcast.md)

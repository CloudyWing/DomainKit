# 入門指南

本指南示範如何導入 DomainKit，包含定義聚合、設定 DbContext、註冊服務，以及進行持久化與查詢。套件目前以 .NET 10 為目標框架。核心套件 `CloudyWing.DomainKit` 在專案檔設定 `IsAotCompatible`，建置時會啟用 trimming 與 AOT 相容性分析器。`CloudyWing.DomainKit.EntityFrameworkCore` 未設定此屬性。此屬性啟用建置期的分析器檢查，Native AOT 發佈的實際相容性仍須由宿主驗證。

## 安裝

DomainKit 分為核心與 EF Core 兩個套件：

```bash
dotnet add package CloudyWing.DomainKit
dotnet add package CloudyWing.DomainKit.EntityFrameworkCore
```

## 定義聚合

聚合根繼承 `AggregateRootBase`（鍵為 `Guid`），以審計標記介面宣告需要自動填值的欄位，`IHasDisabledState` 之類的狀態標記介面則只統一屬性名稱與語意。

```csharp
using CloudyWing.DomainKit.Entities;

public class Customer : AggregateRootBase, IHasDisabledState, IHasDisplayOrder,
    IHasCreatedTime, IHasUpdatedTime, IHasCreator, IHasUpdater, IHasDeletedTime, IHasDeleter {
    public string Name { get; private set; } = "";

    public string City { get; private set; } = "";

    public int DisplayOrder { get; private set; }

    public bool IsDisabled { get; private set; }

    // 審計欄位由 SaveChanges 攔截器填值
    public DateTimeOffset CreatedTime { get; private set; }

    public Guid CreatorId { get; private set; }

    public DateTimeOffset UpdatedTime { get; private set; }

    public Guid UpdaterId { get; private set; }

    public DateTimeOffset? DeletedTime { get; private set; }

    public Guid? DeleterId { get; private set; }

    private Customer() { }

    public static Customer Create(string name) {
        return new Customer { Name = name };
    }
}
```

## 設定 DbContext

以 `IXxxDbContext` 介面宣告需要的資料集，在 `OnModelCreating` 套用審計欄位設定，並在 `ConfigureConventions` 套用 DomainKit 全域 convention。時間的實際儲存型別由宿主依 provider 選擇 converter。

```csharp
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using Microsoft.EntityFrameworkCore;

public interface ICustomerDbContext {
    DbSet<Customer> Customers { get; }
}

public sealed class AppDbContext : DbContext, ICustomerDbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyDomainKitConventions(new UtcDateTimeOffsetConverter());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Customer>(builder => {
            builder.ToTable("Customers");
            builder.ConfigureFullAuditedEntity();
        });
    }
}
```

`UtcDateTimeOffsetConverter` 將值正規化為 offset 0。未傳入 converter 時，DomainKit 不會改變 EF Core 的預設 `DateTimeOffset` 對映。

## 註冊服務

不啟用 Outbox 時，可使用一般 `AddDbContext` 註冊：

```csharp
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
services.AddDomainKit<AppDbContext>(
    handlerAssemblies: [typeof(Program).Assembly]
);
```

`AddDomainKit` 註冊 `IUnitOfWork`、審計與事件攔截器、RowVersion 攔截器、事件 dispatcher，以及行程內廣播服務。

啟用 Outbox 時，DbContext model 必須呼叫 `ConfigureOutbox`，DI 必須提供 `IDbContextFactory<AppDbContext>`，並在選項開啟背景 dispatcher。

```csharp
services.AddDbContextFactory<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped
);
services.AddDomainKit<AppDbContext>(
    options => options.EnableOutboxDispatcher = true,
    handlerAssemblies: [typeof(Program).Assembly]
);
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.ConfigureOutbox();
    modelBuilder.Entity<Customer>(builder => {
        builder.ToTable("Customers");
        builder.ConfigureFullAuditedEntity();
    });
}
```

加入設定後建立 EF Core migration，將 `OutboxMessages` 與 `ProcessedEvents` 納入資料庫。

## 持久化

Repository 操作聚合，`IUnitOfWork` 統一提交：

```csharp
Customer customer = Customer.Create("Acme");
await repository.CreateAsync(customer, cancellationToken: ct).ConfigureAwait(false);
await unitOfWork.SaveChangesAsync(actorId, ct).ConfigureAwait(false);
```

提交時傳入的 `actorId` 會作為審計使用者，未傳時退回 `IAuditUser.UserId`。詳見[聚合與審計](aggregates-and-auditing.md)。

## 查詢

```csharp
IReadOnlyList<CustomerListItem> items = await queryService.GetListAsync(
    customer => new CustomerListItem(customer.Id, customer.Name),
    customer => !customer.IsDisabled,
    query => query.OrderBy(customer => customer.Name),
    ct
).ConfigureAwait(false);
```

更多查詢與規約用法見[查詢與規約](querying-and-specifications.md)。

## 下一步

- [聚合與審計](aggregates-and-auditing.md)
- [持久化與模型設定](persistence-and-modeling.md)
- [查詢與規約](querying-and-specifications.md)
- [多租戶與並行控制](tenancy-and-concurrency.md)
- [事件、Outbox 與廣播](events-outbox-and-broadcast.md)
- [錯誤契約與本地化](errors-and-localization.md)

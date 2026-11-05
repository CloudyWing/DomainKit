# CloudyWing.DomainKit

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE.md)

此專案是我把我過往 Domain 架構往 DDD 演進後的結果，並讓 AI 討論並補足一些不夠熟悉的部分，作為後續研究，配合自身需求維護，可能一加功能就是破壞性變更的大改版，不特別建議其他人直接相依。

DomainKit 是一組可重用的 .NET 領域層基礎建設，提供 DDD 風格的 Entity、Aggregate Root、雙軌領域事件、審計填值、多租戶與樂觀鎖，以及 EF Core 的持久化、查詢、規約和 Outbox 基礎建設。設計前提為真 Code First。實體即 EF 對映型別並同時是充血聚合根，不使用資料物件 wrapper。

## 套件內容

- **[`src/DomainKit`](./src/DomainKit/)（`CloudyWing.DomainKit`）**
  核心領域基礎建設，零 EF 相依。包含 `Entity<TKey>`、`AggregateRoot<TKey>`、審計與 RowVersion 標記介面、`IHasExternalKey<TKey>`、租戶脈絡、雙軌事件、規約、分頁與業務例外契約。
- **[`src/DomainKit.EntityFrameworkCore`](./src/DomainKit.EntityFrameworkCore/)（`CloudyWing.DomainKit.EntityFrameworkCore`）**
  EF Core 持久化與基礎服務。包含 Repository、Unit of Work、QueryService、審計與並行控制攔截器、租戶 convention、Aggregate Include、Enumeration 對映、Outbox dispatcher、行程內廣播與 DI 註冊。

## Installation

```bash
dotnet add package CloudyWing.DomainKit
dotnet add package CloudyWing.DomainKit.EntityFrameworkCore
```

## Quick Start

### 定義聚合與 DbContext

聚合根直接繼承 `AggregateRootBase`（鍵為 `Guid`），並以標記介面宣告審計欄位與狀態屬性。

```csharp
using CloudyWing.DomainKit.Entities;
using CloudyWing.DomainKit.EntityFrameworkCore.Modeling;
using Microsoft.EntityFrameworkCore;

public class Customer : AggregateRootBase, IHasDisabledState,
    IHasCreatedTime, IHasUpdatedTime, IHasCreator, IHasUpdater, IHasDeletedTime, IHasDeleter {
    public string Name { get; private set; } = "";
    public bool IsDisabled { get; private set; }
    // 充血行為與審計欄位
}

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

### 註冊服務

`AddDomainKit<TDbContext>` 註冊 UnitOfWork、審計／事件與 RowVersion 攔截器、事件 dispatcher 及行程內廣播服務。`handlerAssemblies` 指定要掃描事件 handler 的組件。

```csharp
services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
services.AddDomainKit<AppDbContext>(handlerAssemblies: [typeof(Program).Assembly]);
```

### Repository 與 UnitOfWork

`Repository` 操作聚合，`IUnitOfWork` 統一提交（延後單次提交為預設）。提交時傳入的 `actorId` 會作為審計使用者，未傳時退回 `IAuditUser.UserId`。

```csharp
await repository.CreateAsync(customer, cancellationToken: ct).ConfigureAwait(false);
await unitOfWork.SaveChangesAsync(actorId, ct).ConfigureAwait(false);
```

需要明確交易範圍時，使用 `IUnitOfWork.UseTransactionAsync`。

### 查詢

`GetSingleAsync` 的語意是「必須剛好一筆」。當查詢需求是「依排序取第一筆」時，改用 `GetFirstAsync` 或 `GetFirstOrDefaultAsync`。

```csharp
string nextName = await queryService.GetFirstAsync(
    customer => customer.Name,
    customer => !customer.IsDisabled,
    query => query.OrderBy(customer => customer.DisplayOrder),
    ct
).ConfigureAwait(false);
```

若查無資料但不想拋例外，改用 `GetFirstOrDefaultAsync`，無資料時回傳 `null`。

自行組 `IQueryable` 時，`ToPagedListAsync` 提供頁碼分頁，`ToKeysetPageAsync` 以唯一且不可變的排序鍵提供 keyset 分頁，回傳 `KeysetPage<TItem, TKey>` 的 `Items`、`NextKey` 與 `HasMore`。

### 規約 Specifications

`Specification<T>` 只負責條件，`PagedSpecification<T>` 再加上排序與分頁，可直接交給 `IQueryService<TEntity>` 的規約多載。

```csharp
ISpecification<Customer> activeCustomers = new Specification<Customer>(customer => !customer.IsDisabled);
ISpecification<Customer> taipeiCustomers = new Specification<Customer>(customer => customer.City == "Taipei");
ISpecification<Customer> specification = activeCustomers.And(taipeiCustomers);

IPagedSpecification<Customer> pagedSpecification = PagedSpecification<Customer>.Create(
    specification,
    query => query.OrderBy(customer => customer.Name),
    1,
    20
);

PagedList<CustomerListItem> page = await queryService.GetPagedListAsync(
    customer => new CustomerListItem(customer.Id, customer.Name),
    pagedSpecification,
    ct
).ConfigureAwait(false);
```

執行期才決定數量的規約可用 `AllOf`（AND）與 `AnyOf`（OR）組合，空集合分別回傳 `Specification<T>.True` 與 `Specification<T>.False`。習慣以離散參數呼叫時，可透過 `QueryServiceSpecificationExtensions` 組成規約後委派核心多載。

### 審計與雙軌事件

- 聚合實作 `IHasCreatedTime` / `IHasCreator` 等介面後，`SaveChanges` 攔截器在提交時自動填入時間與操作者，不需逐操作手動指定。
- 聚合分別收集 domain event 與 integration event。Domain event 於提交前派發，handler 變更納入同一次 `SaveChanges`。Integration event 可與業務資料同交易寫入 Outbox，再由背景服務重試派發。

Outbox、多租戶及 provider 專屬 RowVersion 設定請依完整文件導入，避免只開啟部分元件。

## 版本與相容性

目標框架為 .NET 10。版號由 MinVer 依 git tag（前綴 `v`）推導，發版即打 tag。

`0.x` 期間不保證公開 API 穩定，次版號遞增即可能包含破壞性變更，變更內容記於 [CHANGELOG.md](./CHANGELOG.md)。相依此套件時建議釘死明確版號，避免使用範圍或浮動版號。

公開 API 以 `PublicAPI.Shipped.txt` 宣告，並由 `Microsoft.CodeAnalysis.PublicApiAnalyzers` 於建置時檢查。未同步更新該檔的公開成員增刪會使建置失敗。

## Documentation

完整文件：<https://cloudywing.github.io/DomainKit/>

- [入門指南](./docs/articles/getting-started.md)
- [聚合與審計](./docs/articles/aggregates-and-auditing.md)
- [持久化與模型設定](./docs/articles/persistence-and-modeling.md)
- [查詢與規約](./docs/articles/querying-and-specifications.md)
- [多租戶與並行控制](./docs/articles/tenancy-and-concurrency.md)
- [事件、Outbox 與廣播](./docs/articles/events-outbox-and-broadcast.md)
- [錯誤契約與本地化](./docs/articles/errors-and-localization.md)

## License

本專案授權見 [LICENSE.md](./LICENSE.md)。

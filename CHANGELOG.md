# Changelog

## v0.1.0 (2026-08-16)

### New Features

- 新增 `CloudyWing.DomainKit` 核心套件，零 EF Core 相依：
  - `Entity<TKey>` 與 `AggregateRoot<TKey>` 提供富血聚合基底，以型別加識別碼判定相等；`EntityBase` 與 `AggregateRootBase` 於建構時以 `Guid.CreateVersion7()` 產生識別碼。
  - 審計標記介面 `IHasCreatedTime`、`IHasCreator`、`IHasUpdatedTime`、`IHasUpdater`、`IHasDeletedTime` 與 `IHasDeleter`。
  - 狀態標記介面 `IHasDisabledState` 與 `IHasDisplayOrder`，統一停用狀態與顯示順序的屬性名稱。
  - `IHasExternalKey<TKey>` 表達與資料庫主鍵解耦的對外辨識鍵。
  - `IHasRowVersion` 與 `IMustHaveTenant` 表達樂觀鎖與租戶語意，`ICurrentTenant` 與 `AmbientCurrentTenant` 以 `AsyncLocal` 傳遞租戶脈絡並支援巢狀 scope。
  - 雙軌事件契約 `IDomainEvent` 與 `IIntegrationEvent`，以及對應的 handler 與 dispatcher 介面。
  - 規約子系統 `ISpecification<T>`、`IPagedSpecification<T>`、`Specification<T>` 與 `PagedSpecification<T>`，支援 `And`、`Or`、`Not` 組合，以及 `AndIf`、`OrIf`、`AndIfHasValue` 與 `OrIfHasValue` 條件式組合。
  - 分頁型別 `PagedList<TItem>` 與 `PagingMetadata`。
  - 業務例外契約 `BusinessException`、`EntityNotFoundException`、`ConcurrencyConflictException` 與 `AppErrorType` 錯誤分類。
- 新增 `CloudyWing.DomainKit.EntityFrameworkCore` 持久化套件：
  - `Repository<TDbContext, TEntity, TKey>` 操作聚合，支援延後提交、依 `IHasDeletedTime` 判定的軟刪除，以及以 Domain Key 查詢。
  - `IUnitOfWork` 統一提交，提供 `UseTransactionAsync` 明確交易與 `DiscardChanges` 放棄工作單元。
  - `IQueryService<TEntity>` 提供投影查詢，含 `AnyAsync`、`CountAsync`、`GetSingleAsync`、`GetFirstAsync`、`GetListAsync`、`GetPagedListAsync` 與 `GetGroupedListAsync`。
  - `AuditedQueryService` 以 LEFT JOIN 帶出建立者與更新者，實體與使用者表之間不需設定外鍵。
  - `AuditAndDomainEventInterceptor` 與 `RowVersionStampInterceptor` 在提交時集中填入審計值、派發雙軌事件並產生並行 token。
  - 租戶 convention `ApplyTenantConventions` 與 `HasTenantPrimaryKey`，搭配 `TenantStampInterceptor` 於新增資料時寫入租戶欄位。
  - `HasAggregateIncludes` 以 model annotation 集中宣告聚合邊界的導覽載入路徑。
  - `ApplyEnumerationConventions` 對映 `CloudyWing.Enumeration` 型別，`EnumerationLocalizer<TEnum>` 支援原生 `enum` 與 Enumeration 型別的顯示名稱在地化，並處理 `[Flags]` 複合值。
  - Transactional outbox：`ConfigureOutbox` 建立模型，`OutboxDispatcherHostedService` 提供租約認領、指數退避重試、dead letter 與 handler 判重。
  - `IEventBroadcast` 提供進程內廣播通道，可由宿主替換為跨節點實作。
  - `AddDomainKit<TDbContext>` 一次註冊上述服務並掃描指定組件中的事件 handler。
  - `ApplyDomainKitConventions` 提供 `DateTimeOffset` 儲存型別的宿主接點，內建 `UtcDateTimeOffsetConverter` 與 `UtcDateTimeConverter`。

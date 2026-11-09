# Changelog

## v0.2.0 (2026-11-09)

### Breaking Changes

- `TenantStampInterceptor` 建構子傳入 `null` 的 `ICurrentTenant` 時，改為立即擲出 `ArgumentNullException`。
- 同步 `SaveChanges()` 與 `SaveChangesAsync()` 一致，會執行審計填值、RowVersion 更新、領域事件派發與 Outbox 寫入，原本同步路徑會略過這些處理。同步路徑以阻塞方式等待領域事件 handler 與廣播通道完成。
- 審計時間與軟刪除時間改用 `TimeProvider`。`AuditAndDomainEventInterceptor` 使用注入的 `TimeProvider`，`Repository` 的原建構子改取 DbContext 應用程式服務容器註冊的 `TimeProvider`，未註冊時使用 `TimeProvider.System`。宿主註冊自訂 `TimeProvider` 時，寫入的時間會隨之改變。
- `AddDomainKit` 掃描 handler 組件時，遇到部分型別無法載入，不再擲出 `ReflectionTypeLoadException`，改為只註冊可載入的型別並略過其餘型別。部署時須確認 handler 組件的相依性完整，避免 handler 被略過。
- `InProcessEventBroadcast` 的訂閱者處理失敗時改為記錄 Warning 日誌，仍不阻斷其他訂閱者與發布端。

### New Features

- `CloudyWing.DomainKit` 核心套件：
  - `Specification<T>.True` 與 `Specification<T>.False` 恆真與恆假規約，以及規約集合的 `AllOf` 與 `AnyOf` 組合擴充方法。
  - `KeysetPage<TItem, TKey>` 游標分頁結果型別，提供 `Items`、`NextKey` 與 `HasMore`。
  - 宣告 `IsAotCompatible`，建置時檢查 trim 與 AOT 警告。
- `CloudyWing.DomainKit.EntityFrameworkCore` 持久化套件：
  - `ToKeysetPageAsync` 擴充方法，提供首頁與依游標取後續頁的兩個多載。
  - `UseResilientTransactionAsync` 擴充方法，在 EF Core execution strategy 內以交易執行作業，暫時性失敗時重試整個交易，提供無回傳值與回傳 `TResult` 的兩個多載。execution strategy 具重試能力且 DbContext 已有未儲存的變更時，呼叫前即擲出 `InvalidOperationException`，避免重試時靜默丟棄這些變更。
  - `IOutboxAdministrator` 提供跨租戶的 `GetStatisticsAsync` 狀態統計與 `RequeueDeadLetteredAsync` Dead Letter 重新排入，統計結果型別為 `OutboxStatistics`。
  - `AddDomainKit` 新增以 `OptionsBuilder<OutboxOptions>` 設定 Outbox 的多載，驗證各選項範圍並支援 configuration 綁定與 `ValidateOnStart`，既有多載維持直接註冊選項實例。
  - 可選用的具名查詢篩選。`UseDomainKitNamedQueryFilters` 啟用後，軟刪除與租戶篩選分別註冊為 `DomainKit.SoftDelete` 與 `DomainKit.Tenant`，名稱常數見 `DomainKitQueryFilterNames`。
  - `QueryService` 新增 `IgnoreSoftDeleteFilterOnly`，只略過軟刪除篩選並保留租戶篩選。
  - `Repository` 新增接受 `TimeProvider` 的建構子多載。
  - `InProcessEventBroadcast` 新增接受 `ILogger<InProcessEventBroadcast>` 的建構子，透過 DI 建立時會使用已註冊的 Logger。

### Bug Fixes

- 修正 Outbox 訊息停留在 `Processing` 狀態時，租約到期後無法重新認領。失敗處理也不再把已由其他執行個體完成的訊息退回待派發。
- 修正交易回復沿用已取消的權杖，導致原始例外被 `OperationCanceledException` 取代。回復改以 `CancellationToken.None` 執行。
- 修正大頁碼換算略過筆數時溢位，導致回傳錯誤頁面。略過筆數改以 `long` 計算並收斂至 `int.MaxValue`。

### Performance

- 快取聚合根事件集合的唯讀檢視、實體代理型別、事件處理器的封閉泛型型別，以及 Outbox 的事件型別解析與判重鍵。
- 快取 `Repository` 的聚合 Include 路徑，避免每次查詢重新反序列化。
- 主鍵查詢條件的鍵值改為可參數化的 closure 捕捉。
- 過期的 Outbox 與判重紀錄改以資料庫端批次刪除，每個資料表每輪最多刪除 `BatchSize` 筆。

### Documentation

- 補上可重試交易的泛型多載與未儲存變更限制、`KeysetPage` 建構子、`OutboxStatistics` 屬性、handler 組件部分載入、核心套件 AOT 宣告、`ApplyDomainKitConventions` 只依轉換器型別建立新實例，以及 `TenantStampInterceptor` 一律以目前租戶覆寫新增實體 `TenantId` 的說明。
- 補充未排序的分頁結果順序不穩定的說明。
- 修正交易回復失敗、廣播取消例外與 `GetSingleOrDefaultAsync` 的行為描述，並修正入門與 RowVersion 範例使其符合泛型條件。
- 新增 ADR-0029，記錄具名查詢篩選採 opt-in 的決策。

### Build / CI

- 兩個套件共用的打包設定集中至 `src/Directory.Build.props`，各專案檔只保留各自的描述與專屬設定。
- 改用 SDK 內建的 SourceLink，移除 `Microsoft.SourceLink.GitHub` 明確參考，符號檔仍保留原始碼對應。
- CI 改用 devops-templates 的 reusable workflow，並新增 `ubuntu-latest` 的建置與測試矩陣。
- NuGet 發佈改為 push 版本標籤時，經驗證與建立 GitHub Release 後以 Trusted Publishing 發佈，不再需要 `NUGET_API_KEY`。發佈前檢查 nupkg 版號與標籤一致，手動觸發時只執行 probe 驗證綁定而不發佈。
- 文件網站改以 GitHub Pages artifact 部署，需將 Pages Source 設為 GitHub Actions。
- 新增 Dependabot 設定。

## v0.1.0 (2026-08-19)

### New Features

- 新增 `CloudyWing.DomainKit` 核心套件，零 EF Core 相依：
  - `Entity<TKey>` 與 `AggregateRoot<TKey>` 提供充血聚合基底，以型別加識別碼判定相等；`EntityBase` 與 `AggregateRootBase` 於建構時以 `Guid.CreateVersion7()` 產生識別碼。
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
  - `IEventBroadcast` 提供行程內廣播通道，可由宿主替換為跨節點實作。
  - `AddDomainKit<TDbContext>` 一次註冊上述服務並掃描指定組件中的事件 handler。
  - `ApplyDomainKitConventions` 提供 `DateTimeOffset` 儲存型別的宿主接點，內建 `UtcDateTimeOffsetConverter` 與 `UtcDateTimeConverter`。

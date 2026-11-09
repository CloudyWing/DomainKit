# 查詢與規約

## QueryService

`IQueryService<TEntity>` 提供唯讀查詢，以投影選擇器回傳結果型別，避免將實體洩漏至上層。

| 方法 | 語意 |
| --- | --- |
| `AnyAsync` / `CountAsync` | 存在性與計數 |
| `GetSingleAsync` | 必須剛好一筆，零筆或多於一筆都拋例外 |
| `GetSingleOrDefaultAsync` | 至多一筆，零筆時回傳預設值，多於一筆時拋例外 |
| `GetFirstAsync` / `GetFirstOrDefaultAsync` | 依排序取第一筆 |
| `GetListAsync` | 清單投影 |
| `GetPagedListAsync` | 分頁投影 |
| `GetGroupedListAsync` | 分組、彙總、Having 與排序投影 |

`GetSingleAsync` 在零筆或多於一筆時拋例外，`GetSingleOrDefaultAsync` 只在多於一筆時拋例外。「依排序取第一筆」請改用 `GetFirstAsync`：

```csharp
string nextName = await queryService.GetFirstAsync(
    customer => customer.Name,
    customer => !customer.IsDisabled,
    query => query.OrderBy(customer => customer.DisplayOrder),
    ct
).ConfigureAwait(false);
```

查無資料但不想拋例外時，改用 `GetFirstOrDefaultAsync`，無資料時回傳 `null`。

## 建立查詢服務

兩個泛型參數的 `QueryService<TDbContext, TEntity>` 提供一般投影查詢。三個泛型參數的版本再加入 Domain Key 查詢。

```csharp
public sealed class CustomerQueryService
    : QueryService<AppDbContext, Customer, string> {
    public CustomerQueryService(AppDbContext dbContext)
        : base(dbContext) {
    }
}
```

實體實作 `IHasExternalKey<string>` 時，`AnyAsync(key)`、`GetAsync(selector, key)` 與 `FindAsync(selector, key)` 會依 `ExternalKey` 查詢。其餘實體使用 EF Core model 的單一主鍵。複合主鍵情境須覆寫 `CreateKeyPredicate`。

## 規約 Specifications

`Specification<T>` 封裝可命名、可組合的查詢條件，`PagedSpecification<T>` 再加上排序與分頁。

```csharp
ISpecification<Customer> active = new Specification<Customer>(customer => !customer.IsDisabled);
ISpecification<Customer> taipei = new Specification<Customer>(customer => customer.City == "Taipei");
ISpecification<Customer> spec = active.And(taipei);
```

`And`、`Or` 與 `Not` 構成完整的組合運算，`And` 與 `Or` 各有接受規約與接受條件運算式的多載，不需為了組合而先把運算式包成規約。組合結果一律是新的 `ISpecification<T>`，來源規約不受影響，可安全重複使用同一個具名規約。

```csharp
ISpecification<Customer> outsideTaipei = taipei.Not();
ISpecification<Customer> reachable = active.Or(customer => customer.HasContactEmail);
```

規約可直接交給 `IQueryService<TEntity>` 的規約多載：

```csharp
IPagedSpecification<Customer> paged = PagedSpecification<Customer>.Create(
    spec,
    query => query.OrderBy(customer => customer.Name),
    1,
    20
);

PagedList<CustomerListItem> page = await queryService.GetPagedListAsync(
    customer => new CustomerListItem(customer.Id, customer.Name),
    paged,
    ct
).ConfigureAwait(false);
```

規約只承載查詢條件與排序分頁，不含投影。投影由 `selector` 顯式指定。查詢經導覽屬性的條件與投影由 EF Core 自動 join，不需要 `Include`；載入聚合圖供領域行為操作屬於 Repository 的職責，見[持久化與模型設定](persistence-and-modeling.md)。習慣以離散參數呼叫時，可透過 `QueryServiceSpecificationExtensions` 組成規約後委派核心多載。

## 條件式組合

搜尋畫面的條件多半是選填的，`SpecificationExtensions` 提供條件成立才組合的擴充方法，讓拼裝流程維持單一運算式，不必用 `if` 逐段改寫規約變數。

| 擴充方法 | 組合時機 |
| --- | --- |
| `AndIf` / `OrIf` | 傳入的 `bool` 為 `true` |
| `AndIfHasValue` / `OrIfHasValue` | 傳入的值存在 |

```csharp
ISpecification<Customer> specification = new Specification<Customer>(customer => !customer.IsDisabled)
    .AndIfHasValue(criteria.City, city => customer => customer.City == city)
    .AndIfHasValue(criteria.RegisteredAfter, date => customer => customer.CreatedTime >= date)
    .AndIf(criteria.OnlyContactable, customer => customer.HasContactEmail);
```

`AndIfHasValue` 與 `OrIfHasValue` 把 `null` 視為無值，字串另外把空字串與純空白也視為無值，因此文字搜尋欄位不需要在呼叫端先做一次 `IsNullOrWhiteSpace`。其他型別只要不是 `null` 就算有值，`0` 與 `false` 都會觸發組合。條件不成立時原樣回傳來源規約，不會產生恆真條件。

工廠委派只在值存在時執行，可直接使用該值的非 null 形式建立條件運算式。

## 組合規約集合

規約數量在執行期才決定時，以 `AllOf` 與 `AnyOf` 將 `IEnumerable<ISpecification<T>>` 組成單一規約。`AllOf` 以 AND 組合，`AnyOf` 以 OR 組合。

```csharp
IEnumerable<ISpecification<Customer>> filters = selectedCities
    .Select(city => new Specification<Customer>(customer => customer.City == city));

ISpecification<Customer> inSelectedCities = filters.AnyOf();
ISpecification<Customer> specification = active.And(inSelectedCities);
```

集合為空時，`AllOf` 回傳 `Specification<T>.True`，`AnyOf` 回傳 `Specification<T>.False`，與 AND、OR 的單位元素一致。`Specification<T>.True` 與 `Specification<T>.False` 也可單獨使用，例如作為不限制條件的預設規約，兩者皆為可共用的不可變執行個體。集合含 `null` 元素時拋出 `ArgumentException`。

## 分頁結果

`GetPagedListAsync` 回傳 `PagedList<TItem>`，`Items` 是本頁投影結果，`Metadata` 是分頁統計。

| 成員 | 說明 |
| --- | --- |
| `PageNumber` / `PageSize` | 查詢時指定的頁碼與每頁筆數 |
| `TotalCount` | 套用條件後的總筆數 |
| `TotalPages` | 依總筆數與每頁筆數計算的總頁數，無資料時為 `0` |
| `HasPreviousPage` / `HasNextPage` | 供分頁控制項判斷上下頁 |

頁碼與每頁筆數必須為正整數，傳入 `0` 或負數會拋出 `ArgumentOutOfRangeException`。頁碼從 `1` 起算。頁碼換算的略過筆數超過 `int.MaxValue` 時以 `int.MaxValue` 計算，回傳空白頁。

`GetPagedListAsync` 的 `orderBy` 為 `null` 時不套用排序，資料庫不保證各頁的資料順序，翻頁時可能重複或遺漏資料。分頁查詢應傳入以唯一且穩定的鍵排序的 `orderBy`，排序欄位不唯一時再以 `ThenBy` 補上主鍵。

查詢無法由 `IQueryService<TEntity>` 表達而需自行組 `IQueryable` 時，用 `ToPagedListAsync` 取得同樣形狀的結果，回傳型別與 QueryService 一致，上層不必分辨資料來自哪一種查詢方式。

```csharp
PagedList<CustomerListItem> page = await dbContext.Customers
    .Where(customer => !customer.IsDisabled)
    .OrderBy(customer => customer.Name)
    .Select(customer => new CustomerListItem(customer.Id, customer.Name))
    .ToPagedListAsync(pageNumber, pageSize, ct)
    .ConfigureAwait(false);
```

它會先執行一次 `CountAsync` 再取本頁資料，共兩次資料庫往返。排序須在呼叫前完成，未排序的來源分頁結果不穩定。

## Keyset 分頁

資料量大且只需依序翻頁時，用 `ToKeysetPageAsync` 以排序鍵定位下一頁。查詢條件只比較排序鍵，不需要 `Skip` 掃過前面的資料，也不另外執行 `CountAsync`。

```csharp
KeysetPage<CustomerListItem, Guid> page = await dbContext.Customers
    .Where(customer => !customer.IsDisabled)
    .Select(customer => new CustomerListItem(customer.Id, customer.Name))
    .ToKeysetPageAsync(item => item.Id, 50, ct)
    .ConfigureAwait(false);

while (page.HasMore) {
    page = await dbContext.Customers
        .Where(customer => !customer.IsDisabled)
        .Select(customer => new CustomerListItem(customer.Id, customer.Name))
        .ToKeysetPageAsync(item => item.Id, page.NextKey, 50, ct)
        .ConfigureAwait(false);
}
```

`KeysetPage<TItem, TKey>` 的成員如下。

| 成員 | 說明 |
| --- | --- |
| `Items` | 本頁資料，依排序鍵遞增 |
| `NextKey` | 本頁最後一筆的排序鍵，查詢下一頁時傳入；本頁沒有資料時為預設值 |
| `HasMore` | 本頁之後是否仍有資料 |

一般查詢直接使用 `ToKeysetPageAsync` 的回傳值。需要自行建立結果時（例如測試中替代查詢方法），使用公開建構子 `KeysetPage<TItem, TKey>(items, nextKey, hasMore)`，三個參數依序對應上表的 `Items`、`NextKey` 與 `HasMore`，`items` 不可為 `null`。

```csharp
IReadOnlyList<CustomerListItem> items = [first, second];
KeysetPage<CustomerListItem, Guid> page = new(items, second.Id, hasMore: false);
```

使用限制如下：

- 排序鍵必須唯一且不可變。`ToKeysetPageAsync` 以排序鍵遞增排序，並取代來源查詢既有的排序。
- 排序鍵選擇器會在 SQL 與物化後的項目上各執行一次，應直接指向欄位或投影屬性。
- 只支援以 `NextKey` 往下一頁移動，無法跳到任意頁碼。資料在游標前後新增或刪除時，後續頁面會反映最新資料，不提供快照一致性。
- 數值與日期時間型別直接比較，字串以 `string.Compare` 比較，其他型別以 `CompareTo` 比較，能否轉譯為 SQL 取決於 provider。
- 參考型別的排序鍵可傳入 `null` 作為第一頁；實值型別的第一頁請使用不含 `afterKey` 的多載。

## 分組查詢

`GetGroupedListAsync` 先套用實體篩選，再執行 `GroupBy` 與彙總投影。`havingPredicate` 套用在彙總投影之後。

```csharp
IReadOnlyList<CustomerCountByCity> counts = await queryService.GetGroupedListAsync(
    customer => customer.City,
    group => new CustomerCountByCity(group.Key, group.Count()),
    customer => !customer.IsDisabled,
    result => result.Count >= 10,
    query => query.OrderByDescending(result => result.Count),
    ct
).ConfigureAwait(false);
```

彙總投影成員只能來自分組鍵或 SQL 可轉譯的彙總計算。自訂 .NET 方法通常無法由 EF Core 轉譯，應在查詢物化後另行處理。

## 審計使用者投影

`AuditedQueryService<TDbContext, TEntity, TUserEntity>` 提供建立者與更新者的 LEFT JOIN 基底。衍生類別指定使用者來源與鍵選擇器，再從 `AuditedQueryEntity.Entity`、`Creator` 與 `Updater` 投影公開結果。

```csharp
public sealed class CustomerAuditQueryService
    : AuditedQueryService<AppDbContext, Customer, AppUser> {
    public CustomerAuditQueryService(AppDbContext dbContext)
        : base(dbContext) {
    }

    protected override IQueryable<AppUser> Users => DbContext.Users;
    protected override Expression<Func<Customer, Guid>> CreatorIdSelector => customer => customer.CreatorId;
    protected override Expression<Func<Customer, Guid?>>? UpdaterIdSelector => customer => customer.UpdaterId;
    protected override Expression<Func<AppUser, Guid>> UserIdSelector => user => user.Id;
}
```

選擇器必須直接指向屬性，不能是運算式或方法呼叫。

## 軟刪除篩選

QueryService 預設使用 `AsNoTracking` 並保留 EF Core 全域 query filter。`IgnoreSoftDeletedFilter = true` 會呼叫 `IgnoreQueryFilters()`，因此不只略過軟刪除條件，也會略過同一實體的租戶等其他全域條件。

> [!WARNING]
> `IgnoreSoftDeletedFilter = true` 會一併略過租戶篩選。此設定只能用於受控的管理流程，並由呼叫端補上必要的隔離條件。

只需要包含已軟刪除資料時，模型先呼叫 `UseDomainKitNamedQueryFilters` 將篩選註冊為具名篩選，再設定 `IgnoreSoftDeleteFilterOnly = true`。此開關只略過 `DomainKit.SoftDelete`，租戶等其他篩選照常套用；兩個開關同時為 `true` 時以 `IgnoreSoftDeletedFilter` 為準。設定方式與限制見[多租戶與並行控制](tenancy-and-concurrency.md)。`IgnoreSoftDeleteFilterOnly` 宣告於 `QueryService` 基底類別，不屬於 `IQueryService<TEntity>` 介面。

## 延伸閱讀

- [持久化與模型設定](persistence-and-modeling.md)
- [多租戶與並行控制](tenancy-and-concurrency.md)

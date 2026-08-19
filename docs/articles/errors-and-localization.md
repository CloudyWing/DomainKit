# 錯誤契約與本地化

DomainKit 以 `BusinessException` 表達可預期的領域或應用錯誤，並提供穩定的錯誤分類與選配錯誤碼。套件不直接決定 HTTP 狀態碼、Problem Details 格式或 UI 呈現方式，這些對映由應用層負責。

## BusinessException

只傳入訊息時，錯誤分類預設為 `AppErrorType.BusinessRule`。

```csharp
throw new BusinessException("訂單目前狀態不可取消。");
```

需要穩定的機器可讀契約時，同時指定分類與錯誤碼。

```csharp
throw new BusinessException(
    "訂單已由其他流程處理。",
    AppErrorType.Conflict,
    "sales-order.state-conflict"
);
```

`AppErrorType` 提供下列分類：

- `Validation`
- `BusinessRule`
- `NotFound`
- `Conflict`
- `Forbidden`

`None` 只代表沒有錯誤，不可傳給 `BusinessException` 的分類建構式。

應用層應以 `ErrorType` 決定回應類別，以 `ErrorCode` 作為前端分支或本地化鍵。不要依賴錯誤訊息文字判斷程式流程。

## 內建例外

`EntityNotFoundException` 保存 `EntityName` 與 `Key`，錯誤分類為 `NotFound`，錯誤碼為 `entity.not-found`。Repository 的 `GetAsync` 在查無資料時會拋出此例外。

`ConcurrencyConflictException` 表示 EF Core 樂觀鎖衝突。`IUnitOfWork.SaveChangesAsync` 捕捉 `DbUpdateConcurrencyException` 後會轉成此型別，讓應用層不必直接相依 EF Core 例外。

## 應用層對映

下列範例只示範分類邊界，實際回應型別由宿主決定。

```csharp
int statusCode = exception.ErrorType switch {
    AppErrorType.Validation => StatusCodes.Status400BadRequest,
    AppErrorType.NotFound => StatusCodes.Status404NotFound,
    AppErrorType.Conflict => StatusCodes.Status409Conflict,
    AppErrorType.Forbidden => StatusCodes.Status403Forbidden,
    _ => StatusCodes.Status422UnprocessableEntity
};
```

記錄日誌時保留 `ErrorCode`、例外型別與追蹤識別碼。回傳給使用者的文字可依 UI culture 重新解析，避免把內部例外詳細資料直接輸出至 API。

## DomainKit 內建資源

`BusinessException`、`EntityNotFoundException` 與 `ConcurrencyConflictException` 的預設訊息由 `DomainKitResources` 取得，並依目前 UI culture 選擇資源。自行提供訊息的建構式會直接使用呼叫端文字。

## enum 與 Enumeration 顯示名稱

`EnumerationLocalizer<TEnum>` 使用傳入的 `IStringLocalizer` 尋找顯示名稱。`TEnum` 可以是原生 `enum`，也可以是繼承 `EnumerationBase<TEnum, TValue>` 的 Enumeration 型別。

成員名稱的取得方式依型別分流，原生 `enum` 取 `ToString()`，Enumeration 型別以反射取 `Name` 屬性。兩者共用同一種資源鍵格式 `{型別名稱}.{成員名稱}`。

```text
ShipmentMethod.Express
OrderStatus.Draft
OrderStatus.Confirmed
```

```csharp
// 原生 enum
EnumerationLocalizer<ShipmentMethod> methodLocalizer = new(localizer);
string method = methodLocalizer.GetDisplayName(ShipmentMethod.Express);

// Enumeration 型別，繼承 EnumerationBase<OrderStatus, int>
EnumerationLocalizer<OrderStatus> statusLocalizer = new(localizer);
string status = statusLocalizer.GetDisplayName(OrderStatus.Confirmed);
```

型別標註 `[Flags]` 時，複合值會逐成員在地化。成員名稱含 `", "` 的值先切分成各個成員，分別查找資源後再以 `", "` 組回，因此資源檔只需提供個別旗標的項目。

找不到資源時回傳原始成員名稱。持久化 Enumeration 值的設定方式見[持久化與模型設定](persistence-and-modeling.md)，該設定僅適用於 `EnumerationBase<TEnum, TValue>` 衍生型別，原生 `enum` 的欄位對映由 EF Core 自行處理。

## 延伸閱讀

- [持久化與模型設定](persistence-and-modeling.md)
- [查詢與規約](querying-and-specifications.md)

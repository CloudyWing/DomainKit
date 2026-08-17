---
status: accepted
---

# ADR-0027：語言版本採 SDK 穩定預設，不使用 preview

## Context

`LangVersion=preview` 會使消費端必須具備同一 SDK band 才能自原始碼重建，套件本身也受未定案語法的行為變更影響。首次公開發佈是把語言版本固定為對外承諾的時點，此後再收斂即為破壞性變更。

## Decision

自 `Directory.Build.props` 移除 `<LangVersion>preview</LangVersion>`，語言版本隨 `TargetFramework` 取用 SDK 的穩定預設。

## Consequences

脫離預覽特性的代價為 EF 套件中 2 處集合運算式引數，改寫為建構式呼叫後語意不變，因為 `IEqualityComparer<T>` 對 `T` 為反變；核心套件原本就不依賴任何預覽特性。排除以 `<LangVersion>14.0</LangVersion>` 明確釘死，該寫法在升級 `TargetFramework` 時會成為隱性天花板。貢獻者在本機以較新 SDK 撰寫預覽語法時無編輯期護欄，須由 CI 的 `-warnaserror` 建置攔截。

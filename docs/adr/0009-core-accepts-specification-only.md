---
status: accepted
---

# ADR-0009：QueryService 核心只收規約，離散參數走擴充方法

## Context

`QueryService` 需同時支援規約輸入與離散參數（predicate、orderBy、take、pageNumber、pageSize）兩種呼叫形式，若兩者都堆在核心型別上會模糊單一輸入語意。

## Decision

核心實例方法只收規約，離散參數的便利多載以無狀態擴充方法掛在 `IQueryService` 公開表面，擴充內部組成規約後委派核心。

## Consequences

核心型別表面最小、最易測試與覆寫，兩條路徑因共用核心而不會行為分歧。排除在核心同時提供兩類多載的作法，該作法會使兩路徑各自實作而易於分歧。

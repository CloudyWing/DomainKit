---
status: accepted
---

# ADR-0008：規約採兩層介面組合，不採三層

## Context

規約子系統需決定介面分層。候選為兩層（filter 加 paged）與三層（filter、ordered、paged），三層另以獨立一層承載「排序但不分頁」。

## Decision

採 `ISpecification<T>` 純 filter 與 `IPagedSpecification<T> : ISpecification<T>` 兩層，不引入中間的 `IOrderedSpecification`。paged 的 `Criteria` 由 filter 的 `ToExpression()` 供給，使兩層確實接合。

## Consequences

「排序但不分頁」由 `QueryService` 的 `orderBy` 參數承載，不另養一層，避開三層繼承鏈與型別轉換成本。日後若出現「排序加不分頁加跨呼叫點復用排序」的需求，可以加法補第三層而不影響既有兩層。

---
status: accepted
date: 2026-08-01
---

# ADR-0020：帶 TKey 的查詢服務沿用 QueryService 名稱

## Context

帶 `TKey` 的查詢服務需要命名，候選為 `QueryService<TDbContext, TEntity, TKey>` 與 `KeyedQueryService<TDbContext, TEntity, TKey>`。

## Decision

沿用 `QueryService` 名稱，以泛型參數數量區分層級，與同樣無 `Keyed` 前綴的 `Repository<TDbContext, TEntity, TKey>` 對稱，並與兩參數版同置於 `QueryService.cs`。

## Consequences

消費端由泛型參數數量即可辨識層級，命名雜訊最小，且同名泛型同檔的規範得以維持。代價是同名不同元的型別需靠參數數量辨識。

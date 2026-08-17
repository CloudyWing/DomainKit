---
status: accepted
---

# ADR-0013：Repository 不收規約

## Context

Repository 的職責是依鍵撈出實體或載入聚合圖供異動，查詢輸出（filter 加投影加排序分頁）則屬 `QueryService`；規約服務於查詢輸出。

## Decision

`Repository<TDbContext, TEntity, TKey>` 不新增收規約的多載，規約只接到 `QueryService`。

## Consequences

兩者職責邊界維持清晰，Repository 的 by-key 語意不被稀釋。批次條件異動（依條件刪除或更新）不在本子系統範圍，有需求時另案評估。

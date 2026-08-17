---
status: accepted
---

# ADR-0004：以 SaveChangesInterceptor 集中處理審計與事件派發

## Context

審計填值與事件派發若寫在各 CUD 操作方法內，會分散於多處而易遺漏，且同一工作單元的多次變更會多次往返資料庫，也缺少集中收集 Domain Event 的時機點。

## Decision

以 `SaveChangesInterceptor` 在提交時集中填審計值並派發雙軌事件，實體是否參與審計由 typed 標記介面判定，取代字串反射。

## Consequences

所有 CUD 路徑（Repository 與直接操作聚合）自動套用審計，無遺漏風險，多次記憶體內變更只在一次提交時統一處理。interceptor 內以 private 方法分段（審計填值、domain event 迴圈派發、integration event 快照與提交後派發），使各段可獨立單元測試。

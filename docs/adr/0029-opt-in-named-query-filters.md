---
status: accepted
date: 2026-11-05
---

# ADR-0029：具名查詢篩選採 opt-in 啟用

## Context

軟刪除與租戶條件原本合併為同一個匿名全域查詢篩選，`IgnoreQueryFilters()` 只能一次略過全部條件，管理流程查詢已刪除資料時會連租戶隔離一起失效。EF Core 10 提供具名查詢篩選，可用 `IgnoreQueryFilters(filterKeys)` 個別略過。EF Core 不允許同一實體同時具有匿名與具名篩選，既有程式在 `ConfigureFullAuditedEntity` 之後再呼叫 `HasQueryFilter(expression)` 時，改成具名篩選會在建立模型時拋出例外。

## Decision

新增 `UseDomainKitNamedQueryFilters`，在 `OnModelCreating` 開頭呼叫後，`ConfigureFullAuditedEntity` 與 `ApplyTenantConventions` 改以 `DomainKit.SoftDelete` 與 `DomainKit.Tenant` 註冊具名篩選。未呼叫時維持匿名篩選。`QueryService.IgnoreSoftDeleteFilterOnly` 只略過 `DomainKit.SoftDelete`，遇到匿名篩選時拋出 `InvalidOperationException`，不默默退回略過全部篩選。

## Consequences

既有模型的篩選型態與 `IgnoreQueryFilters()` 結果不變。需要只略過軟刪除的宿主須明確 opt-in，並改以具名形式加入自訂篩選。實體只有具名篩選時，`ApplyTenantConventions` 一律以具名形式加入租戶篩選，避免與既有具名篩選衝突。

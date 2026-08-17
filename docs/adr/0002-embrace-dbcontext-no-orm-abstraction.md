---
status: accepted
---

# ADR-0002：擁抱 DbContext，不做 ORM 可攜抽象

## Context

`Repository`、`QueryService` 與 `UnitOfWork` 需決定是否定義 ORM-agnostic 介面以隔離 EF 型別，為「將來換 Dapper」保留空間。

## Decision

三者直接以 `TDbContext : DbContext` 為型別參數。介面只為領域意圖表達與可測試性而存在，不為 ORM 可攜性；跨資料庫由各 provider 自實作 Fluent Schema 與 Value Converter 處理。

## Consequences

保留 `IQueryable` 投影與全域查詢篩選等 EF 能力，避免查詢介面退化為最小公分母。代價是更換 ORM 明確不在支援範圍，消費端會傳遞參照 `Microsoft.EntityFrameworkCore`。

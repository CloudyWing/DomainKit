---
status: accepted
date: 2026-08-01
---

# ADR-0018：AuditedQueryService 以 EF.Property 實作 LEFT JOIN

## Context

審計查詢需帶出建立者與異動者的使用者資訊，但實體與 User 表刻意不設外鍵，且 User 可能已被刪除，查詢仍須回傳實體。

## Decision

以 LINQ `join ... into ... DefaultIfEmpty()` 搭配 `EF.Property<T>()` 實作 LEFT JOIN，不依賴導覽屬性。

## Consequences

`EF.Property<T>()` 為 EF Core 官方支援且可翻譯為 SQL，查詢可跨模組 DbContext 邊界，User 不存在時 Creator 為 null 而實體仍回傳。排除導覽屬性 Include（與不設外鍵的約束衝突）與兩次查詢後 client-side join（多一次往返且無法分頁）。

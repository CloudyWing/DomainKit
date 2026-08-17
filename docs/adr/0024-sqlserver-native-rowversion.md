---
status: accepted
---

# ADR-0024：SQL Server 覆寫為原生 rowversion，核心維持 app-managed token

## Context

app-managed 並行 token 僅在寫入走 EF change tracking 時有效，raw SQL、`ExecuteUpdateAsync` 與外部系統直寫都會繞過；但 `IsRowVersion()` 在無原生支援的 provider（如 SQLite）語意不一致，核心層直接採用會違反 provider 中立原則。

## Decision

核心層維持 provider 中立的 app-managed token，SQL Server 的 per-provider 層一律覆寫為原生 `rowversion`，interceptor 依 metadata 跳過該實體的 app stamp。

## Consequences

SQL Server 上的樂觀鎖對繞過 EF 的寫入同樣生效，樂觀鎖無死角；SQLite 等 provider 仍以 app token 運作，測試路徑不中斷。代價是同一機制在不同 provider 有兩種實作形態，文件須說明差異。

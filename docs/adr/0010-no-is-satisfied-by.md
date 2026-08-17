---
status: accepted
---

# ADR-0010：規約不提供 IsSatisfiedBy 的 in-memory 驗證

## Context

規約若提供 `IsSatisfiedBy`，同一條件即同時存在 in-memory 編譯委派與 SQL provider 轉譯兩條求值路徑，兩者在字串 collation、null 排序與大小寫上的行為可能不一致。規約本身已帶排序與分頁，其性質為查詢規格而非領域不變式。

## Decision

規約不提供 `IsSatisfiedBy`，`ISpecification<T>` 只提供 `ToExpression()` 供 EF 轉譯。

## Consequences

求值路徑唯一，不存在兩路徑結果分歧誤導使用者的情形，也不會有每次呼叫都 `Compile()` 的效能陷阱。日後若出現領域不變式的 in-memory 驗證需求，須以加法新增、快取編譯委派，並在文件明標兩路徑的語意差異。

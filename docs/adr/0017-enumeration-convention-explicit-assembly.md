---
status: accepted
date: 2026-08-01
---

# ADR-0017：Enumeration convention 由宿主明確傳入 assembly

## Context

Enumeration 型別的 ValueConverter 需批次套用，掃描範圍決定啟動成本與可預期性；全域掃描所有已載入 assembly 會使啟動時間不可控，也可能掃到非預期的 Enumeration 子類別。

## Decision

由宿主在 `ConfigureConventions` 明確傳入 assembly，只掃描該 assembly 內的 Enumeration 型別。

## Consequences

宿主明確控制掃描範圍，啟動成本可預期。代價是新增含 Enumeration 型別的組件時，宿主須同步更新傳入的 assembly 清單；排除逐屬性手動設定 converter 的高重複作法。

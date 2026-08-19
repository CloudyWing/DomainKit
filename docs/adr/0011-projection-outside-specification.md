---
status: accepted
date: 2026-08-01
---

# ADR-0011：投影不入規約，走顯式 selector 參數

## Context

規約若封裝投影，會與 `QueryService` 既有且運作良好的 `selector` 機制職責重疊，造成同時存在兩套投影。

## Decision

投影不進規約，呼叫端以顯式 `Expression<Func<T, TRecord>>` selector 參數自行挑欄位。

## Consequences

selector 與規約（filter、排序、分頁）保持正交，職責邊界清晰；規約無法單獨表達輸出形狀，投影一律由呼叫端指定。

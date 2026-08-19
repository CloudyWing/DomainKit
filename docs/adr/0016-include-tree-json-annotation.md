---
status: accepted
date: 2026-08-01
---

# ADR-0016：Aggregate Include 樹以 JSON 序列化存入 entity annotation

## Context

Aggregate Include 樹在 EF model building 階段建構一次、查詢時多次讀取，存放位置為 entity annotation，其值型別為 `object?`。

## Decision

以 JSON 序列化字串存入 annotation，不直接存 `IncludeTreeNode` 物件圖。

## Consequences

annotation 值為純資料、無循環參考且可 debug 檢視，不依賴 EF Core 內部的序列化實作。序列化開銷因只在 model building 發生一次而可忽略。

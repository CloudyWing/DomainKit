---
status: accepted
---

# ADR-0012：Include 只屬 Repository，不入規約與 QueryService

## Context

`Include` 的用途是把聚合圖載入記憶體供領域行為操作，而 `QueryService` 為純查詢轉投影輸出，經導覽屬性的 `Where` 與 `Select` 由 EF Core 自動 join 即可，不需 `Include`。

## Decision

`Include` 不進規約、不進 `SpecificationEvaluator`、不進 `QueryService` 規約多載，只屬 `Repository` 的 Aggregate Include 擴充點。

## Consequences

避開強型別 `ThenInclude` 深層鏈的設計成本，該成本高且 ROI 低。需要預先載入聚合圖時一律走 Repository，`QueryService` 無法表達 eager loading。

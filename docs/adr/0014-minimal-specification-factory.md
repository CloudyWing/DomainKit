---
status: accepted
---

# ADR-0014：規約建構只保留最小 factory，不做大 bundle builder

## Context

規約的建構入口有兩種尺度：承載 Criteria、OrderBy、Skip、Take 與 Includes 的大 bundle builder，或只涵蓋實際需要的最小 factory。本套件的規約無 Includes、投影外置、List 與 Paged 分離，所需的建構面遠小於前者。

## Decision

只保留建構不可變 `IPagedSpecification` 所需的最小 factory（`PagedSpecification.Create(spec, orderBy, pageNumber, pageSize)`），`ISpecification` 由 `Specification<T>` 建構子直接建立。

## Consequences

規約於建構時鎖定 criteria、無 public setter，具值語意且多執行緒安全，`And`、`Or`、`Not` 一律回傳新實例。建構入口最小化的代價是複雜組裝需由呼叫端自行組合規約。

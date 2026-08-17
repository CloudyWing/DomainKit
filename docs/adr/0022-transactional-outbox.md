---
status: accepted
---

# ADR-0022：整合事件採 transactional outbox

## Context

稽核軌跡與跨模組連動屬不可遺失需求，而 in-process 派發在「提交完成與派發之間 crash」時事件永久遺失且無從補償，handler 例外自 `SaveChangesAsync` 冒出亦造成「主資料已提交但呼叫端看到失敗」的認知不一致。

## Decision

整合事件採 transactional outbox，事件與業務變更同 `SaveChanges` 原子落庫，由背景服務派發，附重試 backoff 與超限 dead-letter，handler 以事件 id 判重達成冪等。

## Consequences

投遞保證由資料庫原子性承載，不引入外部 message broker 相依，與單一資料庫部署模型一致。排除 best-effort in-process（故障時段必然產生稽核缺漏）與直接引入 broker（發佈本身仍不原子，需 outbox 銜接）；日後若需 broker，可抽換 dispatcher 的派發目標而不動落列機制。

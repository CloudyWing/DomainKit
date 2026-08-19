---
status: rejected
date: 2026-08-01
---

# ADR-0006：將 IUnitOfWork 的 EF 交易多載拆至 IEfUnitOfWork

## Context

為讓下游 Application 組件達成零 EF 傳遞參照，曾規劃將暴露 `IDbContextTransaction` 的泛型 `UseTransactionAsync<TResult>` 自核心 `IUnitOfWork` 拆至 EF 專屬衍生介面 `IEfUnitOfWork`，使核心介面零 EF 型別。

## Decision

否決。該拆分的驅動理由已隨下游模組分層改採「抽象歸位、UI 與 App 同捆」而消失，已實作的拆分程式碼亦已還原；`IEfUnitOfWork` 不存在，泛型多載維持於 `IUnitOfWork`。

## Consequences

交易與 Unit of Work 相關的文件與程式一律以 `src/DomainKit.EntityFrameworkCore/Persistence/IUnitOfWork.cs` 的單一介面形態為準。此 ADR 以 rejected 保留，是為記錄該方案已被評估並否決，避免日後依舊設計稿重提。

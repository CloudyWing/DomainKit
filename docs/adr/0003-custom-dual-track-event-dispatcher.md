---
status: accepted
---

# ADR-0003：自製輕量雙軌事件 Dispatcher，不採 MediatR

## Context

事件派發需求為單向通知，無回傳值也無 pipeline behavior，而 MediatR 的 pipeline、notification 與 request/response 模型對此過重且屬專案硬約束排除項。

## Decision

自製 `IDomainEventDispatcher`（before-commit 軌）與 `IIntegrationEventDispatcher`（after-commit 軌）兩個 dispatcher，以 `IDomainEvent` 與 `IIntegrationEvent` 兩個事件介面區分軌別，dispatcher 以反射建立封閉泛型 handler 型別並自 `IServiceProvider` 解析。

## Consequences

兩軌契約在型別層級分離，消費端註冊與替身測試各自獨立，interceptor 對兩軌的依賴明確可辨。套件只做註冊與觸發，outbox、重試與實際投遞等傳遞政策不落在 dispatcher 內。

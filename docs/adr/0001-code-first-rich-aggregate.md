---
status: accepted
---

# ADR-0001：實體即 EF 對映型別的真 Code First 富血聚合

## Context

領域實體需同時是 EF 對映型別與封裝不變式的聚合根。若讓兩者分屬不同型別，持久化層必須在兩者之間搬運狀態，審計欄位只能以字串約定辨識，聚合上也沒有自然掛載 Domain Event 的位置。

## Decision

實體即 EF 直接對映型別，同時繼承 `AggregateRoot<TKey>`，於行為方法內封裝不變式並呼叫 `AddDomainEvent`。

## Consequences

持久化層以 typed 標記介面判定審計欄位，型別安全且免反射；聚合直接是 EF 追蹤對象，記憶體內變更即被 ChangeTracker 捕捉，符合單次提交模型。代價是領域型別與 EF 對映無法分離，更換 ORM 需重寫實體。

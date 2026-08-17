---
_layout: landing
---

# DomainKit

DDD 風格的 .NET 領域層基礎建設，包含 Entity、Aggregate Root、雙軌領域事件、審計、多租戶、樂觀鎖，以及 EF Core 的持久化、查詢、規約與 Outbox。設計前提為真 Code First。實體即 EF 對映型別並同時是富血聚合根。

## 文檔導航

### 使用指南

- [入門指南](articles/getting-started.md)
- [聚合與審計](articles/aggregates-and-auditing.md)
- [持久化與模型設定](articles/persistence-and-modeling.md)
- [查詢與規約](articles/querying-and-specifications.md)
- [多租戶與並行控制](articles/tenancy-and-concurrency.md)
- [事件、Outbox 與廣播](articles/events-outbox-and-broadcast.md)
- [錯誤契約與本地化](articles/errors-and-localization.md)

### 決策紀錄

架構決策紀錄（ADR）記載各項設計的脈絡、選定方案與後果，包含已被否決與已被覆蓋的決策。

- [決策紀錄索引](adr/)

## License

本專案授權見 [LICENSE.md](https://github.com/CloudyWing/DomainKit/blob/main/LICENSE.md)。

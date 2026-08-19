---
_layout: landing
---

# DomainKit

此專案是我把我過往 Domain 架構往 DDD 演進後的結果，並讓 AI 討論並補足一些不夠熟悉的部分，作為後續研究，配合自身需求維護，可能一加功能就是破壞性變更的大改版，不特別建議其他人直接相依。

DDD 風格的 .NET 領域層基礎建設，包含 Entity、Aggregate Root、雙軌領域事件、審計、多租戶、樂觀鎖，以及 EF Core 的持久化、查詢、規約與 Outbox。設計前提為真 Code First。實體即 EF 對映型別並同時是充血聚合根。

## 文件導覽

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

## 版本與相容性

目標框架為 .NET 10。版號由 MinVer 依 git tag（前綴 `v`）推導，發版即打 tag。

`0.x` 期間不保證公開 API 穩定，次版號遞增即可能包含破壞性變更，變更內容記於 [CHANGELOG.md](https://github.com/CloudyWing/DomainKit/blob/main/CHANGELOG.md)。相依此套件時建議釘死明確版號，避免使用範圍或浮動版號。

## License

本專案授權見 [LICENSE.md](https://github.com/CloudyWing/DomainKit/blob/main/LICENSE.md)。

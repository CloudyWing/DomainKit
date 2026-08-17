---
status: accepted
---

# ADR-0025：公開面全數維持 public，不收斂為 internal

## Context

首次公開發佈前是最後一次可零成本收斂公開面的時點；`PublicAPI.Shipped.txt` 的核心 142 筆與 EF 241 筆為一次性灌入，未經逐筆審視即宣告為已發佈契約。

## Decision

逐筆審視後全數維持 public，無型別改為 `internal`。判定依據為四項證據任一成立即維持公開：出現在其他公開成員的簽章中、消費端直接引用、須由宿主在 `AddDomainKit` 之外自行接線、擴充方法被消費。

## Consequences

公開面自此成為相容性承諾，`EnablePackageValidation` 亦以其為基準。`DomainEventDispatcher` 與 `IntegrationEventDispatcher` 等零引用的實作型別維持公開，是為保留裝飾（logging、tracing 等橫切關注）與自訂 DbContext 接線兩條路徑；排除以 `InternalsVisibleTo` 對測試開放而對外收斂的作法。

---
status: accepted
---

# ADR-0005：對外辨識鍵以標記介面加可覆寫述詞表達

## Context

Repository 對外操作的「辨識鍵」不一定等於真實 PK，需涵蓋複合鍵、代理鍵加業務唯一鍵，以及「某欄位在無軟刪除時應唯一、但軟刪除造成唯一鍵衝突而另設代理 PK，對外仍以該業務欄位為 ID」的情境。

## Decision

核心提供標記介面 `IHasExternalKey<TKey>`，Repository 預設以 EF Model 主鍵建立鍵述詞，實體實作該介面時改以 `ExternalKey` 對應欄位建立；另開放 `protected virtual Expression<Func<TEntity, bool>> CreateKeyPredicate(TKey key)` 供複合鍵與自訂對應覆寫。

## Consequences

對外辨識鍵的語意在領域型別上自我描述且不引入 EF 相依，覆寫點涵蓋進階情境而不強迫所有實體實作介面。排除純委派 keySelector（型別自描述喪失、設定點散落於組裝程式碼）與純主鍵反射推導（只能處理單一主鍵，無法表達鍵解耦）。

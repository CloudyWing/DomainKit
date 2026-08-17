---
status: accepted
---

# ADR-0021：時間 ValueConverter 採 opt-in seam 加兩個內建實作

## Context

`DomainKit.EntityFrameworkCore` 為 provider 無關套件，但各 provider 對時間型別的支援不一致（SQLite 無原生 `DateTimeOffset`），且部分宿主只需 provider 原樣往返。

## Decision

採 opt-in seam `ApplyDomainKitConventions(ValueConverter? dateTimeOffsetConverter = null)`，另提供 `UtcDateTimeOffsetConverter` 與 `UtcDateTimeConverter` 兩個語意命名的內建實作。

## Consequences

宿主依 provider 決定是否轉換與如何轉換，「原樣往返即可」的情境不被強加多餘轉換，內建實作涵蓋 UTC 落庫與 SQLite 型別降轉兩種常見需求。排除依 provider 自動偵測（需引入 provider 相依或脆弱的字串判斷）。

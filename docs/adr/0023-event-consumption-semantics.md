---
status: accepted
date: 2026-08-01
---

# ADR-0023：事件消費語意分為工作型與失效型，各走不同通道

## Context

工作型事件需要 exactly-one-consumer（競爭消費、一實例處理、不可遺失），失效型通知需要 every-instance（如快取失效）。兩者投遞語意相反，以單一機制承載必然犧牲其一。

## Decision

兩類語意分開建模：工作型走 outbox，失效型通知走廣播通道 seam，單實例預設為 in-process 實作，多實例可抽換 backplane。

## Consequences

消費端接線即宣告語意，兩類可各自獨立驗證。排除單一管線加消費端自行判斷的作法，該作法的接錯無編譯期防護，且快取失效誤走 outbox 的錯誤在單實例環境不可見，要到 scale-out 才顯現。

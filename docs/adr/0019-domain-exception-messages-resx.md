---
status: accepted
---

# ADR-0019：領域例外訊息以 .resx 強型別資源承載

## Context

領域例外訊息需支援多語系，而例外類別為 POCO，不適合注入 `IStringLocalizer` 等 DI 服務。

## Decision

以 `.resx` 強型別 resource class 作為例外訊息來源。

## Consequences

屬 ASP.NET Core 標準做法且不引入額外框架，消費端可透過附屬組件提供翻譯。排除硬編碼多語系字串的不可維護作法。

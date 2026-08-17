---
status: accepted
---

# ADR-0007：IQueryService 加法補上 First 系列

## Context

`IQueryService<TEntity>` 原僅有 `GetSingleAsync` 與 `GetSingleOrDefaultAsync`，底層為 `.SingleAsync` 與 `.SingleOrDefaultAsync`，語意是「至多一筆，否則拋」，無法表達「依排序取第一筆」。

## Decision

於 `IQueryService<TEntity>` 與 `QueryService<TDbContext, TEntity>` 基底加法新增 `GetFirstAsync<TOutput>` 與 `GetFirstOrDefaultAsync<TOutput>`，簽章帶 `orderBy` 以確保結果確定，形狀與既有 `GetListAsync`、`GetPagedListAsync` 一致。

## Consequences

既有 Single 系列行為不變，多筆仍拋，呼叫端依語意選用。`orderBy` 為必要參數，未指定排序的「取任一筆」不在支援範圍。

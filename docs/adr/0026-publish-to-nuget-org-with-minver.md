---
status: accepted
date: 2026-08-01
---

# ADR-0026：發佈至 nuget.org，版號由 MinVer 依 git tag 推導

## Context

手動遞增版號需靠紀律維持正確性，SemVer 的 patch 位不進位這項規則曾被誤解為「第三碼滿 9 即進位」。資料夾 feed 另有版本不可變性與稽核軌跡的缺口，套件若自未提交的工作區打包，產物與版控之間即無法互相追溯。

## Decision

對外一律發佈至 nuget.org，版號由 MinVer 依 git tag（前綴 `v`）推導，發版即打 tag，首個公開版號為 `v0.1.0`。

## Consequences

人工進位的動作消失，版號正確性不再倚賴紀律，已發佈產物皆可自 tag 重建。排除 GitHub Packages 作為對外通道、排除資料夾 feed 作為長期發佈管道、排除自 `v0.2.0` 起跳（另立號段會使首個公開版號與專案成熟度脫節）。

---
status: accepted
---

# ADR-0028：發佈憑證採 Trusted Publishing，不建立 API key

## Context

對外發佈至 nuget.org 需要認證憑證。ADR-0026 決定了發佈目的地與版號來源，未涉及以何種憑證認證，兩者正交。長期 API key 是需要保管、輪替且外洩後可被離線使用的持久憑證，nuget.org 亦已在其管理頁面標示不建議用於自動化發佈。

## Decision

以 nuget.org Trusted Publishing policy 搭配 GitHub Actions OIDC 取得臨時發佈憑證，不建立長期 API key，發佈流程不使用任何 GitHub repository secret。

## Consequences

臨時憑證有效期 1 小時且每枚 OIDC token 只能兌換一次，憑證的持有窗口因此壓縮至單次 job 執行之內。policy 的 Package Owner 欄套用於該擁有者名下的全部套件，實際限縮由 Repository 與 Workflow File 兩欄達成，故每多一個發佈用的 workflow 檔名就多一條放行整個命名空間的常設授權。共用範本 `reusable-nuget-publish.yml` 需以 secret 有無分流，使既有以 API key 呼叫的消費端不做修改即維持可用。

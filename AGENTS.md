# 專案 Agent 規範

本檔定義 DomainKit 專案層的 Agent 共通規則，供所有 AI 工具讀取。個人機器偏好寫入 `AGENTS.local.md`，Session 狀態寫入 `CONTEXT.local.md`。

## 共通規範

- 專案目標與範圍：提供可重用的 .NET 領域層基礎建設，涵蓋 DDD 風格的 Entity 與 Aggregate Root、雙軌領域事件、審計填值、多租戶、樂觀鎖，以及 EF Core 的持久化、查詢、規約與 Outbox。設計前提為真 Code First，實體即 EF 對映型別並同時是富血聚合根。
- 主要工作目錄與交付位置：`src/DomainKit`（核心套件）、`src/DomainKit.EntityFrameworkCore`（EF 套件）、`tests/DomainKit.Tests`（測試）、`docs/`（docfx 文件與 ADR）。
- 不可違反的安全或合規限制：發佈憑證一律由 Trusted Publishing 於 CI 執行期間動態取得，不建立長期 API key，不在 repo 或 GitHub secret 中存放發佈憑證。

## 開發規範

- 技術棧與版本：.NET 10（`net10.0`）、EF Core 10、NUnit、MinVer（tag 前綴 `v`）、Microsoft.CodeAnalysis.PublicApiAnalyzers、docfx。
- 建置、測試與驗證命令：`dotnet build DomainKit.slnx --configuration Release -warnaserror`、`dotnet test DomainKit.slnx --configuration Release --no-build`。兩者皆須 exit code 0。
- 語言版本：使用 SDK 隨 `TargetFramework` 決定的穩定預設，**不得加回 `LangVersion=preview`，也不得使用預覽語言特性**。此約束無編輯期護欄，由 CI 的 `-warnaserror` 建置攔截，決策見 `docs/adr/0027-stable-langversion.md`。
- 套件邊界：`CloudyWing.DomainKit` 為核心套件，**零 EF Core 相依，僅相依 BCL**；`CloudyWing.DomainKit.EntityFrameworkCore` 相依核心套件與 EF Core。相依方向由 `tests/DomainKit.Tests/Architecture/DependencyDirectionTests.cs` 守護，新增核心型別時不得引入 EF 型別。
- 公開 API 演進：公開面以 `src/*/PublicAPI.Shipped.txt` 宣告，`RS0016` 與 `RS0017` 已設為建置錯誤。新增公開成員時寫入 `PublicAPI.Unshipped.txt`，發版時再併入 `Shipped`。`0.x` 期間允許破壞性變更，但移除或變更既有公開成員必須同步更新該檔並記入 `CHANGELOG.md`。
- 決策紀錄：架構決策採追加式 ADR，序號不重排。推翻既有決策時建立新序號的 ADR，舊 ADR 只改 `status`，正文不改寫。

## 寫作規範

- 文件語言與讀者：台灣用語正體中文，讀者為套件消費端開發者。
- 文件平台與 Markdown 慣例：`docs/` 由 docfx 建置，`docs/articles/` 內的交叉連結使用相對路徑。`README.md` 的連結一律使用相對路徑，該檔同時是 `PackageReadmeFile`，套件頁面上的相對連結會失效，此為已知並接受的取捨。
- 增刪文章時，`README.md`、`docs/index.md` 與 `docs/articles/toc.yml` 三處導覽清單都要同步。新增 ADR 時同步 `docs/adr/toc.yml`。

<!-- AI-DECLARATIONS:BEGIN -->
## AI Context Declarations

- `context-index-query`：`graphify query "<question>"`
- `glossary`：未建立
- `adr`：`docs/adr/`
- `context-index`：`.local/ai-context/`
<!-- AI-DECLARATIONS:END -->

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:

- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

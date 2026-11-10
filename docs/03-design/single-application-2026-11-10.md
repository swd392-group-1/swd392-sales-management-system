# Single runnable MVC application — 10 November 2026

The user asked to merge the separately runnable team/inventory applications and supporting projects into one runnable project. Their Visual Studio screenshot selected the Domain class library as startup. The resulting solution has one application, `BookSales.Web`, and one test project, `BookSales.Tests`.

## Consolidation

| Former location | Current location |
|---|---|
| `src/BookSales.Domain` | `src/BookSales.Web/Domain` |
| `src/BookSales.Application` | `src/BookSales.Web/Application` |
| `src/BookSales.Infrastructure` | `src/BookSales.Web/Infrastructure` |
| Inventory controllers/data/models/services | `src/BookSales.Web/Inventory` |
| Inventory views | `src/BookSales.Web/Areas/Inventory/Views` |
| Inventory static assets | `src/BookSales.Web/wwwroot/inventory-assets` |
| Inventory tests | `tests/BookSales.Tests/Inventory` |

Removed the obsolete class-library/inventory `.csproj` files and standalone inventory startup/configuration/launch files. Existing namespaces were retained to preserve references and distinguish the modules' similarly named types. `BookSales.Web` now directly references EF Core SQL Server 8.0.32 and QRCoder. `BookSales.sln` lists only the web and test projects, with the web project first.

Inventory uses an MVC area at `/inventory/{controller=Home}/{action=Index}/{id?}`. Both original Home controllers are retained and separated by area. Inventory styles/libraries keep their own static path; Razor tag helpers generate inventory-area URLs. Main navigation and home cards link to the consolidated screens. Inventory controllers retain antiforgery protection through controller filters, replacing the old host's global filter.

Both EF contexts now live in the same assembly and use the same connection setting. Their mapping scans are explicitly restricted to their original configuration namespaces, preventing unrelated Book/Account/etc. models from being accidentally applied to both contexts. This does not merge entity models, alter tables or run migrations.

The inventory demo account middleware runs only for `/inventory` requests, after the existing Development/loopback guard. It does not replace the main cookie Login or authenticate users for main-site permissions. Shared authorization integration remains unfinished. Business/demo restrictions and existing services/rules are preserved. No passwords were provisioned.

## Run/debug

Open `BookSales.sln`, reload if already open, set `BookSales.Web` as the startup project, select Debug and the http profile, then F5. Browser launch is enabled and the address is `http://127.0.0.1:5180`. Inventory runs on this same host at `/inventory`. Tests run through Test Explorer.

The sole active local connection configuration remains `src/BookSales.Web/appsettings.Local.json`, read only in Development. Environment/command-line settings retain precedence. The old inventory local file was preserved under ignored `.artifacts/single-app-20261110/old-local-settings/`. Production reads committed settings and external configuration. No SQL schema/data reset is part of this refactor.

## Verification and limits

- Fresh Debug build: zero warnings/errors, debug PDB generated.
- Final Debug and Release tests: **96/96 passed each**, zero skipped, with no test-process environment override needed. The no-database production test has explicit test configuration; Development-only local JSON is not loaded in Production.
- Added regression checks for isolated EF mapping discovery within one assembly, main/area Razor routing and form URLs, and inventory POST antiforgery enforcement. Existing 93 tests were consolidated and retained; production inventory route tests use the new area URLs.
- Actual Debug host: **16/16 HTTP GET checks passed**, including home, `/health/db`, Login, `/sales`, returns, pricing, report, inventory dashboard/Count/create/adjustments/Audit/purchasing/demo selector and CSS assets. `/health/db` reported `databaseConnected: true`. Count forms used `/inventory/Counts/Create` and included antiforgery tokens.
- SQL-backed GETs used the existing local review database. Read-only stock check remained 12 rows/183 units. No business POST, database migration, seed/reset or credential provisioning was performed. The temporary host was stopped.
- Visual Studio F5/debugger attachment was not automated; the Debug binary/PDB, runnable launch profile and actual Debug HTTP process were verified. No production deployment, CI result or full transactional acceptance is claimed.

TRX and HTTP evidence is ignored under `.artifacts/single-app-20261110/`. The initial test request used `/Sales/Create`, which is not the attribute-routed sales URL; the corrected `/sales` GET passed. Final HTTP JSON contains the correct routes.

Automatic approval blocked recursive deletion of obsolete source/cache folders without a detailed reason. Obsolete tracked project/startup files were removed through explicit file edits. After checking that only generated outputs remained, those old folders were moved reversibly to ignored `.artifacts/single-app-20261110/obsolete-build-folders/`. Active `src` and `tests` each contain only the retained project folder. No source/data deletion workaround was used.

OpenAI Codex assisted with the authorized consolidation, routing/configuration fixes, tests and documentation. Changes are local and uncommitted/unpushed. Human acceptance of business requirements, diagrams and schema is unchanged. The pre-consolidation README is preserved as [dated documentation](historical-readme-before-single-app-2026-11-10.md); its standalone app/project instructions are historical.

## Visual Studio SDK follow-up

The IDE subsequently reported NETSDK1141: system dotnet/Visual Studio found SDK 10.0.403, while SDK 8.0.427 used by prior checks was only in the private cache. System .NET 8 runtimes were already installed. Changed `global.json` rollForward to `major`, retaining the SDK 8 minimum/preference and `net8.0` targets. Verified the default system dotnet now selects 10.0.403; Debug build completed with zero warnings/errors; actual Visual Studio Community 2026 MSBuild built both projects; all 96 tests passed using the system host. No runtime/framework migration. Evidence: `.artifacts/single-app-20261110/vs-sdk-fix_*.trx`.

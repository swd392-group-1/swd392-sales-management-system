# Book Sales Management System · SWD392 Group 1

One .NET 8 MVC application, **BookSales.Web**, runs the team's screens from one address. `BookSales.sln` contains the web project and one test project. The former Domain, Application and Infrastructure libraries are now folders inside the web project; inventory is an MVC area in the same application.

## Run and debug in Visual Studio

1. Open `BookSales.sln` and reload the solution if it was already open.
2. Right-click **BookSales.Web** → **Set as Startup Project**.
3. Select **Debug** and the **http** launch profile, then press **F5**.
4. The browser opens at `http://127.0.0.1:5180/`. Use **Tồn kho và mua hàng** for the inventory screens at `/inventory`.

`BookSales.Tests` is a test project: run it through Test Explorer. All application breakpoints, including the moved business/database/inventory code, belong to the single `BookSales.Web` process.

## Run from PowerShell

The application targets .NET 8. `global.json` prefers SDK 8 and allows a newer major SDK when SDK 8 is absent; SDK 10.0.403 and Visual Studio 2026 MSBuild were verified on this machine. The helper also finds the existing private SDK at `$env:USERPROFILE\.cache\swd392-dotnet8`:

```powershell
.\scripts\dev.ps1 restore
.\scripts\dev.ps1 build
.\scripts\dev.ps1 test
.\scripts\dev.ps1 run
```

Or with SDK 8 on PATH:

```powershell
dotnet run --project src/BookSales.Web --launch-profile http
```

## Local SQL Server

All modules use `ConnectionStrings:BookSales`. For Development, set the ignored `src/BookSales.Web/appsettings.Local.json`:

```json
{
  "ConnectionStrings": {
    "BookSales": "Server=localhost;Database=SWD392_BookSales_3NF_REVIEW_20261027;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

This is the current local review database. The connection uses Windows authentication. Environment/command-line configuration takes precedence; Production uses committed settings and environment/command-line configuration rather than the Development-only local file. `/health/db` checks connectivity. No automatic schema creation, migrations, reset, seed or password provisioning runs on startup.

## Code structure

| Folder inside `src/BookSales.Web` | Purpose |
|---|---|
| `Controllers`, `Views`, `Models` | Main MVC screens and HTTP models |
| `Domain` | Business entities and core interfaces |
| `Application` | Use-case services, contracts and request/result objects |
| `Infrastructure` | SQL Server/EF persistence, access implementations and background workers |
| `Inventory` | Inventory controllers, models, services and persistence |
| `Areas/Inventory/Views` | Inventory Razor views at `/inventory/...` |
| `wwwroot/inventory-assets` | Inventory styles/scripts/libraries, isolated from main styles |
| `Access`, `Middleware`, `Realtime`, `Services` | Web-specific authentication, middleware, SignalR and QR support |

Existing C# namespaces are preserved to avoid conflating the two modules' entity types. There are two EF context classes inside the one application, using one connection setting and isolated mapping discovery; this refactor does not change database tables.

## Available routes and limits

- Main home `/`, sales `/sales`, returns `/Returns/Search`, pricing `/Pricing`, inventory report `/reports/inventory`, Login `/access/login`.
- Inventory dashboard `/inventory`, Count `/inventory/Counts`, adjustments `/inventory/Adjustments`, Audit `/inventory/Audits`, purchasing `/inventory/Purchase`, demo account selection `/inventory/Session`.
- Business/demo routes remain restricted to local Development while shared access integration is unfinished. Inventory keeps its existing local demo selector; it does not authenticate the main site's account. NULL password hashes remain unprovisioned.
- Queue delivery remains in-memory/best-effort. No production deployment, complete permission integration or requirements/schema acceptance is claimed.

[Single-application integration record](docs/03-design/single-application-2026-11-10.md) · [Access integration](docs/03-design/hung-access-integration.md) · [Historical inventory handoff](docs/03-design/kiet-inventory-module.md) · [Third-party notices](THIRD_PARTY_NOTICES.md)

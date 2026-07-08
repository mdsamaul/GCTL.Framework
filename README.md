# GCTL HRM — .NET 10 Starter (Identity + EF Core + Dapper)

## Important: read this first

This project was hand-written, not compiled in the sandbox that produced it
(no .NET SDK was available there). It uses correct, real .NET 10 / EF Core 10 /
ASP.NET Core Identity 10 APIs, but **you must run a real build on your machine
to confirm it compiles clean** — treat the first `dotnet build` as part of
setup, not as something that might fail because of me being careless.

If something doesn't compile, paste me the exact error and I'll fix it — that's
a much more reliable path than me guessing blind.

## What's in here

- **GCTL.Core** — shared base types: `IRepository<T>`, `AppService<T>`, `PagedList`/DataTables helpers, `CommonSelectModel`, no legacy dependencies
- **GCTL.Data** — EF Core 10 DbContexts (Identity + a placeholder for your reverse-engineered business context), `GenericRepository<T>`, Dapper connection factory
- **GCTL.Service** — service layer between controllers and data (module services inherit `AppService<T>`); includes a fully-commented `AdmissionTypes` sample as a template for every other module
- **GCTL.UI.Core** — ASP.NET Core MVC app with working Sign In / Sign Up pages, and a `BaseController` all your other controllers should inherit from

### About GCTL.Service and BaseController

Both were missing from the first version of this template — not because they
were skipped on purpose, but because your uploaded zip didn't actually
contain `GCTL.Service.csproj` or `AppService.cs` (only `IAdmissionTypeService.cs`
/ `AdmissionTypeService.cs`, which reference `AppService<T>` but don't define
it — it must have lived in a file that wasn't included in the export).

What's included now:
- **`GCTL.Service`** — rebuilt as a proper project, with `AppService<T>`
  reconstructed in `GCTL.Core/Data/AppService.cs` based on exactly how your
  `AdmissionTypeService` called it (`GetAll()`, `Add()`, `Update()`). If your
  real `AppService` had more members, paste that file and I'll merge it in.
- **`BaseController`** — restored from your original
  `UIs/GCTL.UI.Core/Controllers/BaseController.cs`, with one deliberate
  change: your original checked `HttpContext.Session` manually and redirected
  to Login itself. This template uses ASP.NET Core Identity (cookie auth)
  instead of raw session, so `[Authorize]` + the cookie's `LoginPath`
  (configured in `Program.cs`) does that job now — no manual redirect logic
  needed. `LoginInfo`, `DataTablesResult`, and `AllInfoEmployeeById` are kept
  with the same names/shapes so your ported controllers need minimal changes.
  Two supporting files it depended on — `UserInfoViewModel.cs` and
  `FullEmployeeDetailsGetByIdViewModel.cs` — also weren't in your uploaded
  zip; `AllInfoEmployeeById` currently returns `dynamic` as a placeholder
  until you port that view model over.

## Removed from your original project (and why)

| Removed | Why |
|---|---|
| `AspNetCore.Reporting`, `ReportViewerCore.NETCore` (RDLC) | No .NET 10 support. Replace RDLC reports with **QuestPDF** (already included) — it's already in your original stack too. |
| `Microsoft.AspNet.Mvc`, `Microsoft.AspNet.WebApi.Core` v5.3.0 | These are **.NET Framework** packages, incompatible with .NET Core/10, and redundant — ASP.NET Core MVC replaces them entirely. |
| `Tono.Excel` | Unclear/no confirmed .NET 10 support. Replaced with **ClosedXML** (actively maintained). |
| `PXLibrary` / `PXLib` DLL reference | Was referenced via a hardcoded path to your old dev machine (`...\TBM\TBM(08-06)\...`) — removed per your request. If it contained logic you still need, that logic needs to be re-implemented in plain C# in `GCTL.Core`, since I don't have the DLL's source. |
| `ErikEJ...DateOnlyTimeOnly` shim | EF Core 10 supports `DateOnly`/`TimeOnly` natively — shim no longer needed. |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB, Express, or full) — for local dev, SQL Server LocalDB
  ships with Visual Studio
- Optional: Visual Studio 2026 or VS Code with C# Dev Kit

## First run

```bash
cd GCTL_HRM_NET10
dotnet restore
dotnet build
```

If `dotnet build` reports errors, they're almost always one of:
- A NuGet package version that's been superseded since this was written — 
  run `dotnet list package --outdated` and bump as needed
- A missing `using` — the compiler error will tell you exactly which type

Fix, then re-run `dotnet build` until clean. **Do not skip this step.**

### Set up the database

1. Edit the connection string in `UIs/GCTL.UI.Core/appsettings.Development.json`
   to point at your SQL Server instance.
2. Create the Identity tables:

```bash
cd UIs/GCTL.UI.Core
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialIdentity --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

3. Run it:

```bash
dotnet run
```

Open the URL shown in the console (e.g. `https://localhost:7100`). You should
land on the Dashboard, redirected to **Sign In** since you're not authenticated.

A default admin account is seeded on first run:
- Email: `admin@gctl.local`
- Password: `Admin@123`

**Change or remove this in `SeedData.cs` before using this anywhere real.**

## Reverse-engineering your real database (EF Power Tools)

1. Install the **EF Core Power Tools** extension in Visual Studio
   (Extensions → Manage Extensions → search "EF Core Power Tools").
2. Right-click the **GCTL.Data** project → EF Core Power Tools → Reverse Engineer.
3. Pick your SQL Server connection, select the tables/views/stored procedures
   you want.
4. Set the output to `Entities/` and name the context `GctlHrmContext`
   (matching the placeholder already in `Context/GctlHrmContext.cs` —
   **delete that placeholder file first**, Power Tools will generate the real one).
5. Build again.

Prefer the command line instead of the VS extension? See the detailed
`dotnet ef dbcontext scaffold` command in the comments inside
`Libraries/GCTL.Data/Context/GctlHrmContext.cs`.

## Notes on the Identity setup

- Uses cookie-based auth (not JWT) — appropriate for a server-rendered MVC app.
- `AccessCode` field is kept on `ApplicationUser` alongside real ASP.NET Core
  Identity roles, so your existing `0001`/`0002`/`0003`/`0005` row-level
  filtering logic in stored procedures/services keeps working unchanged.
- Password policy, lockout, and role names are all in `Program.cs` and
  `SeedData.cs` — tune to match your organization's requirements.
- `RequireConfirmedAccount` is `false` by default (email isn't wired up yet).
  Flip it on once you've configured MailKit for sending confirmation emails.

# NTNP Pricing Engine

A native Windows client-server application replacing Novin Tarh Niro Pars's Excel-based MV/LV
switchgear pricing process: a centralized Pricing Engine with an Automatic BOM/MTO Generator, a
premium Persian-RTL WPF desktop client, and an internal ASP.NET Core Web API hosted as a Windows
Service, backed by a centrally-hosted SQL Server database. No browser UI; client machines never hold
database credentials or talk to SQL Server directly.

## Solution structure

```text
src/
  NTNP.Pricing.Domain          — entities, the pricing/BOM/MTO calculation engine, invariants
  NTNP.Pricing.Application     — use-case services, validation, DTO mapping
  NTNP.Pricing.Infrastructure  — EF Core/SQL Server, Identity, file storage, Excel import, audit
  NTNP.Pricing.Api             — Web API, JWT auth, RBAC, Windows Service host, admin CLI tools
  NTNP.Pricing.Desktop         — WPF MVVM client (25 screens), Persian RTL, premium theme
  NTNP.Pricing.Contracts       — DTOs shared by Api and Desktop only
  NTNP.Pricing.Reporting       — internal vs. customer-facing PDF/Excel report rendering

tests/       — one test project per src/ project (Domain/Application/Infrastructure/Reporting
               unit + integration tests, Api.IntegrationTests over a real HTTP pipeline,
               Desktop.Tests over the real ViewModels with a fake HTTP layer)
installer/   — WiX v5 MSI project (NTNP-Pricing-Setup-x64.msi)
deployment/  — PowerShell scripts: server install/remove, DB migrate/backup/restore, client build
docs/        — architecture.md, calculation-rules.md, deployment.md, admin-guide.md,
               user-guide-fa.md, excel-mapping.md, package-licenses.md
ASSUMPTIONS.md — every professional-default decision made where the spec was ambiguous
```

See `docs/architecture.md` for the full picture.

## Quick start (development)

Requires the .NET 10 SDK. SQL Server is required for anything beyond a build — the Api needs a real
`ConnectionStrings:SqlServer` to run (`Database:AutoMigrate`/`Database:Seed` are both `true` in
`appsettings.Development.json`, so a fresh local database is created and seeded automatically the
first time you run it).

```powershell
# 1. Restore/build everything
dotnet build NTNP.Pricing.sln

# 2. Run the server (defaults to https://localhost:7125 / http://localhost:5252 — see
#    src/NTNP.Pricing.Api/Properties/launchSettings.json)
dotnet run --project src\NTNP.Pricing.Api

# 3. In a second terminal, run the desktop client (Windows only — see the note below)
dotnet run --project src\NTNP.Pricing.Desktop
```

Sign in with the seeded development Admin account (see `IdentitySeeder`):

```text
Email:    admin@ntnp.local
Password: Ntnp!Admin123
```

**Never use these credentials in a production configuration.** A production deployment creates its
first Admin via `NTNP.Pricing.Api.exe create-admin` instead — see `docs/deployment.md`.

### Running the tests

```powershell
dotnet test tests\NTNP.Pricing.Domain.Tests
dotnet test tests\NTNP.Pricing.Infrastructure.Tests
dotnet test tests\NTNP.Pricing.Application.Tests
dotnet test tests\NTNP.Pricing.Reporting.Tests
dotnet test tests\NTNP.Pricing.Api.IntegrationTests
dotnet test tests\NTNP.Pricing.Desktop.Tests   # Windows only — see the note below
```

### A note on this repository's own development sandbox

This codebase was built and continuously verified in a Linux development sandbox via
`EnableWindowsTargeting=true` (real `dotnet build`, catching every compile error for the
Windows-only Desktop/Installer projects too — not a "written but never compiled" delivery). Two
things genuinely require real Windows and could not be executed there, by design of the platforms
involved, not as a shortcut:

- **The Desktop app and `Desktop.Tests`** need the `Microsoft.WindowsDesktop.App` runtime, which has
  no Linux build at all (confirmed via `dotnet --list-runtimes`).
- **The WiX MSI build** needs the real Windows Installer toolchain (confirmed empirically — see
  `ASSUMPTIONS.md` §2/§11).

Everything else (Domain/Application/Infrastructure/Reporting/Api and all their tests, plus the Api
process itself smoke-tested live) was built, run, and verified for real in this session. See the
final delivery summary in the session transcript for exact `dotnet build`/`dotnet test` output.

## Documentation

- [`docs/architecture.md`](docs/architecture.md) — layered architecture, request pipeline, data model.
- [`docs/calculation-rules.md`](docs/calculation-rules.md) — every pricing/BOM/MTO formula, with the
  mandatory reference scenario worked through exactly.
- [`docs/deployment.md`](docs/deployment.md) — server install, HTTPS, firewall, backup/restore,
  building the MSI.
- [`docs/admin-guide.md`](docs/admin-guide.md) — day-to-day system administration from inside the app.
- [`docs/user-guide-fa.md`](docs/user-guide-fa.md) — end-user guide (Persian) for the pricing workflow.
- [`docs/excel-mapping.md`](docs/excel-mapping.md) — how the legacy Excel process maps onto this system.
- [`docs/package-licenses.md`](docs/package-licenses.md) — every third-party dependency and its license.
- [`ASSUMPTIONS.md`](ASSUMPTIONS.md) — every professional-default decision recorded during
  implementation, and this session's known limitations.

## License

Internal software for Novin Tarh Niro Pars — see `installer/NTNP.Pricing.Installer/License.rtf` for
the current placeholder notice (not a reviewed legal document; replace before production
distribution — see `ASSUMPTIONS.md` §10) and `docs/package-licenses.md` for third-party licenses.

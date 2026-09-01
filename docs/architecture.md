# Architecture

## Overview

NTNP Pricing Engine is a native Windows client-server application: a WPF desktop client talks only
to an internal ASP.NET Core Web API (hosted as a Windows Service), which is the sole component with
SQL Server credentials. No browser, no Electron, no direct database access from any client machine
(Section 1/33/40 of the master prompt).

```text
┌─────────────────────────┐        HTTPS / JSON        ┌──────────────────────────────┐        ┌────────────────┐
│  NTNP.Pricing.Desktop    │ ───────────────────────────▶│  NTNP.Pricing.Api             │───────▶│  SQL Server     │
│  (WPF, MVVM, Persian RTL)│◀─────────────────────────── │  (Windows Service, Kestrel)   │◀───────│  (central)      │
└─────────────────────────┘                              └──────────────────────────────┘        └────────────────┘
                                                                    │
                                                                    ▼
                                                           NTNP.Pricing.Reporting
                                                       (Playwright/Chromium → PDF, ClosedXML → xlsx)
```

## Layered solution structure

```text
src/
  NTNP.Pricing.Domain          — entities, calculation engine, invariants. No package dependencies.
  NTNP.Pricing.Application     — use-case services, DTO mapping, FluentValidation, authorization intent.
  NTNP.Pricing.Infrastructure  — EF Core/SQL Server, ASP.NET Core Identity, file storage, Excel import, audit persistence.
  NTNP.Pricing.Api             — controllers, JWT auth, RBAC policies, Windows Service host, admin CLI utilities.
  NTNP.Pricing.Desktop         — WPF MVVM client (25 screens), its own HTTP API clients, no EF/SQL reference at all.
  NTNP.Pricing.Contracts       — request/response DTOs shared by Api and Desktop. No entity/EF types leak across this boundary.
  NTNP.Pricing.Reporting       — internal vs. customer-facing report models + renderers (Playwright PDF, ClosedXML xlsx).
```

Dependency direction is strictly one-way: `Domain ← Application ← Infrastructure`, `Application ←
Api`, `Contracts ← {Api, Desktop, Reporting}`. `Desktop` depends on `Contracts` only — it cannot
reference `Domain`/`Infrastructure`/EF Core even by accident, which is what makes "the desktop
client never touches SQL Server directly" (Section 33) a compile-time guarantee, not just a
convention.

### Why this split

- **Domain has zero package references.** `PricingCalculationEngine`, `ProjectLineCalculator`,
  `ProjectTotalsCalculator`, `MtoCalculator` (Sections 9/14/15/16/17/18/19) are pure static methods
  over plain entities — they can be unit-tested (and were: `Domain.Tests`) without a database, a web
  host, or mocks of either.
- **Application owns "what can happen".** Every use case (create a project, add a line, override a
  field, submit for approval, decide, lock, generate a new revision) is one method on one service,
  each enforcing its own invariants (immutability of approved revisions, override reason
  requirements, approval-state transitions) independent of whether it's called from a controller or
  a test.
- **Infrastructure owns "how it's stored".** EF Core configurations, migrations, the Identity store,
  the Excel importer, and audit persistence all live here so a future storage change (e.g. a
  different provider) never touches Application/Domain.
- **Contracts is the only thing Desktop and Api both reference.** This is what enforces Section 33
  end to end: the client can never accidentally get an `Entity`/`DbContext` reference, and the Api
  never accidentally leaks an EF navigation property into a JSON response.

## Server-side request pipeline

`Program.cs` (top-level statements, `net10.0`): Serilog bootstrap logger → `UseWindowsService()` (a
no-op under `dotnet run`/Kestrel-standalone, active when the Service Control Manager starts the same
executable) → DI registration (`AddInfrastructure` → `AddApplication` → `AddReporting` →
`AddNtnpAuthorization`) → JWT bearer authentication → `AddControllers` with a generic
`ValidationActionFilter` that resolves `IValidator<T>` per action argument automatically → build →
`ExceptionHandlingMiddleware` (translates `NotFoundException`/`DomainValidationException`/
`AuthenticationFailedException`/FluentValidation's `ValidationException`/
`DbUpdateConcurrencyException` into the right HTTP status + a structured `ApiErrorResponse`, so no
controller hand-writes error handling) → `UseAuthentication`/`UseAuthorization` → `MapControllers`.

Every controller action carries an explicit `[Authorize(Policy = PolicyNames.X)]` — Section 6's
"hiding a button is not authorization" is enforced server-side on every single endpoint, never left
to the desktop client to self-police.

Two non-web CLI modes share the exact same DI-configured host instead of separate console projects:
`NTNP.Pricing.Api.exe migrate` (Section 35's migration utility) and
`NTNP.Pricing.Api.exe create-admin` (Section 35's initial-admin utility) — see
`src/NTNP.Pricing.Api/Tools/AdminBootstrap.cs` and `docs/deployment.md`.

## Calculation and immutability model

A `Project` has many `ProjectRevision`s; only one is "current" (mutable) at a time. A revision holds
`ProjectLine`s, each with its own `ProjectLineBomItem`s (electrical BOM, Section 15) and
`ProjectLineBodyEsItem`s (Section 11). Every line and every BOM/BODY+ES item snapshots the
equipment/component unit cost and exchange rate **at the moment the line was generated or last
recalculated** (`UnitCostIrrSnapshot`, `PurchaseExchangeRateSnapshot`) — later price changes never
silently alter an existing revision's numbers (Section 40 "Price and exchange-rate snapshots are
preserved"). `ProjectRevisionRecalculator`/`ProjectTotalsCalculator` re-derive every cost/selling
price/reconciliation figure from those snapshots plus the revision's own pricing settings
(Markup/GrossMargin, Rial/Foreign share, rounding policy) — nothing is calculated once and cached
in a way that could drift from its inputs.

Once a revision reaches `Approved` or `Locked` status, `ProjectRevision.IsImmutable` becomes `true`
and every mutating Application-layer method (`AddLineAsync`, `OverrideLineFieldAsync`,
`UpdateLineQuantityAsync`, …) throws `DomainValidationException` before touching the database
(Section 40 "Approved revisions are immutable"). The only way forward from there is
`CreateNewRevisionUsingLatestPricesAsync`, which creates a **new** revision from the current
equipment/exchange-rate data, leaving the approved one untouched.

## Concurrency (Section 31)

Every mutable entity carries a SQL Server `rowversion` column (`Entity.RowVersion`, mapped via
`.IsRowVersion()` on every configuration except the append-only `AuditLogEntry`). Every update
service sets `_db.Entry(entity).Property(x => x.RowVersion).OriginalValue = request.RowVersion`
before saving; a mismatch (someone else saved first) raises `DbUpdateConcurrencyException`, which
`ExceptionHandlingMiddleware` turns into a 409 with a message telling the user to reload and
re-apply their change — never a silent overwrite.

## Authentication and authorization (Section 6/7)

ASP.NET Core Identity (Guid-keyed `ApplicationUser`/`ApplicationRole`) issues short-lived JWT access
tokens (15 min) plus rotating, hashed, one-time-use refresh tokens (7 days) — see
`src/NTNP.Pricing.Infrastructure/Auth/AuthService.cs`. Five roles (Admin, Engineering, Commercial,
Approver, Viewer) map to twelve named authorization policies
(`src/NTNP.Pricing.Api/Authorization/PolicyNames.cs`), each enforced per endpoint. A failed login or
an invalid/rotated-out refresh token returns a distinct `AuthenticationFailedException` → HTTP 401,
kept separate from `DomainValidationException` → 422 so "you typed the wrong password" and "this
field is invalid" never look like the same class of error to a client.

## Reporting pipeline (Sections 23/26/27/29)

`NTNP.Pricing.Reporting` renders two structurally distinct model families from the same revision
data: `CustomerQuotationModel` (Section 26 — deliberately shaped to contain *only* fields the
customer is allowed to see: no cost, purchase price, markup, margin, supplier, or override reason —
Section 29's automated `ReportSecurityTests` assert this by construction, not by filtering at render
time) and `InternalCostingReportModel`/`BomMtoReportModel`/`RevisionComparisonReportModel` (full
cost/margin detail, an INTERNAL-CONFIDENTIAL banner on every page). Both are rendered to PDF via
real HTML/CSS through headless Chromium (Microsoft.Playwright) — chosen over MigraDoc/PdfSharp
specifically because those lack bidirectional-text support and cannot lay out Persian RTL mixed with
Latin/numeric content correctly (`ASSUMPTIONS.md` §1). Excel exports use ClosedXML with
`sheet.RightToLeft = true`.

## Desktop client (Section 22/23/33)

WPF, .NET 10, MVVM via CommunityToolkit.Mvvm's source generators. `App.xaml.cs` is a small Generic
Host composition root: Login window first (never lets any screen call the API before a token
exists), hands off to the Shell window on success; logout tears the shell down and re-shows Login
without exiting the process. `INavigationService` is view-model-first — the shell's `ContentControl`
resolves the current screen purely from a `DataTemplate` keyed on the view model's type
(`Views/ViewTemplates.xaml`), so adding a screen never touches shell code. `ApiClientBase` is the
single HTTP channel every typed client (`CustomersApiClient`, `ProjectRevisionsApiClient`, …) is
built on: it resolves the current server address live from `IServerConnectionSettingsService` on
every call (so re-pointing the client via the Server Connection Settings screen takes effect
immediately), attaches the bearer token, and transparently refreshes-and-retries once on a 401.

## Database schema (see also the final delivery summary's schema table)

25 explicit entity configurations plus ASP.NET Core Identity's own 6 standard tables (Users, Roles,
UserRoles, UserClaims, UserLogins, RoleClaims — UserTokens included), applied by one EF Core
migration (`InitialCreate`). Grouped by module: Customers; Currencies + ExchangeRates; Equipment +
EquipmentPrices; ProductFamilies + PanelTypes (Section 3's admin-editable lookups, not enums);
PanelTemplates + PanelTemplateBomItems; BodyEsTemplates + BodyEsTemplateItems; PricingProfiles;
Projects + ProjectRevisions + ProjectLines + ProjectLineBomItems + ProjectLineBodyEsItems +
ProjectLineOverrides + ApprovalRecords; AuditLogEntries (append-only); StoredFiles;
CompanySettings; RefreshTokens.

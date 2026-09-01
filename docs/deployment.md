# Deployment Guide

This covers both halves of Section 35's "Server Deployment Package": the internal API/Windows
Service on the company server, and the desktop client's MSI installer. Every script referenced here
lives under `deployment/` or `installer/` and is a real, complete script — not pseudocode.

## 1. Prerequisites

- **Server**: Windows Server 2019+ (or Windows 10/11 for a small pilot), SQL Server 2019+ reachable
  from it, a valid TLS certificate for the API's hostname.
- **Client build machine**: Windows 10/11 or Windows Server with the .NET 10 SDK. The MSI build step
  (WiX) needs Windows specifically — see §5 and `ASSUMPTIONS.md` §2/§11 for why this repository's own
  Linux development sandbox cannot produce the `.msi` itself, confirmed empirically in this session.
- Both machines need outbound access to `api.nuget.org` for the first restore/publish (no internet
  access is needed at runtime after that).

## 2. Server: SQL Server setup

1. Create the database and a dedicated login with only the permissions this app needs:

   ```sql
   CREATE DATABASE NTNP_Pricing;
   CREATE LOGIN ntnp_pricing_app WITH PASSWORD = 'a-real-generated-password';
   USE NTNP_Pricing;
   CREATE USER ntnp_pricing_app FOR LOGIN ntnp_pricing_app;
   ALTER ROLE db_datareader ADD MEMBER ntnp_pricing_app;
   ALTER ROLE db_datawriter ADD MEMBER ntnp_pricing_app;
   ALTER ROLE db_ddladmin ADD MEMBER ntnp_pricing_app;  -- needed for `migrate` to run schema changes
   ```

   Never use `sa` in `ConnectionStrings:SqlServer` (Section 35 "Do not include real production
   credentials" — this applies to the *documentation*; the running server's actual credentials live
   only in its local `appsettings.Production.json`, never in source control).

2. Recovery model: `SIMPLE` is fine for most deployments (this app has no requirement for
   point-in-time recovery beyond daily fulls); switch to `FULL` and add log backups if your
   organization's RPO requires it — `backup-database.ps1` reports the current recovery model on
   every run as a reminder.

## 3. Server: publish and install the Windows Service

```powershell
# On the build machine:
dotnet publish src\NTNP.Pricing.Api\NTNP.Pricing.Api.csproj -c Release -r win-x64 `
    -p:SelfContained=true -p:PublishReadyToRun=true -o .\publish-api

# Copy .\publish-api to the server, then on the server (elevated PowerShell):
cd deployment\server
.\install-service.ps1 -InstallPath 'C:\Program Files\NTNP\Pricing Server' -SourcePath 'C:\path\to\publish-api'
```

`install-service.ps1` copies the files, drops a template `appsettings.Production.json` if one
doesn't already exist, creates `C:\NTNP\Pricing\Storage` and `C:\NTNP\Pricing\Logs`, and registers
the `NTNPPricingService` Windows Service (auto-restart on failure, three attempts). It does **not**
start the service — finish the steps it prints first:

1. **Edit `appsettings.Production.json`** at the install path — set `ConnectionStrings:SqlServer`,
   generate a real `Jwt:SigningKey` (see the comment in
   `deployment/server/appsettings.Production.template.json` for a one-liner), set
   `FileStorage:RootPath`, and the `Kestrel:Endpoints:Https` certificate path/password (§4 below).
2. **Firewall** — open the port:
   ```powershell
   New-NetFirewallRule -DisplayName 'NTNP Pricing API' -Direction Inbound -Protocol TCP -LocalPort 7240 -Action Allow
   ```
3. **Migrate**: `deployment\database\migrate.ps1 -InstallPath 'C:\Program Files\NTNP\Pricing Server'`
   (applies EF Core migrations; asks you to confirm you've taken a backup first — see §6).
4. **Create the first admin**:
   `deployment\database\create-admin.ps1 -InstallPath 'C:\Program Files\NTNP\Pricing Server'`
5. **Start it**: `Start-Service -Name NTNPPricingService`
6. **Verify**: `deployment\server\health-check.ps1 -BaseUrl https://ntnp-pricing-server:7240`

### Removal / rollback

`deployment\server\remove-service.ps1` stops and unregisters the service. It never deletes the
database or `C:\NTNP\Pricing\Storage` — pass `-RemoveFiles -InstallPath ...` only to also delete the
program binaries themselves (Section 34 "uninstall must never delete server data").

### Upgrading to a new version

1. Take a backup (§6).
2. Publish the new version and copy it over the install path (or re-run `install-service.ps1` after
   `remove-service.ps1` — either way, `appsettings.Production.json` is not overwritten since
   `install-service.ps1` only creates it "if one doesn't already exist").
3. Run `migrate.ps1` again (no-ops cleanly if there's nothing pending).
4. Restart the service, then `health-check.ps1`.

Rollback: stop the service, restore the pre-upgrade backup with `restore-database.ps1 -Overwrite`
(§6), redeploy the previous version's binaries, restart.

## 4. HTTPS certificate

Any of the following works — Kestrel just needs a `.pfx` it can load:

- **Internal CA** (typical for an internal-only server): issue a certificate for the server's
  hostname from your organization's CA, export as `.pfx`, place at the path configured in
  `Kestrel:Endpoints:Https:Certificate:Path`.
- **Public CA / Let's Encrypt**: if the server is reachable from the internet under a real domain.
- **Self-signed, pilot-only**: `New-SelfSignedCertificate -DnsName ntnp-pricing-server -CertStoreLocation Cert:\LocalMachine\My`,
  then export it to a `.pfx`. Desktop clients will need to trust it manually (or via Group Policy) —
  do not ship a self-signed cert to production users without a plan to distribute trust for it.

The desktop client itself performs no certificate pinning — it uses the OS trust store like any
other `HttpClient`, so once the certificate is trusted machine-wide (or is from a publicly trusted
CA), every client works with no per-machine configuration.

## 5. Client: building the MSI installer

```powershell
# On a Windows build machine:
cd deployment\client
.\build-installer.ps1 -Version 1.0.0
```

This runs the publish → WiX build → portable-zip pipeline described in the script's own header
comment, producing:

- `installer\NTNP.Pricing.Installer\bin\Release\NTNP-Pricing-Setup-x64.msi` — Section 34's required
  output filename.
- `installer\NTNP.Pricing.Installer\bin\Release\NTNP-Pricing-Portable-1.0.0-x64.zip` — the "also
  produce a self-contained portable build for testing" requirement; unzip and run
  `NTNP.Pricing.Desktop.exe` directly, no installer needed.

Pass `-SignPfx`/`-SignPfxPassword` to Authenticode-sign both outputs (Section 34 "Ready for digital
signing") — omit them to produce unsigned builds (expect a SmartScreen warning until signed).

**This step cannot run in this repository's own Linux development sandbox** — confirmed empirically
in this session (not just assumed): the WiX .NET tool and a real self-contained `win-x64` publish
both work fine here, but the WiX bind step itself fails non-deterministically after printing its own
"only supports Windows ... behavior after this point is undefined" warning. Build the installer on
Windows. See `ASSUMPTIONS.md` §2/§11 for the full account, and `installer/NTNP.Pricing.Installer/`
for the reviewed-but-not-compiler-verified WiX sources.

### Installer QA checklist (run once per release, on Windows)

- [ ] Fresh install: shortcuts appear (Start Menu always; Desktop only if the checkbox was ticked),
      app launches, Server Connection Settings shows the address entered during setup.
- [ ] `HKLM\Software\NTNP\Pricing\ServerUrl` contains the address entered during setup.
- [ ] Upgrade install over an existing install: per-user `client-settings.json` (a custom server
      address a user set themselves) survives; app version bumps.
- [ ] Uninstall: shortcuts removed, Program Files folder removed; `C:\NTNP\Pricing\Storage`/database
      untouched (this client never has server data locally to begin with, but verify the folder
      installer touched is gone and nothing else is).
- [ ] Wizard Back/Next navigation through the custom "Server Connection" page works in both
      directions — the WiX `Publish` `Order` values in `ServerAddressDialog.wxs` were chosen
      defensively (a high `Order` to reliably win over the stock library's own low-numbered rows)
      but were never visually verified; confirm on first real build and adjust if needed.

## 6. Database backup, restore, and retention (Section 36)

```powershell
# Full backup with verification:
deployment\backup\backup-database.ps1 -ServerInstance ntnp-sql-01 -DatabaseName NTNP_Pricing -BackupRoot D:\Backups\NTNP-Pricing

# Restore test (safe — restores into a NEW scratch database, live DB untouched):
deployment\backup\restore-database.ps1 -ServerInstance ntnp-sql-01 -BackupFile D:\Backups\NTNP-Pricing\NTNP_Pricing-Full-20260101-020000.bak

# Real rollback (overwrites the live database — requires typed confirmation):
deployment\backup\restore-database.ps1 -ServerInstance ntnp-sql-01 -BackupFile D:\Backups\...\NTNP_Pricing-Full-20260101-020000.bak -Overwrite
```

- **Schedule** `backup-database.ps1` via Windows Task Scheduler (daily, off-peak) — the script itself
  is idempotent and safe to run more often.
- **Retention**: the script lists (never deletes) backups older than `-RetentionDays` (default 30);
  your organization should own the actual deletion policy and schedule, since "how long is enough"
  is a business decision, not a technical one — see the script's own header comment.
- **Restore test**: run `restore-database.ps1` (default, non-`-Overwrite` mode) periodically against
  your latest backup — this *is* Section 36's "restore test" requirement, using the exact same
  script as a real restore so there's no drift between "the restore procedure we tested" and "the
  restore procedure we'd actually run".
- **Pre-upgrade backup**: `migrate.ps1` refuses to proceed without an explicit "yes" confirming a
  backup was taken first.

## 7. Desktop client "verification" note (Section 22/23/29-style visual QA)

This session built and reviewed all 25 desktop screens but could not run or visually inspect the
WPF client itself — WPF requires the real Windows Desktop runtime, which has no Linux build (see
`ASSUMPTIONS.md` §11). Before the Desktop app is considered visually signed off, on a real Windows
machine:

1. `dotnet test tests\NTNP.Pricing.Desktop.Tests` — should show the same pass count reported in the
   final delivery summary; this is the first real execution of that suite (it only ever *built*, not
   *ran*, in the development sandbox).
2. Run the app (`dotnet run --project src\NTNP.Pricing.Api` in one terminal, then
   `dotnet run --project src\NTNP.Pricing.Desktop` in another, pointed at the dev API), log in with
   the seeded dev admin credentials, and walk every one of the 25 screens listed in the final
   delivery summary — Section 29-style: look for clipped text, RTL layout errors, and confirm every
   button performs a real action against the API (none of them are placeholders, but this is the
   step that actually proves it visually).
3. Test at 100%/125%/150% Windows display scaling and at 1366×768 and 1920×1080 (Section 23).

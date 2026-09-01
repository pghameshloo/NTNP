#Requires -Version 5.1
<#
.SYNOPSIS
    Section 35 "SQL Server migration utility" — applies pending EF Core migrations.

.DESCRIPTION
    Runs `NTNP.Pricing.Api.exe migrate`, which connects using the server's own configured
    ConnectionStrings:SqlServer (production appsettings.json / environment variables — the same
    connection string the running service uses) and applies any pending migrations, then exits.

    ALWAYS take a full backup first (see deployment/backup/backup-database.ps1) — this is Section
    36's "Pre-upgrade backup" requirement. This script does not take that backup for you, so it
    cannot be run unattended without a preceding backup step; wire the two together in your own
    change-management/CI pipeline once you've decided your backup retention policy.

.PARAMETER InstallPath
    Folder containing NTNP.Pricing.Api.exe.

.EXAMPLE
    .\backup-database.ps1 -BackupRoot D:\Backups\NTNP-Pricing -ServerInstance '.\SQLEXPRESS' -DatabaseName NTNP_Pricing
    .\migrate.ps1 -InstallPath 'C:\Program Files\NTNP\Pricing Server'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InstallPath
)

$ErrorActionPreference = 'Stop'

$exePath = Join-Path $InstallPath 'NTNP.Pricing.Api.exe'
if (-not (Test-Path $exePath)) {
    throw "NTNP.Pricing.Api.exe was not found at '$exePath'. Pass -InstallPath pointing at the server deployment folder."
}

Write-Host "Have you taken a fresh backup? (Section 36 requires a pre-upgrade backup before every migration.)" -ForegroundColor Yellow
$confirmation = Read-Host "Type 'yes' to continue"
if ($confirmation -ne 'yes') {
    Write-Host "Aborted — run deployment/backup/backup-database.ps1 first." -ForegroundColor Yellow
    exit 1
}

Write-Host "Running: $exePath migrate" -ForegroundColor Cyan
& $exePath migrate
exit $LASTEXITCODE

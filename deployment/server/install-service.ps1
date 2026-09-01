#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Section 34/35 — installs the NTNP Pricing Engine API as a Windows Service.

.DESCRIPTION
    Copies a self-contained win-x64 publish of NTNP.Pricing.Api (see the -SourcePath parameter) into
    -InstallPath, applies the production appsettings.json template if none exists yet there, and
    registers a Windows Service that runs NTNP.Pricing.Api.exe (the API's Program.cs already calls
    UseWindowsService(), so the same executable that runs standalone via `dotnet run` for local
    development is exactly what the Service Control Manager starts/stops here — no separate
    "service host" build).

    This script does NOT run database migrations or seed data — run
    deployment/database/migrate.ps1 and deployment/database/create-admin.ps1 separately, in that
    order, after the service is installed but before you start it for the first time against a
    brand-new database (or let the service start once with Database:AutoMigrate temporarily enabled
    for a first-time setup — production appsettings.json ships with it OFF, per Section 35's
    preference for an explicit, reviewed migration step).

.PARAMETER InstallPath
    Target folder for the service's files, e.g. 'C:\Program Files\NTNP\Pricing Server'.

.PARAMETER SourcePath
    Folder containing the published NTNP.Pricing.Api output (from
    `dotnet publish -c Release -r win-x64 -p:SelfContained=true`). If omitted, the script assumes
    -InstallPath already contains the published files (e.g. a prior manual copy or CI deployment
    step) and only performs the service-registration steps.

.PARAMETER ServiceName
    Windows Service name. Default: NTNPPricingService.

.PARAMETER ServiceAccount
    Account the service runs as. Default: LocalSystem. For least-privilege, create a dedicated
    virtual/managed service account with access to the SQL Server database and the FileStorage
    folder, and pass it here (you'll be prompted for its password via -Credential instead).

.PARAMETER Port
    HTTPS port Kestrel binds to (written into appsettings.json's Kestrel:Endpoints:Https:Url).
    Default: 7240 (matches the example in Section 33 of the master prompt).

.EXAMPLE
    .\install-service.ps1 -InstallPath 'C:\Program Files\NTNP\Pricing Server' -SourcePath '.\publish'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InstallPath,

    [string]$SourcePath,

    [string]$ServiceName = 'NTNPPricingService',

    [string]$ServiceAccount = 'LocalSystem',

    [int]$Port = 7240
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    throw "A service named '$ServiceName' already exists. Run remove-service.ps1 first if you intend to reinstall, or use a different -ServiceName."
}

New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

if ($SourcePath) {
    Write-Host "Copying published files from '$SourcePath' to '$InstallPath'..." -ForegroundColor Cyan
    robocopy $SourcePath $InstallPath /MIR /NFL /NDL /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE while copying published files." }
}

$exePath = Join-Path $InstallPath 'NTNP.Pricing.Api.exe'
if (-not (Test-Path $exePath)) {
    throw "NTNP.Pricing.Api.exe was not found at '$exePath'. Pass -SourcePath pointing at a self-contained win-x64 publish, or copy the files there manually first."
}

$appsettingsProdPath = Join-Path $InstallPath 'appsettings.Production.json'
if (-not (Test-Path $appsettingsProdPath)) {
    $templatePath = Join-Path $scriptRoot 'appsettings.Production.template.json'
    Write-Host "No appsettings.Production.json found — copying the template. EDIT IT before starting the service (connection string, Jwt:SigningKey, FileStorage:RootPath)." -ForegroundColor Yellow
    Copy-Item $templatePath $appsettingsProdPath
}

New-Item -ItemType Directory -Path 'C:\NTNP\Pricing\Storage' -Force | Out-Null
New-Item -ItemType Directory -Path 'C:\NTNP\Pricing\Logs' -Force | Out-Null

Write-Host "Registering Windows Service '$ServiceName'..." -ForegroundColor Cyan
$binPath = "`"$exePath`""
sc.exe create $ServiceName binPath= $binPath start= auto obj= $ServiceAccount DisplayName= "NTNP Pricing Engine" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed with exit code $LASTEXITCODE." }

sc.exe description $ServiceName "NTNP Pricing Engine — centralized MV/LV switchgear pricing, BOM and MTO server (Section 1/34)." | Out-Null
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host ""
Write-Host "Service '$ServiceName' installed but NOT started." -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Green
Write-Host "  1. Edit '$appsettingsProdPath' — set ConnectionStrings:SqlServer, Jwt:SigningKey, FileStorage:RootPath."
Write-Host "  2. Open the firewall port: deployment\server\configure-firewall.ps1 -Port $Port"
Write-Host "  3. Configure the HTTPS certificate: deployment\server\configure-https-selfsigned.ps1 (pilot/internal-only — see docs/deployment.md 'HTTPS certificate' for the internal-CA/public-CA options a real production deployment should prefer)."
Write-Host "  4. Run deployment\database\migrate.ps1 -InstallPath '$InstallPath'"
Write-Host "  5. Run deployment\database\create-admin.ps1 -InstallPath '$InstallPath'"
Write-Host "  6. Start-Service -Name $ServiceName"
Write-Host "  7. Verify: Invoke-RestMethod https://localhost:$Port/api/health"

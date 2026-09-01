#Requires -Version 5.1
<#
.SYNOPSIS
    Section 35 "Initial Admin creation utility" — creates the first production Admin account.

.DESCRIPTION
    Thin wrapper around `NTNP.Pricing.Api.exe create-admin`, which creates the account through the
    real ASP.NET Core Identity stack (correct password hashing, role assignment) — never a raw SQL
    INSERT. Safe to re-run: it refuses to touch an existing email rather than resetting it.

    Run this ONCE against a freshly migrated, empty production database (see migrate.ps1), from the
    server the API is installed on (so it picks up the production appsettings.json / connection
    string exactly as the running service will).

.PARAMETER InstallPath
    Folder containing NTNP.Pricing.Api.exe (see deployment/server/install-service.ps1's -InstallPath).

.PARAMETER Email
    Admin account email/username. Prompted for if omitted.

.PARAMETER DisplayName
    Admin account display name. Defaults to "System Administrator" if omitted and not prompted.

.EXAMPLE
    .\create-admin.ps1 -InstallPath 'C:\Program Files\NTNP\Pricing Server' -Email admin@ntnp.example
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InstallPath,

    [string]$Email,

    [string]$DisplayName
)

$ErrorActionPreference = 'Stop'

$exePath = Join-Path $InstallPath 'NTNP.Pricing.Api.exe'
if (-not (Test-Path $exePath)) {
    throw "NTNP.Pricing.Api.exe was not found at '$exePath'. Pass -InstallPath pointing at the server deployment folder."
}

$exeArgs = @('create-admin')
if ($Email) { $exeArgs += @('--email', $Email) }
if ($DisplayName) { $exeArgs += @('--display-name', $DisplayName) }
# Password is intentionally left for the interactive prompt inside the tool (masked input) rather
# than a script parameter — a plaintext password should never sit in shell history or a script arg.

Write-Host "Running: $exePath $($exeArgs -join ' ')" -ForegroundColor Cyan
& $exePath @exeArgs
exit $LASTEXITCODE

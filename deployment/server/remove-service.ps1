#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Section 34/35 — Windows Service removal script.

.DESCRIPTION
    Stops and unregisters the NTNP Pricing Engine Windows Service. Never deletes the installed
    files, the database, or the FileStorage folder — this only removes the service registration
    (and, optionally, the program files) so uninstalling never destroys server data (Section 34
    "Uninstall must never delete server data").

.PARAMETER ServiceName
    Windows Service name. Default: NTNPPricingService.

.PARAMETER RemoveFiles
    If set, also deletes -InstallPath's program files after the service is unregistered. The
    database, FileStorage folder (C:\NTNP\Pricing\Storage by default), and logs are NEVER touched by
    this switch — pass it only for the application binaries themselves.

.PARAMETER InstallPath
    Required only when -RemoveFiles is passed.

.EXAMPLE
    .\remove-service.ps1
    .\remove-service.ps1 -RemoveFiles -InstallPath 'C:\Program Files\NTNP\Pricing Server'
#>
[CmdletBinding()]
param(
    [string]$ServiceName = 'NTNPPricingService',
    [switch]$RemoveFiles,
    [string]$InstallPath
)

$ErrorActionPreference = 'Stop'

if ($RemoveFiles -and -not $InstallPath) {
    throw "-InstallPath is required when -RemoveFiles is specified."
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "No service named '$ServiceName' is registered — nothing to do." -ForegroundColor Yellow
}
else {
    if ($service.Status -ne 'Stopped') {
        Write-Host "Stopping service '$ServiceName'..." -ForegroundColor Cyan
        Stop-Service -Name $ServiceName -Force
        $service.WaitForStatus('Stopped', (New-TimeSpan -Seconds 30))
    }

    Write-Host "Removing service '$ServiceName'..." -ForegroundColor Cyan
    sc.exe delete $ServiceName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc.exe delete failed with exit code $LASTEXITCODE." }
}

if ($RemoveFiles) {
    Write-Host "Removing program files at '$InstallPath' (database and FileStorage folder are NOT touched)..." -ForegroundColor Cyan
    Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Done. The database and C:\NTNP\Pricing\Storage were left untouched — remove them yourself only after you are certain the data is no longer needed (Section 34: uninstall must never delete server data)." -ForegroundColor Green

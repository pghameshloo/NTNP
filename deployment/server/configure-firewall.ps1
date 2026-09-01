#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Opens the inbound Windows Firewall rule for the NTNP Pricing API (Section 35 "Firewall
    configuration"). Idempotent — safe to re-run on every deployment/upgrade.

.DESCRIPTION
    Creates (or updates) a single inbound TCP allow rule for the port Kestrel binds to. Run this once
    per server after install-service.ps1 and before pointing any desktop client at the server —
    without it, clients on the LAN cannot reach the API even though the service is Running locally.

.PARAMETER Port
    TCP port the API listens on. Must match Kestrel:Endpoints:Https:Url (or :Http:Url for an
    HTTP-only pilot) in appsettings.Production.json. Default: 7240 (docs/deployment.md's example).

.PARAMETER Remove
    Removes the rule instead of creating it (Section 34 "uninstall must never delete server data" —
    this is the one server-side firewall change install-service.ps1 asks you to make by hand; use
    this switch to undo it during a full decommission).

.EXAMPLE
    .\configure-firewall.ps1 -Port 7240

.EXAMPLE
    .\configure-firewall.ps1 -Port 7240 -Remove
#>
[CmdletBinding()]
param(
    [int]$Port = 7240,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$ruleName = 'NTNP Pricing API'

$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

if ($Remove) {
    if ($existing) {
        $existing | Remove-NetFirewallRule
        Write-Host "Removed firewall rule '$ruleName'." -ForegroundColor Green
    }
    else {
        Write-Host "No firewall rule named '$ruleName' exists — nothing to remove." -ForegroundColor Yellow
    }
    return
}

if ($existing) {
    Write-Host "Firewall rule '$ruleName' already exists — updating its port to $Port." -ForegroundColor Cyan
    $existing | Set-NetFirewallRule -LocalPort $Port -Protocol TCP -Direction Inbound -Action Allow
}
else {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
    Write-Host "Created firewall rule '$ruleName' allowing inbound TCP $Port." -ForegroundColor Green
}

Write-Host "Verify from a client machine on the LAN: Test-NetConnection <server-host> -Port $Port"

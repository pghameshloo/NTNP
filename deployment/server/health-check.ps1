#Requires -Version 5.1
<#
.SYNOPSIS
    Section 35 "Health-check instructions" — verifies the running service is up and reachable.

.DESCRIPTION
    Checks, in order: the Windows Service is Running, the anonymous /api/health endpoint responds
    with databaseReachable=true, and (if -Email/-Password are supplied) a real login succeeds — the
    same three checks the desktop client's "Test Connection" button performs (Section 33).

.PARAMETER BaseUrl
    API base URL, e.g. https://ntnp-pricing-server:7240.

.PARAMETER ServiceName
    Windows Service name. Default: NTNPPricingService.

.PARAMETER Email / Password
    Optional — if supplied, also verifies POST /api/auth/login succeeds end to end.

.EXAMPLE
    .\health-check.ps1 -BaseUrl https://ntnp-pricing-server:7240
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [string]$ServiceName = 'NTNPPricingService',

    [string]$Email,
    [string]$Password
)

$ErrorActionPreference = 'Stop'
$failed = $false

Write-Host "1) Windows Service status" -ForegroundColor Cyan
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "   FAIL: no service named '$ServiceName' is registered." -ForegroundColor Red
    $failed = $true
}
elseif ($service.Status -ne 'Running') {
    Write-Host "   FAIL: service status is '$($service.Status)', expected 'Running'." -ForegroundColor Red
    $failed = $true
}
else {
    Write-Host "   OK: service is Running." -ForegroundColor Green
}

Write-Host "2) GET $BaseUrl/api/health" -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get -TimeoutSec 15
    Write-Host "   apiVersion=$($health.apiVersion) databaseSchemaVersion=$($health.databaseSchemaVersion) databaseReachable=$($health.databaseReachable)"
    if (-not $health.databaseReachable) {
        Write-Host "   FAIL: server is reachable but the database is not. Check ConnectionStrings:SqlServer in appsettings.Production.json and that SQL Server is running/reachable from this machine." -ForegroundColor Red
        $failed = $true
    }
    else {
        Write-Host "   OK." -ForegroundColor Green
    }
}
catch {
    Write-Host "   FAIL: could not reach $BaseUrl/api/health — $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Check: service running, firewall rule for the port, HTTPS certificate bound, correct hostname/port." -ForegroundColor Red
    $failed = $true
}

if ($Email -and $Password) {
    Write-Host "3) POST $BaseUrl/api/auth/login" -ForegroundColor Cyan
    try {
        $body = @{ userNameOrEmail = $Email; password = $Password } | ConvertTo-Json
        $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 15
        Write-Host "   OK: logged in as $($login.user.displayName), roles: $($login.user.roles -join ', ')" -ForegroundColor Green
    }
    catch {
        Write-Host "   FAIL: login did not succeed — $($_.Exception.Message)" -ForegroundColor Red
        $failed = $true
    }
}
else {
    Write-Host "3) Login check skipped (pass -Email/-Password to include it)." -ForegroundColor Yellow
}

Write-Host ""
if ($failed) {
    Write-Host "Health check FAILED — see the FAIL lines above." -ForegroundColor Red
    exit 1
}
else {
    Write-Host "Health check PASSED." -ForegroundColor Green
    exit 0
}

#Requires -Version 5.1
<#
.SYNOPSIS
    Section 36 — full SQL Server backup with retention and verification.

.DESCRIPTION
    Takes a full backup of the NTNP Pricing database via T-SQL BACKUP DATABASE (using the SqlServer
    PowerShell module if present, falling back to sqlcmd.exe otherwise so this works on a bare SQL
    Server box with no extra modules installed), then runs RESTORE VERIFYONLY against the resulting
    file (Section 36 "Backup verification") before declaring success.

    Retention (Section 36 "Backup retention"): old backups are never deleted automatically by this
    script — see the -RetentionDays parameter, which only REPORTS backups older than that threshold
    so an administrator can review and remove them deliberately (Section 36: "Do not automatically
    delete unverified backups" — this script goes further and never auto-deletes ANY backup, verified
    or not, since an automated retention job is a separate, deliberate operational decision your
    organization should own).

.PARAMETER ServerInstance
    SQL Server instance, e.g. '.\SQLEXPRESS' or 'ntnp-sql-01'.

.PARAMETER DatabaseName
    Database to back up. Default: NTNP_Pricing.

.PARAMETER BackupRoot
    Folder to write .bak files into. Created if it doesn't exist.

.PARAMETER RetentionDays
    Backups older than this are only listed (not deleted) at the end of the run. Default: 30.

.EXAMPLE
    .\backup-database.ps1 -ServerInstance 'ntnp-sql-01' -DatabaseName NTNP_Pricing -BackupRoot D:\Backups\NTNP-Pricing
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ServerInstance,

    [string]$DatabaseName = 'NTNP_Pricing',

    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,

    [int]$RetentionDays = 30
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupFile = Join-Path $BackupRoot "$DatabaseName-Full-$timestamp.bak"

function Invoke-Sql {
    param([string]$Query)
    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer -ErrorAction Stop
        Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $Query -QueryTimeout 3600
    }
    else {
        sqlcmd -S $ServerInstance -Q $Query -b
        if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE running: $Query" }
    }
}

Write-Host "Backing up '$DatabaseName' on '$ServerInstance' to '$backupFile'..." -ForegroundColor Cyan
$backupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$backupFile'
WITH INIT, COMPRESSION, CHECKSUM, STATS = 10,
     NAME = N'$DatabaseName-Full-$timestamp';
"@
Invoke-Sql -Query $backupQuery

Write-Host "Verifying backup (RESTORE VERIFYONLY)..." -ForegroundColor Cyan
$verifyQuery = "RESTORE VERIFYONLY FROM DISK = N'$backupFile' WITH CHECKSUM;"
Invoke-Sql -Query $verifyQuery

Write-Host "Backup verified: $backupFile" -ForegroundColor Green

# Section 36 "Log backup guidance when applicable" — only meaningful under the FULL recovery model.
$recoveryModelQuery = "SELECT recovery_model_desc FROM sys.databases WHERE name = N'$DatabaseName';"
Write-Host ""
Write-Host "Recovery model check (log backups only apply under FULL — see docs/deployment.md 'Database backup'):" -ForegroundColor Cyan
Invoke-Sql -Query $recoveryModelQuery

$oldBackups = Get-ChildItem -Path $BackupRoot -Filter "$DatabaseName-Full-*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) }
if ($oldBackups) {
    Write-Host ""
    Write-Host "The following backups are older than $RetentionDays day(s). Review and remove them deliberately if no longer needed — this script never deletes backups automatically:" -ForegroundColor Yellow
    $oldBackups | ForEach-Object { Write-Host "  $($_.FullName) ($($_.LastWriteTime))" }
}

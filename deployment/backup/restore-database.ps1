#Requires -Version 5.1
<#
.SYNOPSIS
    Section 36 — restore/rollback from a full backup (also used for a periodic "restore test").

.DESCRIPTION
    Restores a .bak file taken by backup-database.ps1. By default restores to a NEW database name
    (DatabaseName + "_Restored") rather than overwriting the live database, so this script doubles
    safely as Section 36's "Restore test" — run it regularly against a scratch database to prove
    backups are actually restorable, without any risk to production data. Pass -Overwrite explicitly
    (an unmissable, separate switch) to perform a real rollback restore over the live database.

.PARAMETER ServerInstance
    SQL Server instance.

.PARAMETER BackupFile
    Full path to the .bak file to restore.

.PARAMETER DatabaseName
    Target database name for a restore test (default). Ignored (the backup's own original database
    name is used) when -Overwrite is passed.

.PARAMETER Overwrite
    Restores OVER the live database of the same name the backup was taken from (WITH REPLACE),
    first killing existing connections. This is a real rollback — use it only when you mean it.

.EXAMPLE
    # Restore test (safe, does not touch the live database):
    .\restore-database.ps1 -ServerInstance 'ntnp-sql-01' -BackupFile 'D:\Backups\NTNP-Pricing\NTNP_Pricing-Full-20260101-020000.bak'

    # Real rollback (Section 36 "Rollback"):
    .\restore-database.ps1 -ServerInstance 'ntnp-sql-01' -BackupFile 'D:\Backups\...\NTNP_Pricing-Full-20260101-020000.bak' -Overwrite
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ServerInstance,

    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$DatabaseName = 'NTNP_Pricing',

    [switch]$Overwrite
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $BackupFile)) {
    throw "Backup file not found: $BackupFile"
}

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

# The logical file names inside the backup determine where MOVE points the restored .mdf/.ldf — read
# them from the backup header rather than hardcoding, since they don't change between backups of the
# same database.
$fileListQuery = "RESTORE FILELISTONLY FROM DISK = N'$BackupFile';"
Write-Host "Reading backup file list..." -ForegroundColor Cyan
$fileList = if (Get-Module -ListAvailable -Name SqlServer) {
    Import-Module SqlServer -ErrorAction Stop
    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $fileListQuery
}
else {
    throw "Restore requires the SqlServer PowerShell module (Install-Module SqlServer) to read RESTORE FILELISTONLY output programmatically; sqlcmd-only mode is not supported for this script."
}

$dataFile = $fileList | Where-Object { $_.Type -eq 'D' } | Select-Object -First 1
$logFile = $fileList | Where-Object { $_.Type -eq 'L' } | Select-Object -First 1
$sqlDataDir = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA'  # adjust to your instance's default data path if different

if ($Overwrite) {
    Write-Host "OVERWRITE MODE: restoring over the LIVE database '$DatabaseName'. This is a real rollback." -ForegroundColor Red
    $confirmation = Read-Host "Type the database name ('$DatabaseName') to confirm"
    if ($confirmation -ne $DatabaseName) {
        Write-Host "Confirmation did not match — aborted." -ForegroundColor Yellow
        exit 1
    }

    Invoke-Sql -Query @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
"@

    $restoreQuery = @"
RESTORE DATABASE [$DatabaseName]
FROM DISK = N'$BackupFile'
WITH REPLACE, RECOVERY, STATS = 10,
     MOVE N'$($dataFile.LogicalName)' TO N'$sqlDataDir\$DatabaseName.mdf',
     MOVE N'$($logFile.LogicalName)' TO N'$sqlDataDir\$DatabaseName.ldf';
ALTER DATABASE [$DatabaseName] SET MULTI_USER;
"@
    Invoke-Sql -Query $restoreQuery
    Write-Host "Rollback restore of '$DatabaseName' complete." -ForegroundColor Green
}
else {
    $restoreTargetName = "$($DatabaseName)_Restored"
    Write-Host "Restore test: restoring into a NEW database '$restoreTargetName' (live '$DatabaseName' is untouched)." -ForegroundColor Cyan

    $restoreQuery = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$restoreTargetName')
BEGIN
    ALTER DATABASE [$restoreTargetName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$restoreTargetName];
END
RESTORE DATABASE [$restoreTargetName]
FROM DISK = N'$BackupFile'
WITH RECOVERY, STATS = 10,
     MOVE N'$($dataFile.LogicalName)' TO N'$sqlDataDir\$restoreTargetName.mdf',
     MOVE N'$($logFile.LogicalName)' TO N'$sqlDataDir\$restoreTargetName.ldf';
"@
    Invoke-Sql -Query $restoreQuery
    Write-Host "Restore test succeeded: database '$restoreTargetName' is restored and usable. Drop it when done reviewing:" -ForegroundColor Green
    Write-Host "  DROP DATABASE [$restoreTargetName];"
}

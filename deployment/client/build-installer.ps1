#Requires -Version 5.1
<#
.SYNOPSIS
    Section 34 — builds the self-contained win-x64 publish and the NTNP-Pricing-Setup-x64.msi
    installer. Run this on Windows (see ASSUMPTIONS.md §2/§11 — the WiX bind step cannot run in this
    repository's Linux development sandbox).

.DESCRIPTION
    Two steps, in order:
      1. `dotnet publish` the Desktop project as a self-contained win-x64 build (Section 34
         "No separate .NET runtime installation required").
      2. `dotnet build` the WiX installer project, which harvests step 1's publish output via its
         <Files Include="$(var.PublishDir)**"> element (see installer/NTNP.Pricing.Installer/Package.wxs)
         and produces NTNP-Pricing-Setup-x64.msi.

    Also copies the self-contained publish folder itself next to the .msi as a zip — Section 34's
    "Also produce a self-contained portable build for testing" (no installer needed to run it; just
    unzip and run NTNP.Pricing.Desktop.exe).

.PARAMETER Version
    Version stamped into both the publish assembly info and the MSI (Package/@Version in Package.wxs
    is edited by this script to match). Must be a plain three-part number, e.g. "1.2.0".

.PARAMETER SignPfx / SignPfxPassword / TimestampUrl
    Optional Authenticode signing (Section 34 "Ready for digital signing"). If omitted, the .msi and
    portable .exe are left unsigned — expect a SmartScreen warning on first run until they're signed.

.EXAMPLE
    .\build-installer.ps1 -Version 1.0.0
    .\build-installer.ps1 -Version 1.0.0 -SignPfx C:\certs\ntnp-codesign.pfx -SignPfxPassword (Read-Host -AsSecureString)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$SignPfx,
    [SecureString]$SignPfxPassword,
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$desktopProject = Join-Path $repoRoot 'src\NTNP.Pricing.Desktop\NTNP.Pricing.Desktop.csproj'
$installerProject = Join-Path $repoRoot 'installer\NTNP.Pricing.Installer\NTNP.Pricing.Installer.wixproj'
$publishDir = Join-Path $repoRoot 'src\NTNP.Pricing.Desktop\bin\Release\net10.0-windows\win-x64\publish'
$outputDir = Join-Path $repoRoot 'installer\NTNP.Pricing.Installer\bin\Release'

Write-Host "=== Step 1/3: publishing self-contained win-x64 (version $Version) ===" -ForegroundColor Cyan
dotnet publish $desktopProject `
    -c Release -r win-x64 `
    -p:SelfContained=true `
    -p:PublishReadyToRun=true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

Write-Host "=== Step 2/3: building the MSI ===" -ForegroundColor Cyan
$wxsPath = Join-Path $repoRoot 'installer\NTNP.Pricing.Installer\Package.wxs'
(Get-Content $wxsPath -Raw) -replace 'Version="[\d.]+"', "Version=`"$Version`"" | Set-Content $wxsPath -NoNewline

dotnet build $installerProject -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet build (WiX) failed with exit code $LASTEXITCODE." }

$msiPath = Join-Path $outputDir 'NTNP-Pricing-Setup-x64.msi'
if (-not (Test-Path $msiPath)) { throw "Expected MSI not found at $msiPath — check the WiX build output above." }

Write-Host "=== Step 3/3: portable zip + (optional) signing ===" -ForegroundColor Cyan
$portableZip = Join-Path $outputDir "NTNP-Pricing-Portable-$Version-x64.zip"
if (Test-Path $portableZip) { Remove-Item $portableZip }
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $portableZip

if ($SignPfx) {
    $signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if (-not $signtool) { throw "signtool.exe not found on PATH — install the Windows SDK, or omit -SignPfx to skip signing." }

    Write-Host "Signing MSI and the portable EXE..." -ForegroundColor Cyan
    & $signtool sign /f $SignPfx /p (ConvertFrom-SecureString $SignPfxPassword -AsPlainText) /fd SHA256 /tr $TimestampUrl /td SHA256 $msiPath
    if ($LASTEXITCODE -ne 0) { throw "signtool failed signing the MSI (exit $LASTEXITCODE)." }

    $portableExe = Join-Path $publishDir 'NTNP.Pricing.Desktop.exe'
    & $signtool sign /f $SignPfx /p (ConvertFrom-SecureString $SignPfxPassword -AsPlainText) /fd SHA256 /tr $TimestampUrl /td SHA256 $portableExe
    if ($LASTEXITCODE -ne 0) { throw "signtool failed signing the portable EXE (exit $LASTEXITCODE)." }
}
else {
    Write-Host "Skipping Authenticode signing (-SignPfx not supplied) — the installer will show a SmartScreen warning until it is signed." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  MSI:              $msiPath"
Write-Host "  Portable (zip):   $portableZip"

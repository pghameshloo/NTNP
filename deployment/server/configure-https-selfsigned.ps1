#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Section 35 "HTTPS configuration" — pilot/internal-only path: generates a self-signed certificate
    for the API's hostname and exports it as the .pfx Kestrel needs. See docs/deployment.md "HTTPS
    certificate" for the two other options (internal CA, public CA) that a real production deployment
    should prefer — a self-signed cert requires you to distribute trust to every client machine
    yourself (see the -PrintTrustInstructions output below).

.PARAMETER HostName
    DNS name the certificate is issued for — must match what desktop clients use as the server
    address (Server Connection Settings / the installer's ServerUrl), e.g. ntnp-pricing-server.

.PARAMETER OutputPfxPath
    Where to write the exported .pfx. Point Kestrel:Endpoints:Https:Certificate:Path at this file in
    appsettings.Production.json.

.PARAMETER PfxPassword
    Password protecting the exported .pfx (SecureString). Put the same value into
    Kestrel:Endpoints:Https:Certificate:Password.

.PARAMETER ValidYears
    Certificate lifetime. Default 5 years — plan a renewal before it expires (this script does not
    track expiry; re-run it to reissue).

.EXAMPLE
    .\configure-https-selfsigned.ps1 -HostName ntnp-pricing-server `
        -OutputPfxPath C:\NTNP\Pricing\Certs\ntnp-pricing.pfx `
        -PfxPassword (Read-Host -AsSecureString)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HostName,

    [Parameter(Mandatory = $true)]
    [string]$OutputPfxPath,

    [Parameter(Mandatory = $true)]
    [SecureString]$PfxPassword,

    [int]$ValidYears = 5
)

$ErrorActionPreference = 'Stop'

$outputDir = Split-Path $OutputPfxPath -Parent
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Generating self-signed certificate for '$HostName' (valid $ValidYears years)..." -ForegroundColor Cyan
$cert = New-SelfSignedCertificate `
    -DnsName $HostName `
    -CertStoreLocation Cert:\LocalMachine\My `
    -NotAfter (Get-Date).AddYears($ValidYears) `
    -KeyExportPolicy Exportable `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -Type SSLServerAuthentication

Write-Host "Exporting to $OutputPfxPath..." -ForegroundColor Cyan
Export-PfxCertificate -Cert $cert -FilePath $OutputPfxPath -Password $PfxPassword | Out-Null

# The cert this script just created is self-signed, so no client will trust it until it's added to
# each machine's Trusted Root store. Ship the .cer (public half, no private key) for that purpose.
$cerPath = [System.IO.Path]::ChangeExtension($OutputPfxPath, '.cer')
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  PFX (for Kestrel, keep private): $OutputPfxPath"
Write-Host "  CER (public half, distribute to every client machine): $cerPath"
Write-Host ""
Write-Host "On EACH desktop client machine, trust the certificate once:" -ForegroundColor Yellow
Write-Host "  Import-Certificate -FilePath '$cerPath' -CertStoreLocation Cert:\LocalMachine\Root"
Write-Host "(or distribute via Group Policy for many machines). Until trusted, the desktop client's"
Write-Host "HttpClient will reject the connection with a certificate-validation error."

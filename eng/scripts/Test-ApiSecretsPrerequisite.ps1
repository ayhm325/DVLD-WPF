#Requires -Version 5.1
<#
.SYNOPSIS
  Validates that Jwt__SecretKey from Azure DevOps variable group DVLD-Test-Secrets
  is configured and meets DVLD.Api's >= 32 UTF-8 byte requirement without
  logging secret values.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$secretCandidate = $env:JWT_SECRET_CANDIDATE
$unexpandedToken = '$' + '(Jwt__SecretKey)'

if ([string]::IsNullOrWhiteSpace($secretCandidate) -or
    $secretCandidate -eq $unexpandedToken) {
    Write-Host "##vso[task.logissue type=error]Missing prerequisite: Secret variable 'Jwt__SecretKey' from variable group 'DVLD-Test-Secrets' is not set."
    exit 1
}

$byteCount = [System.Text.Encoding]::UTF8.GetByteCount($secretCandidate)
if ($byteCount -lt 32) {
    Write-Host "##vso[task.logissue type=error]Invalid prerequisite: 'Jwt__SecretKey' in variable group 'DVLD-Test-Secrets' must be at least 32 UTF-8 bytes (required by DVLD.Api Program.cs)."
    exit 1
}

Write-Host 'JWT signing key prerequisite verified (>= 32 UTF-8 bytes).'
Write-Host '##vso[task.setvariable variable=ApiPrerequisitesReady]true'

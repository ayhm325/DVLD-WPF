#Requires -Version 5.1
<#
.SYNOPSIS
  Stages per-suite TRX files and deduplicated Cobertura XML coverage reports
  (excluding VSTest '\In\' attachment copies) into a clean directory structure,
  and verifies that every executed test suite produced its coverage report.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rawResultsRoot = $env:TEST_RESULTS_DIRECTORY
$publishedRoot = $env:PUBLISHED_REPORTS_DIRECTORY
$expectUnit = ($env:SOLUTION_BUILT -eq 'true')
$expectApi = (($env:SOLUTION_BUILT -eq 'true') -and ($env:SQL_SERVER_READY -eq 'true') -and ($env:API_PREREQUISITES_READY -eq 'true'))
$expectInfra = (($env:SOLUTION_BUILT -eq 'true') -and ($env:SQL_SERVER_READY -eq 'true'))

if ([string]::IsNullOrWhiteSpace($rawResultsRoot) -or [string]::IsNullOrWhiteSpace($publishedRoot)) {
    Write-Host '##vso[task.logissue type=error]TEST_RESULTS_DIRECTORY and PUBLISHED_REPORTS_DIRECTORY must be provided.'
    exit 1
}

$trxDestDir = Join-Path $publishedRoot 'trx'
$covDestDir = Join-Path $publishedRoot 'coverage'
New-Item -ItemType Directory -Path $trxDestDir -Force | Out-Null
New-Item -ItemType Directory -Path $covDestDir -Force | Out-Null
Write-Host '##vso[task.setvariable variable=ReportsDirectoryReady]true'

$suites = @(
    @{ Name = 'Application.UnitTests'; Expected = $expectUnit },
    @{ Name = 'API.IntegrationTests'; Expected = $expectApi },
    @{ Name = 'Infrastructure.IntegrationTests'; Expected = $expectInfra }
)

$stagedCoverageCount = 0
$missingExpectedReport = $false

foreach ($suite in $suites) {
    $suiteName = $suite.Name
    $suiteExpected = $suite.Expected
    $suiteDir = Join-Path $rawResultsRoot $suiteName

    if (-not (Test-Path $suiteDir)) {
        if ($suiteExpected) {
            Write-Host "##vso[task.logissue type=error]Expected test results directory for '$suiteName' was not found at '$suiteDir'."
            $missingExpectedReport = $true
        }
        continue
    }

    $trxFile = Join-Path $suiteDir "$suiteName.trx"
    if (Test-Path $trxFile) {
        Copy-Item -Path $trxFile -Destination (Join-Path $trxDestDir "$suiteName.trx") -Force
        Write-Host "Staged TRX report: $suiteName.trx"
    }
    elseif ($suiteExpected) {
        Write-Host "##vso[task.logissue type=error]Expected TRX file '$suiteName.trx' was not produced."
        $missingExpectedReport = $true
    }

    # Exclude duplicate VSTest run-attachment copies placed under '<run>/In/<host>/coverage.cobertura.xml'
    $coberturaFiles = @(
        Get-ChildItem -Path $suiteDir -Recurse -Filter 'coverage.cobertura.xml' -File |
            Where-Object { $_.FullName -notmatch '[\\/]In[\\/]' } |
            Sort-Object LastWriteTimeUtc -Descending
    )

    if ($coberturaFiles.Count -gt 0) {
        $canonicalCoverage = $coberturaFiles[0]
        if ($canonicalCoverage.Length -le 0) {
            Write-Host "##vso[task.logissue type=error]Cobertura coverage report for '$suiteName' is empty (0 bytes)."
            $missingExpectedReport = $true
        }
        else {
            $destCoveragePath = Join-Path $covDestDir "$suiteName.cobertura.xml"
            Copy-Item -Path $canonicalCoverage.FullName -Destination $destCoveragePath -Force
            $stagedCoverageCount++
            Write-Host "Staged deduplicated Cobertura report: $suiteName.cobertura.xml ($($canonicalCoverage.Length) bytes)"
        }
    }
    elseif ($suiteExpected) {
        Write-Host "##vso[task.logissue type=error]Expected Cobertura coverage report was not generated for executed suite '$suiteName'."
        $missingExpectedReport = $true
    }
}

if ($stagedCoverageCount -gt 0) {
    Write-Host "Total deduplicated suite Cobertura files staged: $stagedCoverageCount"
    Write-Host '##vso[task.setvariable variable=CoverageReportsStaged]true'
}
else {
    Write-Host '##vso[task.setvariable variable=CoverageReportsStaged]false'
}

if ($missingExpectedReport) {
    exit 1
}

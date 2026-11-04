#Requires -Version 5.1
<#
.SYNOPSIS
  Validates DVLD_TEST_CONNECTION safety, initializes a run-specific SQL Server
  LocalDB instance when configured for LocalDB, records verifiable run ownership,
  and verifies SQL connectivity before integration tests start.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$connectionString = $env:DVLD_TEST_CONNECTION
$expectedOwnedInstance = $env:EXPECTED_LOCALDB_INSTANCE
$buildId = if ([string]::IsNullOrWhiteSpace($env:BUILD_BUILDID)) { 'local' } else { $env:BUILD_BUILDID }
$jobAttempt = if ([string]::IsNullOrWhiteSpace($env:SYSTEM_JOBATTEMPT)) { '1' } else { $env:SYSTEM_JOBATTEMPT }

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host '##vso[task.logissue type=error]Database prerequisite failure: DVLD_TEST_CONNECTION is empty.'
    exit 1
}

try {
    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($connectionString)
}
catch {
    Write-Host '##vso[task.logissue type=error]Database prerequisite failure: DVLD_TEST_CONNECTION is not a valid SQL Server connection string.'
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($builder.InitialCatalog) -and
    -not $builder.InitialCatalog.Equals('master', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "##vso[task.logissue type=error]Database safety violation: DVLD_TEST_CONNECTION specifies InitialCatalog '$($builder.InitialCatalog)'. Integration fixtures require a server-level connection (empty InitialCatalog or 'master') so isolated DVLD_IntegrationTests_* databases are used."
    exit 1
}

$probeDataSource = $builder.DataSource
$createdFreshOwnedInstance = $false
$ownedInstanceName = ''

if ($probeDataSource -match '^\(localdb\)\\(.+)$') {
    $instanceName = $Matches[1]
    $localDbCmd = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
    if ($null -eq $localDbCmd) {
        Write-Host "##vso[task.logissue type=error]Database infrastructure failure: SqlLocalDB.exe is not installed on this Windows agent; cannot initialize LocalDB instance '$instanceName'."
        exit 1
    }

    $null = & SqlLocalDB.exe info $instanceName 2>&1
    $instanceAlreadyExisted = ($LASTEXITCODE -eq 0)

    if (-not $instanceAlreadyExisted) {
        Write-Host "Creating SQL Server LocalDB instance '$instanceName'..."
        & SqlLocalDB.exe create $instanceName | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Failed to create SQL Server LocalDB instance '$instanceName'."
            exit 1
        }

        if (-not [string]::IsNullOrWhiteSpace($expectedOwnedInstance) -and
            $instanceName.Equals($expectedOwnedInstance, [System.StringComparison]::OrdinalIgnoreCase)) {
            $createdFreshOwnedInstance = $true
            $ownedInstanceName = $instanceName
        }
    }
    else {
        Write-Host "SQL Server LocalDB instance '$instanceName' already exists on host; treating as shared instance."
    }

    Write-Host "Starting SQL Server LocalDB instance '$instanceName'..."
    & SqlLocalDB.exe start $instanceName | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Failed to start SQL Server LocalDB instance '$instanceName'."
        exit 1
    }

    $startedInfo = & SqlLocalDB.exe info $instanceName 2>&1
    $pipeLine = $startedInfo | Where-Object { $_ -match 'Instance pipe name:\s*(.+)$' } | Select-Object -First 1
    if ($pipeLine -match 'Instance pipe name:\s*(.+)$') {
        $probeDataSource = $Matches[1].Trim()
    }
}

$probeBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($connectionString)
$probeBuilder.DataSource = $probeDataSource
$probeBuilder.InitialCatalog = 'master'

$runOwnershipToken = ''

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($probeBuilder.ConnectionString)
    try {
        $connection.Open()

        $probeCmd = $connection.CreateCommand()
        $probeCmd.CommandTimeout = 30
        $probeCmd.CommandText = "SELECT CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128)), CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128));"
        $reader = $probeCmd.ExecuteReader()
        if ($reader.Read()) {
            Write-Host "SQL Server test engine verified: Version=$($reader.GetString(0)), Edition=$($reader.GetString(1))"
        }
        $reader.Close()

        if ($createdFreshOwnedInstance) {
            $runOwnershipToken = [guid]::NewGuid().ToString('N')
            $ownershipCmd = $connection.CreateCommand()
            $ownershipCmd.CommandTimeout = 30
            $ownershipCmd.CommandText = @"
IF OBJECT_ID(N'dbo.__DVLD_CI_RunOwnership', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__DVLD_CI_RunOwnership (
        RunToken NVARCHAR(64) NOT NULL PRIMARY KEY,
        BuildId NVARCHAR(64) NOT NULL,
        JobAttempt NVARCHAR(32) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
END;

INSERT INTO dbo.__DVLD_CI_RunOwnership (RunToken, BuildId, JobAttempt, CreatedAtUtc)
VALUES (@runToken, @buildId, @jobAttempt, SYSUTCDATETIME());
"@
            [void]$ownershipCmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter('@runToken', $runOwnershipToken)))
            [void]$ownershipCmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter('@buildId', $buildId)))
            [void]$ownershipCmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter('@jobAttempt', $jobAttempt)))
            [void]$ownershipCmd.ExecuteNonQuery()

            Write-Host "Registered verifiable run ownership marker on dedicated LocalDB instance '$ownedInstanceName' (BuildId=$buildId, JobAttempt=$jobAttempt)."
        }
    }
    finally {
        $connection.Dispose()
    }
}
catch {
    Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Unable to verify or initialize 'master' on the SQL Server test engine ($($_.Exception.Message))."
    exit 1
}

if ($createdFreshOwnedInstance -and -not [string]::IsNullOrWhiteSpace($runOwnershipToken)) {
    Write-Host '##vso[task.setvariable variable=SqlExclusiveInstanceOwned]true'
    Write-Host "##vso[task.setvariable variable=SqlOwnedLocalDbInstance]$ownedInstanceName"
    Write-Host "##vso[task.setvariable variable=SqlRunOwnershipToken]$runOwnershipToken"
}
else {
    Write-Host '##vso[task.setvariable variable=SqlExclusiveInstanceOwned]false'
}

Write-Host "##vso[task.setvariable variable=SqlProbeDataSource]$probeDataSource"
Write-Host '##vso[task.setvariable variable=SqlServerReady]true'

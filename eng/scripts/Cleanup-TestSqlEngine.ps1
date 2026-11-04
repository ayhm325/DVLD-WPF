#Requires -Version 5.1
<#
.SYNOPSIS
  Cleans up residual DVLD_IntegrationTests_* databases and tears down the
  run-specific LocalDB instance ONLY when verifiable exclusive ownership of the
  SQL instance by the current pipeline execution is positively proven.
  Strictly protects 'DVLDf' and never deletes databases on shared or unverified
  SQL Server instances.
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$SqlLocalDbExecutable = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$exclusiveOwned = $env:SQL_EXCLUSIVE_INSTANCE_OWNED
$ownedInstance = $env:SQL_OWNED_LOCALDB_INSTANCE
$ownershipToken = $env:SQL_RUN_OWNERSHIP_TOKEN
$connectionString = $env:DVLD_TEST_CONNECTION
$probeDataSource = $env:SQL_PROBE_DATA_SOURCE

if ([string]::IsNullOrWhiteSpace($SqlLocalDbExecutable)) {
    $SqlLocalDbExecutable = if (-not [string]::IsNullOrWhiteSpace($env:DVLD_SQLLOCALDB_EXE)) {
        $env:DVLD_SQLLOCALDB_EXE
    }
    else {
        'SqlLocalDB.exe'
    }
}

$unexpandedOwned = '$' + '(SqlExclusiveInstanceOwned)'
$unexpandedInstance = '$' + '(SqlOwnedLocalDbInstance)'
$unexpandedToken = '$' + '(SqlRunOwnershipToken)'
$unexpandedProbe = '$' + '(SqlProbeDataSource)'

if ($exclusiveOwned -ne 'true' -or
    $exclusiveOwned -eq $unexpandedOwned -or
    [string]::IsNullOrWhiteSpace($ownedInstance) -or
    $ownedInstance -eq $unexpandedInstance -or
    $ownedInstance -notmatch '^DVLD_CI_[A-Za-z0-9_]+$' -or
    [string]::IsNullOrWhiteSpace($ownershipToken) -or
    $ownershipToken -eq $unexpandedToken -or
    $ownershipToken -notmatch '^[0-9a-fA-F]{32}$') {
    Write-Host 'Skipping post-run residual database deletion: SQL Server instance is shared or exclusive run ownership was not established. Per-test fixture DisposeAsync cleanup remains in effect.'
    exit 0
}

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host 'Skipping post-run residual database deletion: DVLD_TEST_CONNECTION is empty.'
    exit 0
}

try {
    # Explicit CLR constructor + .psbase property access avoids PowerShell 5.1's
    # IDictionaryAdapter indexing bug ("Keyword not supported: 'DataSource'").
    $cleanupBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    $configuredCatalog = [string]$cleanupBuilder.psbase.InitialCatalog

    if ($configuredCatalog.Equals('DVLDf', [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host '##vso[task.logissue type=warning]CRITICAL DATABASE PROTECTION: Refusing cleanup because DVLD_TEST_CONNECTION references protected database ''DVLDf''.'
        exit 0
    }

    $configuredDataSource = [string]$cleanupBuilder.psbase.DataSource
    $expectedDataSource = "(localdb)\$ownedInstance"
    if (-not $configuredDataSource.Equals($expectedDataSource, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Skipping post-run residual database deletion: Connection DataSource '$configuredDataSource' does not match owned LocalDB instance '$expectedDataSource'."
        exit 0
    }

    if (-not [string]::IsNullOrWhiteSpace($probeDataSource) -and $probeDataSource -ne $unexpandedProbe) {
        $cleanupBuilder.psbase.DataSource = $probeDataSource
    }
    $cleanupBuilder.psbase.InitialCatalog = 'master'
    $cleanupConnectionString = [string]$cleanupBuilder.psbase.ConnectionString

    $ownershipVerified = $false
    $connection = [System.Data.SqlClient.SqlConnection]::new($cleanupConnectionString)
    try {
        $connection.Open()

        $dbCheckCmd = $connection.CreateCommand()
        $dbCheckCmd.CommandTimeout = 30
        $dbCheckCmd.CommandText = 'SELECT DB_NAME();'
        $activeDatabase = [string]$dbCheckCmd.ExecuteScalar()
        if (-not $activeDatabase.Equals('master', [System.StringComparison]::OrdinalIgnoreCase)) {
            Write-Host "Skipping post-run residual database deletion: Expected 'master' connection, but connected to '$activeDatabase'."
            exit 0
        }

        $verifyCmd = $connection.CreateCommand()
        $verifyCmd.CommandTimeout = 30
        $verifyCmd.CommandText = @"
SELECT COUNT(1)
FROM sys.tables AS t
WHERE t.name = N'__DVLD_CI_RunOwnership'
  AND SCHEMA_NAME(t.schema_id) = N'dbo';
"@
        $hasOwnershipTable = ([int]$verifyCmd.ExecuteScalar() -gt 0)
        if ($hasOwnershipTable) {
            $tokenCmd = $connection.CreateCommand()
            $tokenCmd.CommandTimeout = 30
            $tokenCmd.CommandText = 'SELECT COUNT(1) FROM dbo.__DVLD_CI_RunOwnership WHERE RunToken = @runToken;'
            [void]$tokenCmd.Parameters.Add([System.Data.SqlClient.SqlParameter]::new('@runToken', $ownershipToken))
            $ownershipVerified = ([int]$tokenCmd.ExecuteScalar() -eq 1)
        }

        if (-not $ownershipVerified) {
            Write-Host "Skipping post-run residual database deletion: Ownership token verification failed on instance '$ownedInstance'. No databases or instances were deleted."
            exit 0
        }

        $listCmd = $connection.CreateCommand()
        $listCmd.CommandTimeout = 60
        $listCmd.CommandText = @"
SELECT name
FROM sys.databases
WHERE name LIKE N'DVLD[_]IntegrationTests[_]%'
  AND name <> N'DVLDf'
  AND database_id > 4;
"@
        $orphanDbs = New-Object System.Collections.Generic.List[string]
        $reader = $listCmd.ExecuteReader()
        try {
            while ($reader.Read()) {
                $orphanDbs.Add($reader.GetString(0))
            }
        }
        finally {
            $reader.Close()
        }

        foreach ($dbName in $orphanDbs) {
            if ($dbName.Equals('DVLDf', [System.StringComparison]::OrdinalIgnoreCase)) {
                Write-Host 'CRITICAL DATABASE PROTECTION: Skipping protected database DVLDf.'
                continue
            }

            if ($dbName.StartsWith('DVLD_IntegrationTests_', [System.StringComparison]::OrdinalIgnoreCase)) {
                $escapedIdentifier = $dbName.Replace(']', ']]')
                $escapedLiteral = $dbName.Replace("'", "''")
                $dropCmd = $connection.CreateCommand()
                $dropCmd.CommandTimeout = 120
                $dropCmd.CommandText = @"
IF DB_ID(N'$escapedLiteral') IS NOT NULL AND N'$escapedLiteral' <> N'DVLDf'
BEGIN
    ALTER DATABASE [$escapedIdentifier] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$escapedIdentifier];
END
"@
                [void]$dropCmd.ExecuteNonQuery()
                Write-Host "Dropped residual ephemeral test database owned by this run: $dbName"
            }
        }

        if ($orphanDbs.Count -eq 0) {
            Write-Host "Verified 0 residual DVLD_IntegrationTests_* databases on owned instance '$ownedInstance'."
        }
    }
    finally {
        $connection.Dispose()
        [System.Data.SqlClient.SqlConnection]::ClearAllPools()
    }

    $localDbCmd = Get-Command $SqlLocalDbExecutable -ErrorAction SilentlyContinue
    if ($null -ne $localDbCmd) {
        $localDbExePath = $localDbCmd.Source
        Write-Host "Stopping and deleting ephemeral run-owned LocalDB instance '$ownedInstance'..."

        $stopOutput = @(& $localDbExePath stop $ownedInstance -k 2>&1)
        $stopExit = $LASTEXITCODE
        foreach ($line in $stopOutput) {
            if ($null -ne $line -and -not [string]::IsNullOrWhiteSpace($line.ToString())) {
                Write-Host "  [SqlLocalDB stop] $($line.ToString())"
            }
        }
        if ($stopExit -ne 0 -or ($stopOutput -join "`n") -match '(?i)failed because of the following error') {
            Write-Host "##vso[task.logissue type=warning]Stopping LocalDB instance '$ownedInstance' reported a warning (ExitCode=$stopExit)."
        }

        $deleteOutput = @(& $localDbExePath delete $ownedInstance 2>&1)
        $deleteExit = $LASTEXITCODE
        foreach ($line in $deleteOutput) {
            if ($null -ne $line -and -not [string]::IsNullOrWhiteSpace($line.ToString())) {
                Write-Host "  [SqlLocalDB delete] $($line.ToString())"
            }
        }
        if ($deleteExit -ne 0 -or ($deleteOutput -join "`n") -match '(?i)failed because of the following error') {
            Write-Host "##vso[task.logissue type=warning]Deleting LocalDB instance '$ownedInstance' reported a warning (ExitCode=$deleteExit)."
        }
    }
}
catch {
    Write-Host "##vso[task.logissue type=warning]Post-test cleanup on owned LocalDB instance '$ownedInstance' encountered a non-fatal warning: $($_.Exception.Message)"
}

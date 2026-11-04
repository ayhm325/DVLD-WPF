#Requires -Version 5.1
<#
.SYNOPSIS
  Cleans up residual DVLD_IntegrationTests_* databases and tears down the
  run-specific LocalDB instance ONLY when verifiable exclusive ownership of the
  SQL instance by the current pipeline execution is proven.
  Never deletes databases on shared or unverified SQL Server instances.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$exclusiveOwned = $env:SQL_EXCLUSIVE_INSTANCE_OWNED
$ownedInstance = $env:SQL_OWNED_LOCALDB_INSTANCE
$ownershipToken = $env:SQL_RUN_OWNERSHIP_TOKEN
$connectionString = $env:DVLD_TEST_CONNECTION
$probeDataSource = $env:SQL_PROBE_DATA_SOURCE

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

try {
    $cleanupBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($connectionString)
    $expectedDataSource = "(localdb)\$ownedInstance"
    if (-not $cleanupBuilder.DataSource.Equals($expectedDataSource, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Skipping post-run residual database deletion: Connection DataSource '$($cleanupBuilder.DataSource)' does not match owned LocalDB instance '$expectedDataSource'."
        exit 0
    }

    if (-not [string]::IsNullOrWhiteSpace($probeDataSource) -and $probeDataSource -ne $unexpandedProbe) {
        $cleanupBuilder.DataSource = $probeDataSource
    }
    $cleanupBuilder.InitialCatalog = 'master'

    $ownershipVerified = $false
    $connection = New-Object System.Data.SqlClient.SqlConnection($cleanupBuilder.ConnectionString)
    try {
        $connection.Open()

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
            [void]$tokenCmd.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter('@runToken', $ownershipToken)))
            $ownershipVerified = ([int]$tokenCmd.ExecuteScalar() -eq 1)
        }

        if (-not $ownershipVerified) {
            Write-Host "Skipping post-run residual database deletion: Ownership token verification failed on instance '$ownedInstance'. No databases were deleted."
            exit 0
        }

        $listCmd = $connection.CreateCommand()
        $listCmd.CommandTimeout = 60
        $listCmd.CommandText = @"
SELECT name
FROM sys.databases
WHERE name LIKE N'DVLD[_]IntegrationTests[_]%'
  AND database_id > 4;
"@
        $orphanDbs = New-Object System.Collections.Generic.List[string]
        $reader = $listCmd.ExecuteReader()
        while ($reader.Read()) {
            $orphanDbs.Add($reader.GetString(0))
        }
        $reader.Close()

        foreach ($dbName in $orphanDbs) {
            if ($dbName.StartsWith('DVLD_IntegrationTests_', [System.StringComparison]::OrdinalIgnoreCase)) {
                $escapedIdentifier = $dbName.Replace(']', ']]')
                $escapedLiteral = $dbName.Replace("'", "''")
                $dropCmd = $connection.CreateCommand()
                $dropCmd.CommandTimeout = 120
                $dropCmd.CommandText = @"
IF DB_ID(N'$escapedLiteral') IS NOT NULL
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

    $localDbCmd = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
    if ($null -ne $localDbCmd) {
        Write-Host "Stopping and deleting ephemeral run-owned LocalDB instance '$ownedInstance'..."
        & SqlLocalDB.exe stop $ownedInstance -k | Out-Host
        & SqlLocalDB.exe delete $ownedInstance | Out-Host
    }
}
catch {
    Write-Host "##vso[task.logissue type=warning]Post-test cleanup on owned LocalDB instance '$ownedInstance' encountered a non-fatal warning: $($_.Exception.Message)"
}

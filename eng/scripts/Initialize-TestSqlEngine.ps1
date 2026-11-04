#Requires -Version 5.1
<#
.SYNOPSIS
  Validates DVLD_TEST_CONNECTION safety (including strict DVLDf protection),
  reliably initializes a run-specific SQL Server LocalDB instance when configured
  for LocalDB, records verifiable run ownership in master, and verifies SQL
  connectivity before integration tests execute.
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$SqlLocalDbExecutable = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-SqlLocalDbCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $rawOutput = @(& $ExecutablePath @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $lines = New-Object System.Collections.Generic.List[string]

    foreach ($item in $rawOutput) {
        if ($null -ne $item) {
            $lines.Add($item.ToString())
        }
    }

    return [PSCustomObject]@{
        ExitCode = $exitCode
        Lines    = $lines.ToArray()
        RawText  = ($lines -join [Environment]::NewLine)
    }
}

function Test-LocalDbCommandFailed {
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$CommandResult
    )

    if ($CommandResult.ExitCode -ne 0) {
        return $true
    }

    foreach ($line in $CommandResult.Lines) {
        if ($line -match '(?i)(failed because of the following error|unexpected error occurred|invalid command-line|internal error occurred|access is denied|doesn''t exist|does not exist)') {
            return $true
        }
    }

    return $false
}

function Test-LocalDbMissingInstanceMessage {
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$CommandResult
    )

    $hasMissingMessage = $false
    foreach ($line in $CommandResult.Lines) {
        if ($line -match '(?i)unexpected error occurred') {
            return $false
        }

        if ($line -match '(?i)(specified LocalDB instance does not exist|LocalDB instance .* doesn''t exist)') {
            $hasMissingMessage = $true
        }
    }

    return $hasMissingMessage
}

function Get-LocalDbInstanceStatus {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,

        [Parameter(Mandatory = $true)]
        [string]$InstanceName
    )

    $listResult = Invoke-SqlLocalDbCommand -ExecutablePath $ExecutablePath -Arguments @('info')
    if (Test-LocalDbCommandFailed -CommandResult $listResult) {
        $diag = ($listResult.Lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | '
        throw "SqlLocalDB.exe instance enumeration ('SqlLocalDB.exe info') failed (ExitCode=$($listResult.ExitCode)): $diag"
    }

    $listedInstances = @(
        $listResult.Lines |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )

    $isListed = $false
    foreach ($entry in $listedInstances) {
        if ($entry.Equals($InstanceName, [System.StringComparison]::OrdinalIgnoreCase)) {
            $isListed = $true
            break
        }
    }

    $detailResult = Invoke-SqlLocalDbCommand -ExecutablePath $ExecutablePath -Arguments @('info', $InstanceName)

    if (-not $isListed -and (Test-LocalDbMissingInstanceMessage -CommandResult $detailResult)) {
        return [PSCustomObject]@{
            Exists              = $false
            ListedInEnumeration = $false
            Name                = $InstanceName
            Version             = ''
            Owner               = ''
            State               = ''
            PipeName            = ''
        }
    }

    if (Test-LocalDbCommandFailed -CommandResult $detailResult) {
        $diag = ($detailResult.Lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | '
        throw "SqlLocalDB.exe status inspection for instance '$InstanceName' failed (Listed=$isListed, ExitCode=$($detailResult.ExitCode)): $diag"
    }

    $parsedName = ''
    $parsedVersion = ''
    $parsedOwner = ''
    $parsedState = ''
    $parsedPipe = ''

    foreach ($line in $detailResult.Lines) {
        if ($line -match '^\s*Name:\s*(.+)$') {
            $parsedName = $Matches[1].Trim()
        }
        elseif ($line -match '^\s*Version:\s*(.+)$') {
            $parsedVersion = $Matches[1].Trim()
        }
        elseif ($line -match '^\s*Owner:\s*(.+)$') {
            $parsedOwner = $Matches[1].Trim()
        }
        elseif ($line -match '^\s*State:\s*(.+)$') {
            $parsedState = $Matches[1].Trim()
        }
        elseif ($line -match '^\s*Instance pipe name:\s*(.*)$') {
            $parsedPipe = $Matches[1].Trim()
        }
    }

    if ([string]::IsNullOrWhiteSpace($parsedName) -or
        -not $parsedName.Equals($InstanceName, [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::IsNullOrWhiteSpace($parsedState)) {
        $diag = ($detailResult.Lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | '
        throw "SqlLocalDB.exe returned unstructured or mismatched metadata for instance '$InstanceName': $diag"
    }

    return [PSCustomObject]@{
        Exists              = $true
        ListedInEnumeration = $isListed
        Name                = $parsedName
        Version             = $parsedVersion
        Owner               = $parsedOwner
        State               = $parsedState
        PipeName            = $parsedPipe
    }
}

$connectionString = $env:DVLD_TEST_CONNECTION
$expectedOwnedInstance = $env:EXPECTED_LOCALDB_INSTANCE
$buildId = if ([string]::IsNullOrWhiteSpace($env:BUILD_BUILDID)) { 'local' } else { $env:BUILD_BUILDID }
$jobAttempt = if ([string]::IsNullOrWhiteSpace($env:SYSTEM_JOBATTEMPT)) { '1' } else { $env:SYSTEM_JOBATTEMPT }

if ([string]::IsNullOrWhiteSpace($SqlLocalDbExecutable)) {
    $SqlLocalDbExecutable = if (-not [string]::IsNullOrWhiteSpace($env:DVLD_SQLLOCALDB_EXE)) {
        $env:DVLD_SQLLOCALDB_EXE
    }
    else {
        'SqlLocalDB.exe'
    }
}

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host '##vso[task.logissue type=error]Database prerequisite failure: DVLD_TEST_CONNECTION is empty.'
    exit 1
}

try {
    # Explicit CLR constructor + .psbase property access avoids PowerShell 5.1's
    # IDictionaryAdapter indexing bug ("Keyword not supported: 'DataSource'").
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
}
catch {
    Write-Host '##vso[task.logissue type=error]Database prerequisite failure: DVLD_TEST_CONNECTION is not a valid SQL Server connection string.'
    exit 1
}

$configuredCatalog = [string]$builder.psbase.InitialCatalog
$configuredDataSource = [string]$builder.psbase.DataSource

if ($configuredCatalog.Equals('DVLDf', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host '##vso[task.logissue type=error]CRITICAL DATABASE PROTECTION VIOLATION: DVLD_TEST_CONNECTION targets protected database ''DVLDf''. Execution aborted immediately.'
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($configuredCatalog) -and
    -not $configuredCatalog.Equals('master', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "##vso[task.logissue type=error]Database safety violation: DVLD_TEST_CONNECTION specifies InitialCatalog '$configuredCatalog'. Integration fixtures require a server-level base connection ('master' or empty InitialCatalog) so isolated DVLD_IntegrationTests_* databases are used."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($configuredDataSource)) {
    Write-Host '##vso[task.logissue type=error]Database prerequisite failure: DVLD_TEST_CONNECTION does not specify a SQL Server Data Source.'
    exit 1
}

$probeDataSource = $configuredDataSource
$createdFreshOwnedInstance = $false
$ownedInstanceName = ''

if ($configuredDataSource -match '^\(localdb\)\\(.+)$') {
    $instanceName = $Matches[1].Trim()
    if ([string]::IsNullOrWhiteSpace($instanceName)) {
        Write-Host '##vso[task.logissue type=error]Database prerequisite failure: LocalDB instance name in DVLD_TEST_CONNECTION is empty.'
        exit 1
    }

    $localDbCmd = Get-Command $SqlLocalDbExecutable -ErrorAction SilentlyContinue
    if ($null -eq $localDbCmd) {
        Write-Host "##vso[task.logissue type=error]Database infrastructure failure: '$SqlLocalDbExecutable' is not installed or not found on PATH; cannot initialize LocalDB instance '$instanceName'."
        exit 1
    }

    $localDbExePath = $localDbCmd.Source
    $currentWindowsUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    Write-Host "Inspecting SQL Server LocalDB instance '$instanceName' under Windows identity '$currentWindowsUser' (Executable='$localDbExePath')..."

    try {
        $initialStatus = Get-LocalDbInstanceStatus -ExecutablePath $localDbExePath -InstanceName $instanceName
    }
    catch {
        Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Unable to query LocalDB status for '$instanceName' ($($_.Exception.Message))."
        exit 1
    }

    if (-not $initialStatus.Exists) {
        Write-Host "SQL Server LocalDB instance '$instanceName' does not exist for '$currentWindowsUser'. Creating dedicated instance..."
        $createResult = Invoke-SqlLocalDbCommand -ExecutablePath $localDbExePath -Arguments @('create', $instanceName)
        foreach ($line in $createResult.Lines) {
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                Write-Host "  [SqlLocalDB create] $line"
            }
        }

        if (Test-LocalDbCommandFailed -CommandResult $createResult) {
            Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Failed to create SQL Server LocalDB instance '$instanceName' (ExitCode=$($createResult.ExitCode))."
            exit 1
        }

        try {
            $postCreateStatus = Get-LocalDbInstanceStatus -ExecutablePath $localDbExePath -InstanceName $instanceName
        }
        catch {
            Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Post-create verification failed for LocalDB instance '$instanceName' ($($_.Exception.Message))."
            exit 1
        }

        if (-not $postCreateStatus.Exists) {
            Write-Host "##vso[task.logissue type=error]Database infrastructure failure: 'SqlLocalDB.exe create $instanceName' reported completion, but instance '$instanceName' still does not exist."
            exit 1
        }

        if (-not [string]::IsNullOrWhiteSpace($expectedOwnedInstance) -and
            $instanceName.Equals($expectedOwnedInstance, [System.StringComparison]::OrdinalIgnoreCase)) {
            $createdFreshOwnedInstance = $true
            $ownedInstanceName = $instanceName
        }

        $currentStatus = $postCreateStatus
    }
    else {
        Write-Host "SQL Server LocalDB instance '$instanceName' already exists (State=$($initialStatus.State), Version=$($initialStatus.Version)); treating as shared pre-existing instance."
        $currentStatus = $initialStatus
    }

    if (-not $currentStatus.State.Equals('Running', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::IsNullOrWhiteSpace($currentStatus.PipeName)) {
        Write-Host "Starting SQL Server LocalDB instance '$instanceName'..."
        $startResult = Invoke-SqlLocalDbCommand -ExecutablePath $localDbExePath -Arguments @('start', $instanceName)
        foreach ($line in $startResult.Lines) {
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                Write-Host "  [SqlLocalDB start] $line"
            }
        }

        if (Test-LocalDbCommandFailed -CommandResult $startResult) {
            Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Failed to start SQL Server LocalDB instance '$instanceName' (ExitCode=$($startResult.ExitCode))."
            exit 1
        }
    }

    try {
        $runningStatus = Get-LocalDbInstanceStatus -ExecutablePath $localDbExePath -InstanceName $instanceName
    }
    catch {
        Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Unable to inspect LocalDB instance '$instanceName' after start ($($_.Exception.Message))."
        exit 1
    }

    if (-not $runningStatus.Exists -or
        -not $runningStatus.State.Equals('Running', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::IsNullOrWhiteSpace($runningStatus.PipeName) -or
        $runningStatus.PipeName -notmatch '^np:\\\\\.\\pipe\\LOCALDB#') {
        Write-Host "##vso[task.logissue type=error]Database infrastructure failure: LocalDB instance '$instanceName' is not healthy after start (Exists=$($runningStatus.Exists), State='$($runningStatus.State)', PipeDefined=$(-not [string]::IsNullOrWhiteSpace($runningStatus.PipeName)))."
        exit 1
    }

    $probeDataSource = $runningStatus.PipeName
    Write-Host "Verified LocalDB instance '$instanceName' is Running (Version=$($runningStatus.Version)) with active named pipe endpoint."
}

try {
    $probeBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    $probeBuilder.psbase.DataSource = $probeDataSource
    $probeBuilder.psbase.InitialCatalog = 'master'
    $probeConnectionString = [string]$probeBuilder.psbase.ConnectionString
}
catch {
    Write-Host "##vso[task.logissue type=error]Database infrastructure failure: Failed to construct master probe SQL connection string ($($_.Exception.Message))."
    exit 1
}

$runOwnershipToken = ''

try {
    $connection = [System.Data.SqlClient.SqlConnection]::new($probeConnectionString)
    try {
        $connection.Open()

        $probeCmd = $connection.CreateCommand()
        $probeCmd.CommandTimeout = 30
        $probeCmd.CommandText = "SELECT DB_NAME(), CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128)), CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128));"
        $reader = $probeCmd.ExecuteReader()
        try {
            if (-not $reader.Read()) {
                throw 'SQL Server probe query returned no rows.'
            }

            $connectedDb = $reader.GetString(0)
            $productVersion = $reader.GetString(1)
            $edition = $reader.GetString(2)
        }
        finally {
            $reader.Close()
        }

        if (-not $connectedDb.Equals('master', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Expected probe connection to open 'master', but connected to '$connectedDb'."
        }

        Write-Host "SQL Server test engine verified on '$connectedDb': Version=$productVersion, Edition=$edition"

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
            [void]$ownershipCmd.Parameters.Add([System.Data.SqlClient.SqlParameter]::new('@runToken', $runOwnershipToken))
            [void]$ownershipCmd.Parameters.Add([System.Data.SqlClient.SqlParameter]::new('@buildId', $buildId))
            [void]$ownershipCmd.Parameters.Add([System.Data.SqlClient.SqlParameter]::new('@jobAttempt', $jobAttempt))
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

#Requires -Version 5.1
<#
.SYNOPSIS
  Local developer bootstrap for the DVLD SQL integration-test environment.

.DESCRIPTION
  Starts the existing SQL Server LocalDB instance (default: MSSQLLocalDB) if it
  is stopped, discovers the ACTUAL instance identity from the running engine on
  every invocation, configures the process-scoped test variables

    DVLD_TEST_CONNECTION         (server-level, Initial Catalog=master, Windows Auth)
    DVLD_TEST_EXPECTED_INSTANCE  ("<SERVERPROPERTY('ServerName')>|<@@SERVICENAME>")

  and launches Visual Studio with the solution so that Test Explorer test hosts
  inherit them.

  Safety:
    * Variables are set for the current process only (inherited by the launched
      Visual Studio). Persistent User/Machine environment variables are never
      written.
    * The identity is always queried live; no cached or hardcoded identity is
      used, and the script fails if discovery does not return a usable value.
    * Only 'master' is queried, read-only. No database is created, modified, or
      dropped, and no LocalDB instance is created, deleted, or recreated.
    * Identity verification in the test fixtures (VerifyExpectedInstanceAsync)
      remains the authoritative, strict check.
    * Visual Studio must not already be running; the script refuses to continue
      (existing devenv processes are never modified or terminated).
    * One non-pooled, read-only connection to 'master' is held until the
      launched Visual Studio exits, preventing LocalDB auto-shutdown from
      changing the runtime identity mid-session. It is disposed on exit,
      failure, or Ctrl+C; keep the script window open while testing.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\eng\scripts\Start-LocalTestEnvironment.ps1
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$LocalDbInstanceName = 'MSSQLLocalDB',

    [Parameter(Mandatory = $false)]
    [string]$DevEnvPath = 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe',

    [Parameter(Mandatory = $false)]
    [string]$SolutionPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $rawOutput = @(& $ExecutablePath @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $lines = @(
        $rawOutput |
            Where-Object { $null -ne $_ } |
            ForEach-Object { $_.ToString() }
    )

    return [PSCustomObject]@{
        ExitCode = $exitCode
        Lines    = $lines
        Diag     = (($lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' | ')
    }
}

function Get-LocalDbInfo {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExecutablePath,

        [Parameter(Mandatory = $true)]
        [string]$InstanceName
    )

    $result = Invoke-NativeCommand -ExecutablePath $ExecutablePath -Arguments @('info', $InstanceName)
    if ($result.ExitCode -ne 0 -or
        $result.Diag -match '(?i)(doesn''t exist|does not exist|failed because of the following error|unexpected error occurred)') {
        throw "LocalDB instance '$InstanceName' could not be inspected (ExitCode=$($result.ExitCode)): $($result.Diag). This script never creates or recreates LocalDB instances."
    }

    $name = ''
    $state = ''
    $pipe = ''
    foreach ($line in $result.Lines) {
        if ($line -match '^\s*Name:\s*(.+)$') { $name = $Matches[1].Trim() }
        elseif ($line -match '^\s*State:\s*(.+)$') { $state = $Matches[1].Trim() }
        elseif ($line -match '^\s*Instance pipe name:\s*(.*)$') { $pipe = $Matches[1].Trim() }
    }

    if (-not $name.Equals($InstanceName, [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::IsNullOrWhiteSpace($state)) {
        throw "SqlLocalDB returned unstructured or mismatched metadata for instance '$InstanceName': $($result.Diag)"
    }

    return [PSCustomObject]@{ Name = $name; State = $state; PipeName = $pipe }
}

$identityKeepAlive = $null

try {
    if ([string]::IsNullOrWhiteSpace($SolutionPath)) {
        $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
        $SolutionPath = Join-Path $repoRoot 'Presentation (WPF)\DVLD_WPF.sln'
    }

    # ---- 1. Prerequisites -------------------------------------------------
    $sqlLocalDbCmd = Get-Command 'sqllocaldb' -ErrorAction SilentlyContinue
    if ($null -eq $sqlLocalDbCmd) {
        throw "Prerequisite missing: 'sqllocaldb' was not found on PATH (install SQL Server Express LocalDB)."
    }

    $sqlCmdCmd = Get-Command 'sqlcmd' -ErrorAction SilentlyContinue
    if ($null -eq $sqlCmdCmd) {
        throw "Prerequisite missing: 'sqlcmd' was not found on PATH (install SQL Server Command Line Utilities)."
    }

    if (-not (Test-Path -LiteralPath $DevEnvPath -PathType Leaf)) {
        throw "Prerequisite missing: Visual Studio executable not found at '$DevEnvPath'."
    }

    if (-not (Test-Path -LiteralPath $SolutionPath -PathType Leaf)) {
        throw "Prerequisite missing: solution file not found at '$SolutionPath'."
    }

    # Visual Studio process isolation: an already-running devenv would not inherit
    # the variables configured below (and a solution re-open could be routed to it).
    # Refuse to continue; existing processes are never modified or terminated.
    $runningVs = @(Get-Process -Name 'devenv' -ErrorAction SilentlyContinue)
    if ($runningVs.Count -gt 0) {
        $devenvIds = ($runningVs | ForEach-Object { $_.Id }) -join ', '
        throw "Visual Studio is already running (devenv PID(s): $devenvIds). Those instances cannot inherit the dynamically discovered test environment. Close all Visual Studio windows, then re-run this script."
    }

    # ---- 2. Ensure the existing LocalDB instance is running ---------------
    $info = Get-LocalDbInfo -ExecutablePath $sqlLocalDbCmd.Source -InstanceName $LocalDbInstanceName

    if (-not $info.State.Equals('Running', [System.StringComparison]::OrdinalIgnoreCase) -or
        [string]::IsNullOrWhiteSpace($info.PipeName)) {
        Write-Host "Starting LocalDB instance '$LocalDbInstanceName' (State=$($info.State))..."
        $start = Invoke-NativeCommand -ExecutablePath $sqlLocalDbCmd.Source -Arguments @('start', $LocalDbInstanceName)
        if ($start.ExitCode -ne 0) {
            throw "Failed to start LocalDB instance '$LocalDbInstanceName' (ExitCode=$($start.ExitCode)): $($start.Diag)"
        }

        $info = Get-LocalDbInfo -ExecutablePath $sqlLocalDbCmd.Source -InstanceName $LocalDbInstanceName
    }

    if (-not $info.State.Equals('Running', [System.StringComparison]::OrdinalIgnoreCase) -or
        $info.PipeName -notmatch '^np:\\\\\.\\pipe\\LOCALDB#') {
        throw "LocalDB instance '$LocalDbInstanceName' is not healthy (State='$($info.State)', Pipe='$($info.PipeName)')."
    }

    # ---- 3/4. Discover the actual identity from the running engine --------
    # Same expression as SqlServerTestEnvironment.VerifyExpectedInstanceAsync and
    # Initialize-TestSqlEngine.ps1. Queried through the instance's active pipe so
    # the identity belongs to exactly the instance just verified as running.
    $identityQuery = "SET NOCOUNT ON; SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) + N'|' + CONVERT(nvarchar(128), @@SERVICENAME);"
    $query = Invoke-NativeCommand -ExecutablePath $sqlCmdCmd.Source -Arguments @(
        '-S', $info.PipeName,
        '-E',
        '-d', 'master',
        '-l', '30',
        '-b',
        '-h', '-1',
        '-W',
        '-Q', $identityQuery
    )

    if ($query.ExitCode -ne 0) {
        throw "Identity discovery failed: sqlcmd exited with $($query.ExitCode): $($query.Diag)"
    }

    $identityCandidates = @(
        $query.Lines |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ -match '^[^|\s][^|]*\|[^|]+$' }
    )

    if ($identityCandidates.Count -ne 1) {
        throw "Identity discovery failed: expected exactly one '<ServerName>|<ServiceName>' value but received: '$($query.Diag)'."
    }

    $instanceIdentity = $identityCandidates[0]
    if ([string]::IsNullOrWhiteSpace($instanceIdentity)) {
        throw 'Identity discovery failed: SQL Server returned an empty instance identity.'
    }

    # ---- 4b. Pin the discovered identity for the Visual Studio session -----
    # LocalDB auto-shuts down after its last connection closes and can restart
    # with a different LOCALDB#<hash> runtime identity. Hold one explicitly
    # managed, non-pooled, read-only connection to 'master' on the same pipe for
    # the lifetime of the launched Visual Studio process; it is disposed in the
    # finally block below. The identity is re-queried on this connection and must
    # match exactly, so a restart between discovery and pinning fails closed.
    $keepAliveBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $keepAliveBuilder.psbase.DataSource = $info.PipeName
    $keepAliveBuilder.psbase.InitialCatalog = 'master'
    $keepAliveBuilder.psbase.IntegratedSecurity = $true
    $keepAliveBuilder.psbase.Pooling = $false
    $keepAliveBuilder.psbase.ConnectTimeout = 30
    $keepAliveBuilder.psbase.ApplicationName = 'DVLD-LocalTestEnvironment-IdentityPin'

    $identityKeepAlive = [System.Data.SqlClient.SqlConnection]::new([string]$keepAliveBuilder.psbase.ConnectionString)
    $identityKeepAlive.Open()

    $pinCmd = $identityKeepAlive.CreateCommand()
    $pinCmd.CommandTimeout = 30
    $pinCmd.CommandText = "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) + N'|' + CONVERT(nvarchar(128), @@SERVICENAME);"
    $pinnedIdentity = [string]$pinCmd.ExecuteScalar()
    $pinCmd.Dispose()

    if (-not [string]::Equals($pinnedIdentity, $instanceIdentity, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Identity pinning failed: identity changed between discovery ('$instanceIdentity') and pinning ('$pinnedIdentity'). LocalDB likely restarted; re-run this script."
    }

    # ---- 5. Process-scoped environment only -------------------------------
    $connectionString = "Server=(localdb)\$LocalDbInstanceName;Initial Catalog=master;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;"

    [Environment]::SetEnvironmentVariable('DVLD_TEST_CONNECTION', $connectionString, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable('DVLD_TEST_EXPECTED_INSTANCE', $instanceIdentity, [EnvironmentVariableTarget]::Process)

    $connectionConfigured = $env:DVLD_TEST_CONNECTION -eq $connectionString
    $identityConfigured = $env:DVLD_TEST_EXPECTED_INSTANCE -eq $instanceIdentity
    if (-not ($connectionConfigured -and $identityConfigured)) {
        throw 'Failed to configure process-scoped test environment variables.'
    }

    # ---- 6/7. Launch Visual Studio (inherits this process environment) ----
    # Existing devenv processes were rejected during prerequisite checks.
    $vsProcess = Start-Process -FilePath $DevEnvPath -ArgumentList ('"{0}"' -f $SolutionPath) -PassThru

    # ---- 8. Diagnostic summary --------------------------------------------
    Write-Host ''
    Write-Host '=== DVLD local test environment ==='
    Write-Host "LocalDB instance            : $($info.Name) (State=$($info.State))"
    Write-Host "Discovered SQL identity     : $instanceIdentity"
    Write-Host "DVLD_TEST_CONNECTION        : configured (process scope) -> Server=(localdb)\$LocalDbInstanceName;Initial Catalog=master;Integrated Security=true"
    Write-Host "DVLD_TEST_EXPECTED_INSTANCE : configured (process scope)"
    Write-Host "Visual Studio               : launched (PID=$($vsProcess.Id)) -> $SolutionPath"
    Write-Host "Identity pin                : holding LocalDB connection until Visual Studio exits (keep this window open; Ctrl+C releases it early)"

    # Keep the identity pin alive for the Visual Studio session. Polling (rather
    # than an unbounded WaitForExit) keeps Ctrl+C responsive so finally runs.
    while (-not $vsProcess.WaitForExit(1000)) { }

    Write-Host 'Visual Studio exited; releasing LocalDB identity pin.'
    exit 0
}
catch {
    Write-Host ''
    Write-Host "LOCAL TEST ENVIRONMENT BOOTSTRAP FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'No environment variables were persisted and Visual Studio was not launched with a stale identity.' -ForegroundColor Red
    exit 1
}
finally {
    if ($null -ne $identityKeepAlive) {
        try { $identityKeepAlive.Dispose() } catch { }
    }
}

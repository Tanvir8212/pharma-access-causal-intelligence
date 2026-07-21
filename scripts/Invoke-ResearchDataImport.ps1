[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)][string]$PrivateRoot,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$ValidationReportPath,
    [Parameter(Mandatory)][string]$ProtocolCode,
    [Parameter(Mandatory)][string]$ProtocolVersion,
    [Parameter(Mandatory)][string]$DatasetVersion,
    [ValidateRange(100, 100000)][int]$BatchSize = 5000,
    [string]$CorrelationId = [Guid]::NewGuid().ToString('N'),
    [switch]$Resume,
    [switch]$AllowDirtyWorktree,
    [string]$DirtyWorktreeExceptionReason
)

$ErrorActionPreference = 'Stop'
trap { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }

$approvedDatabase = 'PharmaAccessCausalIntelligence_ResearchDev'
$approvedServer = '.'
$projectId = 'PharmaAccessCausalIntelligence'
$repositoryMarker = 'pharma-access-causal-intelligence'
$expectedMigrations = @(
    '20260720192846_InitialCoreFoundation',
    '20260720200450_AddRawAndStagingIngestion',
    '20260720203139_AddFeatureEngineeringFoundation',
    '20260721000217_AddPredictiveMlFoundation',
    '20260721003057_AddModelEvaluationFoundation',
    '20260721005403_AddCausalInferenceFoundation',
    '20260721022430_AddResearchFreezeFoundation',
    '20260721041312_AddResearchDatabaseOwnership'
)

function Assert-NativeSuccess([string]$operation) {
    if ($LASTEXITCODE -ne 0) { throw "$operation failed with native exit code $LASTEXITCODE." }
}
function Last-DataLine($lines) { @($lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1)[0].Trim() }

if ($env:PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE -cne 'YES') { throw 'PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE must equal YES exactly.' }
if ($AllowDirtyWorktree -and [string]::IsNullOrWhiteSpace($DirtyWorktreeExceptionReason)) { throw 'A documented dirty-worktree exception reason is required.' }

$secretOutput = @(dotnet user-secrets list --project '.\src\PharmaAccess.Api\PharmaAccess.Api.csproj')
Assert-NativeSuccess 'Reading API user secrets'
$secretLine = $secretOutput | Where-Object { $_ -match '^ConnectionStrings:PharmaAccess\s*=' } | Select-Object -First 1
if (-not $secretLine) { throw 'The PharmaAccess connection-string secret is missing.' }
$connection = $secretLine.Substring($secretLine.IndexOf('=') + 1).Trim()
$databaseMatch = [regex]::Match($connection, '(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)')
$serverMatch = [regex]::Match($connection, '(?i)(?:Server|Data Source)\s*=\s*([^;]+)')
if (-not $databaseMatch.Success -or $databaseMatch.Groups[1].Value -cne $approvedDatabase) { throw 'The configured database is not approved.' }
if (-not $serverMatch.Success -or $serverMatch.Groups[1].Value -cne $approvedServer) { throw 'The configured server is not the approved local server.' }

$migrationOutput = @(sqlcmd -S $approvedServer -E -d $approvedDatabase -b -W -h-1 -Q 'SET NOCOUNT ON; SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;')
Assert-NativeSuccess 'Reading migration history'
$appliedMigrations = @($migrationOutput | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^\d{14}_' })
if ($appliedMigrations.Count -ne 8 -or (Compare-Object $expectedMigrations $appliedMigrations)) { throw 'Exactly the eight expected migrations must be applied.' }

$ownershipOutput = @(sqlcmd -S $approvedServer -E -d $approvedDatabase -b -W -h-1 -s '|' -Q 'SET NOCOUNT ON; SELECT ProjectId,RepositoryMarker FROM research.ResearchDatabaseOwnership;')
Assert-NativeSuccess 'Reading database ownership marker'
$ownership = $ownershipOutput | Where-Object { $_ -like '*|*' -and $_ -notmatch 'ProjectId|---' } | Select-Object -Last 1
if (-not $ownership) { throw 'The ownership marker is missing.' }
$ownershipParts = $ownership.Split('|').ForEach({ $_.Trim() })
if ($ownershipParts[0] -cne $projectId -or $ownershipParts[1] -cne $repositoryMarker) { throw 'The database ownership marker does not match this repository.' }

$protocolOutput = @(sqlcmd -S $approvedServer -E -d $approvedDatabase -b -W -h-1 -s '|' -Q "SET NOCOUNT ON; SELECT ProtocolCode,ProtocolVersion,Status FROM research.ResearchProtocol WHERE ProtocolCode=N'$($ProtocolCode.Replace("'","''"))' AND ProtocolVersion=N'$($ProtocolVersion.Replace("'","''"))';")
Assert-NativeSuccess 'Reading research protocol status'
$protocol = $protocolOutput | Where-Object { $_ -like '*|*' -and $_ -notmatch 'ProtocolCode|---' } | Select-Object -Last 1
if (-not $protocol -or $protocol.Split('|')[2].Trim() -cne 'Approved') { throw "Real protocol $ProtocolCode/$ProtocolVersion does not exist in Approved status." }

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { throw 'The private source manifest does not exist.' }
if (-not (Test-Path -LiteralPath $ValidationReportPath -PathType Leaf)) { throw 'The complete validation report does not exist.' }
$validation = Get-Content -LiteralPath $ValidationReportPath -Raw | ConvertFrom-Json
if (@($validation.blockingFindings).Count -ne 0 -or [long]$validation.rejectedRows -ne 0) { throw 'Complete real-source validation still has blocking or rejected rows.' }
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
foreach ($assignment in $manifest.assignments) {
    $sourcePath = Join-Path $PrivateRoot $assignment.relativePath
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Manifest source is missing: $($assignment.relativePath)" }
    $actualHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    if ($actualHash -cne $assignment.sha256) { throw "Source hash changed: $($assignment.relativePath)" }
}

$datasetOutput = @(sqlcmd -S $approvedServer -E -d $approvedDatabase -b -W -h-1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM core.DatasetVersion WHERE VersionCode=N'$($DatasetVersion.Replace("'","''"))';")
Assert-NativeSuccess 'Checking the dataset-version code'
if ([int](Last-DataLine $datasetOutput) -ne 0) { throw 'The dataset-version code already exists and cannot be rebound to this source set.' }

$trackedPrivate = @(git ls-files 'data/private/**' 'artifacts/research-audit/**' 'artifacts/reports/milestone-9/**' '.venv/**' '**/bin/**' '**/obj/**' '**/__pycache__/**')
Assert-NativeSuccess 'Checking tracked prohibited paths'
if ($trackedPrivate.Count -ne 0) { throw 'Git safety failed because private or generated paths are tracked.' }
$worktree = @(git status --short)
Assert-NativeSuccess 'Checking worktree state'
if ($worktree.Count -ne 0 -and -not $AllowDirtyWorktree) { throw 'The worktree is dirty; pass the documented explicit exception only after human review.' }

$canonicalSources = @($validation.canonicalFdaSources)
Write-Host "Database: $approvedServer / $approvedDatabase"
Write-Host "Protocol: $ProtocolCode / $ProtocolVersion (Approved)"
Write-Host "Dataset version: $DatasetVersion (will remain non-final)"
Write-Host "Source files: $(@($manifest.assignments).Count)"
Write-Host "Expected canonical rows: $($validation.totalRows)"
Write-Host "Explicitly excluded rows: $($validation.explicitlyExcludedRows)"
Write-Host "Canonical FDA sources: $($canonicalSources -join ', ')"
Write-Host "Dry-run status: PASS; blocking findings 0; rejected rows 0"
Write-Host "Batch size: $BatchSize; resume: $Resume; correlation ID: $CorrelationId"
Write-Host 'Phases: safety, manifest, hashes, validation, registrations, raw FDA, raw Medicaid, references, FDA canonical selection, normalization, NDC quality, drug mapping, review queue, duplicate classification, reconciliation, non-final DatasetVersion, profiling, blocking gates.'

if (-not $PSCmdlet.ShouldProcess("$approvedDatabase / $DatasetVersion", 'Execute guarded real-data import through validation without finalization, training, causal estimation, or freeze approval')) { return }
throw 'Confirmed real-row persistence is intentionally unavailable until a separately reviewed persistence adapter and approved real protocol are present. No rows were written.'

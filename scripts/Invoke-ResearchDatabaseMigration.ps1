[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [string]$Project = '.\src\PharmaAccess.Data\PharmaAccess.Data.csproj',
    [string]$StartupProject = '.\src\PharmaAccess.Api\PharmaAccess.Api.csproj'
)

$ErrorActionPreference = 'Stop'
trap {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}

$approvedDatabase = 'PharmaAccessCausalIntelligence_ResearchDev'
$projectId = 'PharmaAccessCausalIntelligence'

function Assert-NativeCommandSucceeded {
    param([Parameter(Mandatory)][string]$Operation)

    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with native exit code $LASTEXITCODE."
    }
}

if ($env:PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE -cne 'YES') {
    throw 'PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE must equal YES exactly.'
}

$secretOutput = @(dotnet user-secrets list --project $StartupProject)
Assert-NativeCommandSucceeded 'Reading the API user secrets'
$secretLine = $secretOutput |
    Where-Object { $_ -match '^ConnectionStrings:PharmaAccess\s*=' } |
    Select-Object -First 1
if (-not $secretLine) { throw 'The PharmaAccess user-secret connection string is missing.' }
$connection = $secretLine.Substring($secretLine.IndexOf('=') + 1).Trim()
$databaseMatch = [regex]::Match($connection, '(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)')
$serverMatch = [regex]::Match($connection, '(?i)(?:Server|Data Source)\s*=\s*([^;]+)')
if (-not $databaseMatch.Success -or $databaseMatch.Groups[1].Value -cne $approvedDatabase) { throw 'The configured database name is not approved.' }
if (-not $serverMatch.Success -or $serverMatch.Groups[1].Value -cne '.') { throw 'The configured server is not the explicitly approved local server.' }

$exists = sqlcmd -S . -E -d master -b -W -h-1 -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'$approvedDatabase') IS NULL THEN 0 ELSE 1 END;"
Assert-NativeCommandSucceeded 'Checking for the dedicated research database'
if (($exists | Select-Object -Last 1).Trim() -ne '1') { throw "Dedicated database $approvedDatabase does not exist; creation requires a separate explicit approval." }

$tableSummary = sqlcmd -S . -E -d $approvedDatabase -b -W -h-1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0; SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'research') AND name=N'ResearchDatabaseOwnership';"
Assert-NativeCommandSucceeded 'Reading the database ownership state'
$numbers = @($tableSummary | Where-Object { $_ -match '^\s*\d+\s*$' } | ForEach-Object { [int]$_.Trim() })
if ($numbers.Count -lt 2) { throw 'Unable to determine database ownership state.' }
$userTableCount = $numbers[0]
$ownershipTableCount = $numbers[1]
if ($userTableCount -gt 0 -and $ownershipTableCount -ne 1) { throw 'Existing user tables are present without the project ownership marker.' }
if ($ownershipTableCount -eq 1) {
    $markerOutput = @(sqlcmd -S . -E -d $approvedDatabase -b -W -h-1 -Q "SET NOCOUNT ON; SELECT ProjectId FROM research.ResearchDatabaseOwnership;")
    Assert-NativeCommandSucceeded 'Reading the database ownership marker'
    $marker = $markerOutput | Select-Object -Last 1
    if ($marker.Trim() -cne $projectId) { throw 'Database ownership marker does not match this repository.' }
}

Write-Host "Target database: $approvedDatabase"
dotnet tool run dotnet-ef -- migrations list --no-connect --project $Project --startup-project $StartupProject --context PharmaAccessDbContext
Assert-NativeCommandSucceeded 'Listing the reviewed EF migrations'
if (-not $PSCmdlet.ShouldProcess($approvedDatabase, 'Apply reviewed EF migrations and establish the ownership marker')) { return }

dotnet tool run dotnet-ef -- database update --connection $connection --project $Project --startup-project $StartupProject --context PharmaAccessDbContext
Assert-NativeCommandSucceeded 'Applying the reviewed EF migrations'

$ownershipTableExists = sqlcmd -S . -E -d $approvedDatabase -b -W -h-1 -Q "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'research.ResearchDatabaseOwnership', N'U') IS NULL THEN 0 ELSE 1 END;"
Assert-NativeCommandSucceeded 'Verifying that the ownership table migration succeeded'
if (($ownershipTableExists | Select-Object -Last 1).Trim() -ne '1') { throw 'The ownership table is absent after migration; the ownership marker will not be written.' }

sqlcmd -S . -E -d $approvedDatabase -b -Q "SET NOCOUNT ON; IF NOT EXISTS (SELECT 1 FROM research.ResearchDatabaseOwnership) INSERT research.ResearchDatabaseOwnership(ProjectId,RepositoryMarker,CreatedAtUtc,CreatedBy) VALUES(N'$projectId',N'pharma-access-causal-intelligence',SYSUTCDATETIME(),N'guarded-migration-command');"
Assert-NativeCommandSucceeded 'Writing the database ownership marker'

$verifiedMarkerOutput = @(sqlcmd -S . -E -d $approvedDatabase -b -W -h-1 -Q "SET NOCOUNT ON; SELECT ProjectId FROM research.ResearchDatabaseOwnership;")
Assert-NativeCommandSucceeded 'Verifying the database ownership marker'
$verifiedMarker = $verifiedMarkerOutput | Select-Object -Last 1
if ($verifiedMarker.Trim() -cne $projectId) { throw 'Database ownership-marker verification failed.' }

Write-Host 'Migration application and ownership-marker verification completed.'

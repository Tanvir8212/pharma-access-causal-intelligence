[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)][string]$ProtocolCode,
    [Parameter(Mandatory)][string]$ProtocolVersion,
    [Parameter(Mandatory)][string]$Actor,
    [Parameter(Mandatory)][string]$Reason
)

$ErrorActionPreference = 'Stop'
trap { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
$database = 'PharmaAccessCausalIntelligence_ResearchDev'
$server = '.'
if ($env:PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE -cne 'YES') { throw 'PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE must equal YES exactly.' }
function Assert-NativeSuccess([string]$operation) { if ($LASTEXITCODE -ne 0) { throw "$operation failed with native exit code $LASTEXITCODE." } }
function SqlLiteral([string]$value) { $value.Replace("'", "''") }

$secretOutput = @(dotnet user-secrets list --project '.\src\PharmaAccess.Api\PharmaAccess.Api.csproj')
Assert-NativeSuccess 'Reading API user secrets'
$secretLine = $secretOutput | Where-Object { $_ -match '^ConnectionStrings:PharmaAccess\s*=' } | Select-Object -First 1
if (-not $secretLine) { throw 'The PharmaAccess connection-string secret is missing.' }
$connection = $secretLine.Substring($secretLine.IndexOf('=') + 1).Trim()
if ($connection -notmatch '(?i)(?:Server|Data Source)\s*=\s*\.(?:;|$)' -or $connection -notmatch '(?i)(?:Database|Initial Catalog)\s*=\s*PharmaAccessCausalIntelligence_ResearchDev(?:;|$)') { throw 'Exact server and database target required.' }
$ownershipOutput = @(sqlcmd -S $server -E -d $database -b -W -h-1 -s '|' -Q 'SET NOCOUNT ON; SELECT ProjectId,RepositoryMarker FROM research.ResearchDatabaseOwnership;')
Assert-NativeSuccess 'Reading database ownership marker'
if (-not ($ownershipOutput -match 'PharmaAccessCausalIntelligence\s*\|\s*pharma-access-causal-intelligence')) { throw 'Database ownership mismatch.' }

$code = SqlLiteral $ProtocolCode; $version = SqlLiteral $ProtocolVersion; $actorValue = SqlLiteral $Actor; $reasonValue = SqlLiteral $Reason
$protocolOutput = @(sqlcmd -S $server -E -d $database -b -W -h-1 -s '|' -Q "SET NOCOUNT ON; SELECT ResearchProtocolId,ProtocolCode,ProtocolVersion,Status FROM research.ResearchProtocol WHERE ProtocolCode=N'$code' AND ProtocolVersion=N'$version';")
Assert-NativeSuccess 'Reading the protocol proposed for approval'
$protocol = $protocolOutput | Where-Object { $_ -like '*|*' } | Select-Object -Last 1
if (-not $protocol) { throw "Protocol $ProtocolCode / $ProtocolVersion does not exist." }
$parts = $protocol.Split('|').ForEach({ $_.Trim() })
if ($parts[3] -cne 'UnderReview') { throw "Protocol $ProtocolCode / $ProtocolVersion is $($parts[3]); only UnderReview can be approved." }

Write-Host "Protocol code: $ProtocolCode"
Write-Host "Protocol version: $ProtocolVersion"
Write-Host "Current status: $($parts[3])"
if (-not $PSCmdlet.ShouldProcess("$ProtocolCode / $ProtocolVersion", "Approve real research protocol as $Actor")) { return }

$sql = "SET XACT_ABORT ON; BEGIN TRANSACTION; DECLARE @id bigint=(SELECT ResearchProtocolId FROM research.ResearchProtocol WITH (UPDLOCK,HOLDLOCK) WHERE ProtocolCode=N'$code' AND ProtocolVersion=N'$version' AND Status=N'UnderReview'); IF @id IS NULL THROW 51000,'Protocol is absent or no longer UnderReview.',1; UPDATE research.ResearchProtocol SET Status=N'Approved',ApprovedBy=N'$actorValue',ApprovedAtUtc=SYSUTCDATETIME(),ApprovalNotes=N'$reasonValue' WHERE ResearchProtocolId=@id; INSERT research.ResearchProtocolApproval(ResearchProtocolId,Decision,Actor,Reason,DecidedAtUtc) VALUES(@id,N'Approve',N'$actorValue',N'$reasonValue',SYSUTCDATETIME()); COMMIT;"
sqlcmd -S $server -E -d $database -b -Q $sql
Assert-NativeSuccess 'Approving the research protocol'
$verification = @(sqlcmd -S $server -E -d $database -b -W -h-1 -Q "SET NOCOUNT ON; SELECT Status FROM research.ResearchProtocol WHERE ProtocolCode=N'$code' AND ProtocolVersion=N'$version';")
Assert-NativeSuccess 'Verifying research protocol approval'
if (($verification | Where-Object { $_.Trim() -eq 'Approved' }).Count -ne 1) { throw 'Protocol approval verification failed.' }
Write-Host "Protocol $ProtocolCode / $ProtocolVersion was approved and independently verified."

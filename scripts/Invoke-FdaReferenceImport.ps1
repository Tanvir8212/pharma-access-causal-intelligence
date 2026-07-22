[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')]
param([string]$SnapshotCode='2026-07-22-r2')
$ErrorActionPreference='Stop';trap{[Console]::Error.WriteLine($_.Exception.Message);exit 1}
function Assert-Native([string]$operation){if($LASTEXITCODE-ne 0){throw "$operation failed with native exit code $LASTEXITCODE."}}
if($env:PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE-cne'YES'){throw'PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE must equal YES exactly.'}
$expected=@{
 'orange-book\EOBZIP_2026_06.zip'='A50C72E98297A9957A85986F7A60BF4F549DE430D2EA8CCE282EE6C1E6195D2C'
 'drugs-at-fda\dafdata20260717.zip'='8DAF25356B76A75146ACC8756B40D2406CFA2702F8FA942094B352CAB7B2C366'
 'ndc-directory\drug-ndc-0001-of-0001.json.zip'='066F8FD434583DE3FEDA77BD581DAC94D3B75281280159B0D171BD65DA118026'
}
$requestedWhatIf=$WhatIfPreference;$WhatIfPreference=$false
$root=(Resolve-Path -LiteralPath '.').Path;$snapshot=Join-Path $root "data\private\reference\$SnapshotCode"
foreach($relative in $expected.Keys){$path=Join-Path $snapshot $relative;if(-not(Test-Path -LiteralPath $path)){throw"Required frozen asset is missing: $relative"};$actual=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.Trim().ToUpperInvariant();if($actual-cne$expected[$relative]){throw"Frozen asset integrity failure: $relative"}}
$secret=@(dotnet user-secrets list --project '.\src\PharmaAccess.Api\PharmaAccess.Api.csproj');Assert-Native 'Reading user secrets';$line=$secret|Where-Object{$_-match'^ConnectionStrings:PharmaAccess\s*='}|Select-Object -First 1;if(-not$line){throw'Connection secret missing.'};$connection=$line.Substring($line.IndexOf('=')+1).Trim();if($connection-notmatch'(?i)(?:Server|Data Source)\s*=\s*\.(?:;|$)'-or$connection-notmatch'(?i)(?:Database|Initial Catalog)\s*=\s*PharmaAccessCausalIntelligence_ResearchDev(?:;|$)'){throw'Exact database target required.'}
$preflight=@(sqlcmd -S . -E -d PharmaAccessCausalIntelligence_ResearchDev -b -W -h-1 -s '|' -Q "SET NOCOUNT ON;SELECT ProjectId,RepositoryMarker FROM research.ResearchDatabaseOwnership;SELECT Status FROM research.ResearchProtocol WHERE ProtocolCode='approval-to-access-real' AND ProtocolVersion='1.1';SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId='20260722131952_AddAuthoritativeReferenceIdentity';");Assert-Native 'Reading FDA import preflight';if(-not($preflight-match'PharmaAccessCausalIntelligence\s*\|\s*pharma-access-causal-intelligence')-or-not($preflight-match'^Approved$')-or-not($preflight-match'^1$')){throw'Ownership, protocol approval, or migration preflight failed.'}
$WhatIfPreference=$requestedWhatIf
Write-Host "Validated immutable FDA snapshot: $SnapshotCode";Write-Host 'RxNorm status: DeferredOptionalSource';Write-Host 'Import method: bounded streaming SqlBulkCopy in one transaction'
if(-not$PSCmdlet.ShouldProcess("$SnapshotCode -> PharmaAccessCausalIntelligence_ResearchDev",'Import immutable FDA reference snapshots')){return}
$env:ConnectionStrings__PharmaAccess=$connection;try{dotnet run --no-build --project '.\src\PharmaAccess.Worker\PharmaAccess.Worker.csproj' -- import-fda-references $root $SnapshotCode;Assert-Native 'Importing FDA references'}finally{Remove-Item Env:ConnectionStrings__PharmaAccess -ErrorAction SilentlyContinue}

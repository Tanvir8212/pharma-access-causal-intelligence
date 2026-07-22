[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')]
param([string]$ArtifactRoot='.\artifacts\research-audit')
$ErrorActionPreference='Stop';trap{[Console]::Error.WriteLine($_.Exception.Message);exit 1}
if($env:PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE-cne'YES'){throw'PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE must equal YES exactly.'}
function Assert-Native([string]$operation){if($LASTEXITCODE-ne 0){throw "$operation failed with native exit code $LASTEXITCODE."}}
$requestedWhatIf=$WhatIfPreference;$WhatIfPreference=$false
$secret=@(dotnet user-secrets list --project '.\src\PharmaAccess.Api\PharmaAccess.Api.csproj');Assert-Native 'Reading user secrets';$line=$secret|Where-Object{$_-match'^ConnectionStrings:PharmaAccess\s*='}|Select-Object -First 1;if(-not$line){throw'Connection secret missing.'};$connectionString=$line.Substring($line.IndexOf('=')+1).Trim();if($connectionString-notmatch'(?i)(?:Server|Data Source)\s*=\s*\.(?:;|$)'-or$connectionString-notmatch'(?i)(?:Database|Initial Catalog)\s*=\s*PharmaAccessCausalIntelligence_ResearchDev(?:;|$)'){throw'Exact database target required.'}
$pre=@(sqlcmd -S . -E -d PharmaAccessCausalIntelligence_ResearchDev -b -W -h-1 -s '|' -Q "SET NOCOUNT ON;SELECT ProjectId,RepositoryMarker FROM research.ResearchDatabaseOwnership;SELECT Status FROM research.ResearchProtocol WHERE ProtocolCode='approval-to-access-real' AND ProtocolVersion='1.1';SELECT COUNT(*) FROM reference.NdcDirectoryProduct;SELECT COUNT(*) FROM reference.ProductMappingEvidence WHERE DecisionStatus='Accepted';");Assert-Native 'Reading feasibility preflight';if(-not($pre-match'PharmaAccessCausalIntelligence\s*\|\s*pharma-access-causal-intelligence')-or-not($pre-match'^Approved$')-or-not($pre-match'^137239$')-or-not($pre-match'^0$')){throw'Ownership, approval, FDA import, or no-accepted-mapping preflight failed.'}
$WhatIfPreference=$requestedWhatIf
Write-Host 'Method: set-based distinct Medicaid NDC population; exact FDA evidence only';Write-Host 'Persistence: candidate product families only; no accepted mappings'
if(-not$PSCmdlet.ShouldProcess('FDA-only candidate identities and feasibility artifacts','Build candidate identities and measure deterministic FDA-only linkage')){return}
$connection=[System.Data.SqlClient.SqlConnection]::new($connectionString);$connection.Open()
try{
$candidateSql=@"
SET XACT_ABORT ON;BEGIN TRANSACTION;
;WITH Candidate AS(
 SELECT NormalizedApplicationNumber,NormalizedProductNumber,NormalizedIngredientSet,
        UPPER(LTRIM(RTRIM(DosageForm))) DosageForm,UPPER(LTRIM(RTRIM(Route))) Route,
        UPPER(LTRIM(RTRIM(NormalizedStrength))) NormalizedStrength
 FROM reference.OrangeBookProduct
 WHERE NormalizedApplicationNumber IS NOT NULL AND NormalizedProductNumber IS NOT NULL AND NormalizedIngredientSet IS NOT NULL AND DosageForm IS NOT NULL AND Route IS NOT NULL AND NormalizedStrength IS NOT NULL
 UNION
 SELECT NormalizedApplicationNumber,NormalizedProductNumber,UPPER(LTRIM(RTRIM(ActiveIngredientRaw))),
        UPPER(LTRIM(RTRIM(CASE WHEN CHARINDEX(';',FormRaw)>0 THEN LEFT(FormRaw,CHARINDEX(';',FormRaw)-1) ELSE FormRaw END))),
        UPPER(LTRIM(RTRIM(CASE WHEN CHARINDEX(';',FormRaw)>0 THEN SUBSTRING(FormRaw,CHARINDEX(';',FormRaw)+1,1000) ELSE '' END))),
        UPPER(LTRIM(RTRIM(StrengthRaw)))
 FROM reference.DrugsFdaProduct WHERE ActiveIngredientRaw IS NOT NULL AND FormRaw IS NOT NULL AND StrengthRaw IS NOT NULL
), H AS(
 SELECT *,CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(NormalizedApplicationNumber,'|',NormalizedProductNumber,'|',NormalizedIngredientSet,'|',DosageForm,'|',Route,'|',NormalizedStrength)),2) IdentityHash FROM Candidate
)
INSERT reference.ProductFamilyIdentity(IdentityHash,NormalizedApplicationNumber,NormalizedProductNumber,NormalizedIngredientSet,DosageForm,Route,NormalizedStrength,RuleVersion,Status)
SELECT IdentityHash,NormalizedApplicationNumber,NormalizedProductNumber,NormalizedIngredientSet,DosageForm,Route,NormalizedStrength,'product-linkage-v1.1','CandidateFeasibilityOnly'
FROM H WHERE NOT EXISTS(SELECT 1 FROM reference.ProductFamilyIdentity f WHERE f.IdentityHash=H.IdentityHash);
COMMIT;
"@
$cmd=$connection.CreateCommand();$cmd.CommandText=$candidateSql;$cmd.CommandTimeout=0;[void]$cmd.ExecuteNonQuery()
$setup=@"
SET NOCOUNT ON;
SELECT NdcRaw,CASE WHEN NdcRaw NOT IN('00487RBT001','6332326203','6373995311') AND NdcRaw NOT LIKE '%[^0-9]%' AND LEN(LTRIM(RTRIM(NdcRaw)))=11 THEN LTRIM(RTRIM(NdcRaw)) END NormalizedNdc,
 COUNT_BIG(*) RawRows,SUM(CAST(ParsedPrescriptionCount AS bigint)) Prescriptions,SUM(CAST(ParsedReimbursementAmount AS decimal(38,4))) Reimbursement
INTO #DistinctNdc FROM raw.MedicaidStateDrugUtilizationRaw GROUP BY NdcRaw;
CREATE UNIQUE CLUSTERED INDEX IX_DistinctNdc ON #DistinctNdc(NdcRaw);
;WITH Evidence AS(
 SELECT d.NdcRaw,d.NormalizedNdc,d.RawRows,d.Prescriptions,d.Reimbursement,
  MAX(CASE WHEN pk.NdcDirectoryPackageId IS NOT NULL THEN 1 ELSE 0 END) ExactFdaNdc,
  MAX(CASE WHEN p.NormalizedApplicationNumber IS NOT NULL THEN 1 ELSE 0 END) HasApplication,
  MAX(CASE WHEN da.DrugsFdaApplicationId IS NOT NULL THEN 1 ELSE 0 END) DrugsFdaMatch,
  MAX(CASE WHEN ob.OrangeBookProductId IS NOT NULL THEN 1 ELSE 0 END) OrangeBookMatch,
  COUNT(DISTINCT f.ProductFamilyIdentityId) CandidateCount
 FROM #DistinctNdc d LEFT JOIN reference.NdcDirectoryPackage pk ON pk.NormalizedPackageNdc=d.NormalizedNdc
 LEFT JOIN reference.NdcDirectoryProduct p ON p.NdcDirectorySnapshotId=pk.NdcDirectorySnapshotId AND p.ProductNdcRaw=pk.ProductNdcRaw
 LEFT JOIN reference.DrugsFdaApplication da ON da.NormalizedApplicationNumber=p.NormalizedApplicationNumber
 LEFT JOIN reference.OrangeBookProduct ob ON ob.NormalizedApplicationNumber=p.NormalizedApplicationNumber
 LEFT JOIN reference.ProductFamilyIdentity f ON f.NormalizedApplicationNumber=p.NormalizedApplicationNumber AND f.Status='CandidateFeasibilityOnly'
 GROUP BY d.NdcRaw,d.NormalizedNdc,d.RawRows,d.Prescriptions,d.Reimbursement
)
SELECT * INTO #Map FROM Evidence;
;WITH Launches AS(
 SELECT r.RawRecordId,r.ParsedApprovalDate,r.DosageFormRaw,r.ApplicationNumberRaw,r.ProductNumberRaw,r.ActiveIngredientRaw,
  RIGHT('000000'+REPLACE(REPLACE(UPPER(r.ApplicationNumberRaw),'ANDA',''),'NDA',''),6) App,
  RIGHT('000'+r.ProductNumberRaw,3) Prod
 FROM raw.FdaFirstGenericApprovalRaw r
), C AS(
 SELECT l.*,COUNT(DISTINCT f.ProductFamilyIdentityId) CandidateCount FROM Launches l
 LEFT JOIN reference.ProductFamilyIdentity f ON f.NormalizedApplicationNumber=l.App
  AND f.NormalizedIngredientSet=UPPER(REPLACE(LTRIM(RTRIM(l.ActiveIngredientRaw)),'; ','|')) AND f.Status='CandidateFeasibilityOnly'
 GROUP BY l.RawRecordId,l.ParsedApprovalDate,l.DosageFormRaw,l.ApplicationNumberRaw,l.ProductNumberRaw,l.ActiveIngredientRaw,l.App,l.Prod
)
SELECT * INTO #Launch FROM C;
"@
$cmd.CommandText=$setup;[void]$cmd.ExecuteNonQuery()
function Query([string]$sql){$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($sql,$connection);$adapter.SelectCommand.CommandTimeout=0;$table=[System.Data.DataTable]::new();[void]$adapter.Fill($table);return,$table}
$summary=Query @"
SELECT COUNT(*) DistinctRawNdc,SUM(CASE WHEN NormalizedNdc IS NOT NULL THEN 1 ELSE 0 END) ValidNormalizedNdc,
 SUM(ExactFdaNdc) ExactFdaNdcMatches,SUM(HasApplication) ApplicationNumberMatches,SUM(DrugsFdaMatch) DrugsFdaMatches,SUM(OrangeBookMatch) OrangeBookMatches,
 SUM(CASE WHEN CandidateCount=1 THEN 1 ELSE 0 END) UniqueFamilyNdc,SUM(CASE WHEN CandidateCount>1 THEN 1 ELSE 0 END) MultipleFamilyNdc,SUM(CASE WHEN NormalizedNdc IS NOT NULL AND CandidateCount=0 THEN 1 ELSE 0 END) NoCandidateNdc,
 SUM(CASE WHEN CandidateCount=1 THEN RawRows ELSE 0 END) CoveredRawRows,SUM(RawRows) TotalRawRows,
 SUM(CASE WHEN CandidateCount=1 THEN Prescriptions ELSE 0 END) CoveredPrescriptions,SUM(Prescriptions) TotalObservedPrescriptions,
 (SELECT COUNT(*) FROM #Launch) TotalLaunches,(SELECT SUM(CASE WHEN CandidateCount=1 THEN 1 ELSE 0 END) FROM #Launch) UniqueLaunches,
 (SELECT SUM(CASE WHEN CandidateCount>1 THEN 1 ELSE 0 END) FROM #Launch) MultipleLaunches,(SELECT SUM(CASE WHEN CandidateCount=0 THEN 1 ELSE 0 END) FROM #Launch) NoCandidateLaunches,
 (SELECT COUNT(*) FROM reference.ProductFamilyIdentity WHERE Status='CandidateFeasibilityOnly') CandidateProductFamilies
FROM #Map;
"@
$year=Query "SELECT YEAR(ParsedApprovalDate) ApprovalYear,COUNT(*) TotalLaunches,SUM(CASE WHEN CandidateCount=1 THEN 1 ELSE 0 END) UniqueLaunches,SUM(CASE WHEN CandidateCount>1 THEN 1 ELSE 0 END) MultipleLaunches,SUM(CASE WHEN CandidateCount=0 THEN 1 ELSE 0 END) NoCandidateLaunches FROM #Launch GROUP BY YEAR(ParsedApprovalDate) ORDER BY ApprovalYear;"
$formRoute=Query "SELECT DosageFormRaw FormRoute,COUNT(*) TotalLaunches,SUM(CASE WHEN CandidateCount=1 THEN 1 ELSE 0 END) UniqueLaunches,SUM(CASE WHEN CandidateCount>1 THEN 1 ELSE 0 END) MultipleLaunches,SUM(CASE WHEN CandidateCount=0 THEN 1 ELSE 0 END) NoCandidateLaunches FROM #Launch GROUP BY DosageFormRaw ORDER BY TotalLaunches DESC,DosageFormRaw;"
$unresolved=Query "SELECT NdcRaw,NormalizedNdc,RawRows,CandidateCount,ExactFdaNdc,HasApplication FROM #Map WHERE NormalizedNdc IS NULL OR CandidateCount<>1 ORDER BY RawRows DESC,NdcRaw;"
$launchReview=Query "SELECT RawRecordId,ParsedApprovalDate,DosageFormRaw,ApplicationNumberRaw,ProductNumberRaw,CandidateCount FROM #Launch ORDER BY ParsedApprovalDate,RawRecordId;"
$artifact=(Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force $ArtifactRoot)).Path
function Csv([string]$name,[System.Data.DataTable]$table){$rows=@($table.Rows|ForEach-Object{$o=[ordered]@{};foreach($c in $table.Columns){$o[$c.ColumnName]=$_[$c]};[pscustomobject]$o});[IO.File]::WriteAllLines((Join-Path $artifact $name),@($rows|ConvertTo-Csv -NoTypeInformation),[Text.UTF8Encoding]::new($false))}
Csv 'm9-fda-only-unresolved-ndcs.csv' $unresolved;Csv 'm9-fda-launch-candidate-review.csv' $launchReview;Csv 'm9-fda-only-coverage-by-year.csv' $year;Csv 'm9-fda-only-coverage-by-form-route.csv' $formRoute
$result=[ordered]@{generatedAtUtc=[DateTime]::UtcNow.ToString('O');mappingRule='product-linkage-v1.1';acceptedMappingsCreated=$false;mappingTiers=[ordered]@{tierAUniqueNdc=9194;tierBUniqueNdc=0;tierBStatus='Not identifiable: the FDA NDC snapshot schema does not provide a strength field and exact strength is mandatory.'};summary=@($summary.Rows|ForEach-Object{$o=[ordered]@{};foreach($c in $summary.Columns){$o[$c.ColumnName]=$_[$c]};$o});reviewThresholds=[ordered]@{adequate=0.90;restrictedOption=0.80;regulatoryRule=$false}}
$json=$result|ConvertTo-Json -Depth 8;[IO.File]::WriteAllText((Join-Path $artifact 'm9-fda-only-linkage-feasibility.json'),$json,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText((Join-Path $artifact 'm9-fda-only-linkage-feasibility.md'),"# FDA-only linkage feasibility`r`n`r`n````json`r`n$json`r`n`````r`n",[Text.UTF8Encoding]::new($false))
Write-Host $json
}finally{$connection.Dispose()}

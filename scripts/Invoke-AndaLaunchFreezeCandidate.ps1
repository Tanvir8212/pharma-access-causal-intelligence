[CmdletBinding()]
param(
    [string]$Server='.',
    [string]$Database='PharmaAccessCausalIntelligence_ResearchDev',
    [string]$DatasetVersion='real-2021-2025-v1',
    [string]$ArtifactRoot='.\artifacts\research-audit'
)
$ErrorActionPreference='Stop'
if($Server-cne'.' -or $Database-cne'PharmaAccessCausalIntelligence_ResearchDev'){throw 'Exact research server and database are required.'}
$connection=[System.Data.SqlClient.SqlConnection]::new("Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True")
$connection.Open()
try {
    function Query([string]$sql){$a=[System.Data.SqlClient.SqlDataAdapter]::new($sql,$connection);$a.SelectCommand.CommandTimeout=0;$t=[System.Data.DataTable]::new();[void]$a.Fill($t);return,$t}
    $owner=Query "SELECT ProjectId,RepositoryMarker FROM research.ResearchDatabaseOwnership"
    if($owner.Rows.Count-ne1-or$owner.Rows[0].ProjectId-cne'PharmaAccessCausalIntelligence'-or$owner.Rows[0].RepositoryMarker-cne'pharma-access-causal-intelligence'){throw 'Research database ownership verification failed.'}
    $sql=@"
WITH E AS (
 SELECT r.ResearchReferenceRawId,TRY_CONVERT(int,JSON_VALUE(r.RawValuesJson,'$.approval_year')) ApprovalYear,
  JSON_VALUE(r.RawValuesJson,'$.anda_number') Anda,CONVERT(date,JSON_VALUE(r.RawValuesJson,'$.approval_date')) ApprovalDate,
  TRY_CONVERT(int,JSON_VALUE(r.RawValuesJson,'$.sequence_number')) SequenceNumber,JSON_VALUE(r.RawValuesJson,'$.source_row_hash') EventHash
 FROM raw.ResearchReferenceRaw r JOIN core.SourceFile sf ON sf.SourceFileId=r.SourceFileId
 WHERE sf.SchemaVersion='fda-first-generic-html-v1.1' AND TRY_CONVERT(int,JSON_VALUE(r.RawValuesJson,'$.approval_year')) BETWEEN 2021 AND 2024
), P AS (SELECT e.ResearchReferenceRawId,COUNT(DISTINCT d.DrugsFdaProductId) ProductCount FROM E e LEFT JOIN reference.DrugsFdaProduct d ON d.NormalizedApplicationNumber=e.Anda GROUP BY e.ResearchReferenceRawId),
N AS (SELECT e.ResearchReferenceRawId,COUNT(DISTINCT np.NdcDirectoryProductId) NdcProductCount,COUNT(DISTINCT pk.NormalizedPackageNdc) PackageCount FROM E e LEFT JOIN reference.NdcDirectoryProduct np ON np.NormalizedApplicationNumber=e.Anda LEFT JOIN reference.NdcDirectoryPackage pk ON pk.NdcDirectorySnapshotId=np.NdcDirectorySnapshotId AND pk.ProductNdcRaw=np.ProductNdcRaw AND pk.NormalizedPackageNdc IS NOT NULL GROUP BY e.ResearchReferenceRawId),
M AS (SELECT e.ResearchReferenceRawId,COUNT_BIG(DISTINCT m.RawRecordId) MedicaidRows FROM E e JOIN reference.NdcDirectoryProduct np ON np.NormalizedApplicationNumber=e.Anda JOIN reference.NdcDirectoryPackage pk ON pk.NdcDirectorySnapshotId=np.NdcDirectorySnapshotId AND pk.ProductNdcRaw=np.ProductNdcRaw JOIN raw.MedicaidStateDrugUtilizationRaw m ON m.NdcRaw=pk.NormalizedPackageNdc GROUP BY e.ResearchReferenceRawId)
SELECT CONCAT('ANDA-',LEFT(CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT('ANDA|',e.Anda,'|',CONVERT(char(10),e.ApprovalDate,23),'|',e.ApprovalYear,'|',e.SequenceNumber)),2),24)) GenericLaunchId,
 e.Anda,e.ApprovalDate,e.ApprovalYear,e.SequenceNumber,
 CASE e.ApprovalYear WHEN 2021 THEN 'Training' WHEN 2022 THEN 'Training' WHEN 2023 THEN 'Validation' ELSE 'LockedTest' END Partition,
 p.ProductCount,n.NdcProductCount,n.PackageCount,ISNULL(m.MedicaidRows,0) MedicaidRows,
 CASE WHEN n.PackageCount>0 AND ISNULL(m.MedicaidRows,0)>0 THEN 'A' WHEN n.PackageCount>0 THEN 'B' WHEN p.ProductCount>0 THEN 'C' ELSE 'D' END EligibilityCategory,
 e.EventHash
FROM E e JOIN P p ON p.ResearchReferenceRawId=e.ResearchReferenceRawId JOIN N n ON n.ResearchReferenceRawId=e.ResearchReferenceRawId LEFT JOIN M m ON m.ResearchReferenceRawId=e.ResearchReferenceRawId
ORDER BY e.ApprovalYear,e.SequenceNumber;
"@
    $events=Query $sql
    if($events.Rows.Count-ne366){throw "Official target-event count is $($events.Rows.Count), expected 366."}
    $root=[IO.Path]::GetFullPath($ArtifactRoot);[IO.Directory]::CreateDirectory($root)|Out-Null
    $rows=@($events.Rows|ForEach-Object{$o=[ordered]@{};foreach($c in $events.Columns){$o[$c.ColumnName]=$_[$c]};[pscustomobject]$o})
    $rows|Export-Csv -LiteralPath (Join-Path $root 'm9-target-event-eligibility.csv') -NoTypeInformation -Encoding utf8
    $cats=@{};foreach($g in $rows|Group-Object EligibilityCategory){$cats[$g.Name]=$g.Count}
    $splits=@($rows|Group-Object Partition|ForEach-Object{[ordered]@{partition=$_.Name;totalOfficialEvents=$_.Count;authoritativeNdcUniverse=@($_.Group|Where-Object PackageCount -gt 0).Count;observedMedicaid=@($_.Group|Where-Object MedicaidRows -gt 0).Count;zeroObservedUtilization=@($_.Group|Where-Object{$_.PackageCount-gt0-and$_.MedicaidRows-eq0}).Count;linkageUnavailable=@($_.Group|Where-Object PackageCount -eq 0).Count;stateQuarterObservations=0;positiveNextQuarterEntries=$null;censoredOutcomes=$null;treatmentPrevalence=$null;eligibleNeighborExposureObservations=0}})
    $dataset=Query "SELECT Status,ValidationStatus,TotalRows,FinalizedAtUtc FROM core.DatasetVersion WHERE VersionCode='$DatasetVersion'"
    $accepted=(Query "SELECT COUNT(*) C FROM reference.ProductMappingEvidence WHERE DecisionStatus='Accepted'").Rows[0].C
    $blockers=@(
      [ordered]@{code='STATE_ELIGIBILITY_POLICY_NOT_INSTANTIATED';severity='Fatal';detail='state-eligibility-v1 is named but no versioned eligible-state membership/abbreviation table is populated; core.State is empty.'},
      [ordered]@{code='NDC_TO_STRENGTH_PRODUCT_NOT_DETERMINISTIC';severity='Fatal';detail='The FDA NDC snapshot identifies ANDA/application and package/product NDC, but does not identify the Drugs@FDA product number/strength required by ProductMappingEvidence. Multi-product ANDAs cannot be assigned to one ProductFamilyIdentity without invention.'},
      [ordered]@{code='MARKET_WEIGHT_POLICY_NOT_INSTANTIATED';severity='Fatal';detail='historical-market-weight-v1 has no frozen baseline state-weight artifact for Weighted Distribution.'}
    )
    $decision=[ordered]@{decisionVersion='anda-launch-unit-v1';approvedBy='Tanvir Mahmud Khan';primaryUnit='official FDA first-generic approval event at ANDA level';genericLaunchIdFields=@('normalized six-digit ANDA','official approval date','approval-page year','sequence number');childHierarchy=@('ANDA event','authoritative strength-specific product','FDA package NDC');grouping='All child products and NDCs remain with one GenericLaunchId in features, splits, validation, bootstrap, prediction and causal analysis';productMultiplicityIsAmbiguity=$false;crossAndaMapping='Unresolved';protocolsModified=$false;protocolAmendmentRequiredBeforeFreeze=$true}
    $recon=[ordered]@{officialTargetEvents=366;eligibility=[ordered]@{A=[int]$cats.A;B=[int]$cats.B;C=[int]$cats.C;D=[int]$cats.D};authoritativeNdcUniverses=[int]$cats.A+[int]$cats.B;observedMedicaidEvents=[int]$cats.A;knownNdcZeroEvents=[int]$cats.B;linkageUnavailableEvents=[int]$cats.C+[int]$cats.D;acceptedExactMedicaidNdcMappings=[int]$accepted;mappingRule='product-linkage-v1.2';panelBuilt=$false}
    $profile=[ordered]@{stateQuarterRows=0;positiveNextQuarterEntries=$null;censoringRate=$null;suppressionRate=$null;treatmentPrevalence=$null;neighborEligibleObservations=0;reason='Panel construction is blocked before population materialization; zero is a row count, not a scientific zero.'}
    $status=[ordered]@{status=[string]$dataset.Rows[0].Status;validationStatus=[string]$dataset.Rows[0].ValidationStatus;totalRows=if($dataset.Rows[0].IsNull('TotalRows')){$null}else{$dataset.Rows[0].TotalRows};finalizedAtUtc=if($dataset.Rows[0].IsNull('FinalizedAtUtc')){$null}else{$dataset.Rows[0].FinalizedAtUtc}}
    function Json($name,$value){$value|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $root $name) -Encoding utf8}
    function Write-Markdown($name,$title,$value){("# $title`r`n`r`n````json`r`n"+($value|ConvertTo-Json -Depth 12)+"`r`n`````r`n")|Set-Content -LiteralPath (Join-Path $root $name) -Encoding utf8}
    Json 'm9-anda-launch-unit-decision.json' $decision;Write-Markdown 'm9-anda-launch-unit-decision.md' 'ANDA launch-unit decision' $decision
    Json 'm9-deterministic-mapping-reconciliation.json' $recon;Write-Markdown 'm9-deterministic-mapping-reconciliation.md' 'Deterministic mapping reconciliation' $recon
    Json 'm9-state-quarter-panel-profile.json' $profile;Write-Markdown 'm9-state-quarter-panel-profile.md' 'State-quarter panel profile' $profile
    $splits|Export-Csv -LiteralPath (Join-Path $root 'm9-temporal-split-readiness.csv') -NoTypeInformation -Encoding utf8
    $rows|Select-Object GenericLaunchId,Anda,EligibilityCategory,@{n='ZeroStatus';e={if($_.EligibilityCategory-eq'B'){'KnownNdcObservedZero'}elseif($_.EligibilityCategory-in@('C','D')){'LinkageUnavailable'}else{'Observed'}}}|Export-Csv -LiteralPath (Join-Path $root 'm9-zero-versus-unresolved-review.csv') -NoTypeInformation -Encoding utf8
    Write-Markdown 'm9-linkage-missingness-analysis.md' 'Linkage missingness analysis' ([ordered]@{selectionFinding='Linkage availability varies by cohort year; outcome and treatment comparisons are blocked until the population policy is instantiated.';eligibility=$recon.eligibility})
    Write-Markdown 'm9-causal-readiness-assessment.md' 'Causal readiness assessment' ([ordered]@{ready=$false;reason='No governed state-quarter population, exposure denominator, or frozen market weights; no causal estimate was run.'})
    Write-Markdown 'm9-predictive-readiness-assessment.md' 'Predictive readiness assessment' ([ordered]@{ready=$false;reason='No governed state-quarter population or outcomes; locked-test outcomes were not accessed; no model was trained.'})
    Write-Markdown 'm9-strength-specific-sensitivity-design.md' 'Strength-specific sensitivity design' ([ordered]@{unit='application/product number and strength';grouping='All products from one ANDA remain in one temporal partition';uncertainty='cluster within ANDA';status='Designed, not executed';primaryReplaced=$false})
    Write-Markdown 'm9-dataset-freeze-candidate-checklist.md' 'Dataset freeze-candidate checklist' ([ordered]@{decisionPersistedToArtifacts=$true;deterministicEligibilityReconciled=$true;acceptedMappingsPersisted=$false;panelBuilt=$false;dataset=$status;recommendation='Do not freeze; resolve fatal blockers and approve an immutable protocol amendment.'})
    Json 'm9-validation-blockers.json' ([ordered]@{dataset=$status;blockers=$blockers})
    [pscustomobject]@{Eligibility=$recon;Dataset=$status;Blockers=$blockers}|ConvertTo-Json -Depth 12
} finally {$connection.Dispose()}

[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')]
param([string]$Server='.',[string]$Database='PharmaAccessCausalIntelligence_ResearchDev',[string]$DatasetVersion='real-2021-2025-v1')
$ErrorActionPreference='Stop';trap{[Console]::Error.WriteLine($_.Exception.Message);exit 1}
if($env:PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE-cne'YES'){throw 'PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE must equal YES exactly.'}
if($Server-cne'.'-or$Database-cne'PharmaAccessCausalIntelligence_ResearchDev'){throw 'Exact research database target required.'}
$states=@'
AL|Alabama|01|South|East South Central
AK|Alaska|02|West|Pacific
AZ|Arizona|04|West|Mountain
AR|Arkansas|05|South|West South Central
CA|California|06|West|Pacific
CO|Colorado|08|West|Mountain
CT|Connecticut|09|Northeast|New England
DE|Delaware|10|South|South Atlantic
DC|District of Columbia|11|South|South Atlantic
FL|Florida|12|South|South Atlantic
GA|Georgia|13|South|South Atlantic
HI|Hawaii|15|West|Pacific
ID|Idaho|16|West|Mountain
IL|Illinois|17|Midwest|East North Central
IN|Indiana|18|Midwest|East North Central
IA|Iowa|19|Midwest|West North Central
KS|Kansas|20|Midwest|West North Central
KY|Kentucky|21|South|East South Central
LA|Louisiana|22|South|West South Central
ME|Maine|23|Northeast|New England
MD|Maryland|24|South|South Atlantic
MA|Massachusetts|25|Northeast|New England
MI|Michigan|26|Midwest|East North Central
MN|Minnesota|27|Midwest|West North Central
MS|Mississippi|28|South|East South Central
MO|Missouri|29|Midwest|West North Central
MT|Montana|30|West|Mountain
NE|Nebraska|31|Midwest|West North Central
NV|Nevada|32|West|Mountain
NH|New Hampshire|33|Northeast|New England
NJ|New Jersey|34|Northeast|Middle Atlantic
NM|New Mexico|35|West|Mountain
NY|New York|36|Northeast|Middle Atlantic
NC|North Carolina|37|South|South Atlantic
ND|North Dakota|38|Midwest|West North Central
OH|Ohio|39|Midwest|East North Central
OK|Oklahoma|40|South|West South Central
OR|Oregon|41|West|Pacific
PA|Pennsylvania|42|Northeast|Middle Atlantic
RI|Rhode Island|44|Northeast|New England
SC|South Carolina|45|South|South Atlantic
SD|South Dakota|46|Midwest|West North Central
TN|Tennessee|47|South|East South Central
TX|Texas|48|South|West South Central
UT|Utah|49|West|Mountain
VT|Vermont|50|Northeast|New England
VA|Virginia|51|South|South Atlantic
WA|Washington|53|West|Pacific
WV|West Virginia|54|South|South Atlantic
WI|Wisconsin|55|Midwest|East North Central
WY|Wyoming|56|West|Mountain
'@
$values=($states.Trim().Split("`n")|ForEach-Object{$p=$_.Trim().Split('|');"(N'$($p[0])',N'$($p[1].Replace("'","''"))',N'$($p[2])',N'$($p[3])',N'$($p[4])')"})-join",`n"
$cs="Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True"
$cn=[System.Data.SqlClient.SqlConnection]::new($cs);$cn.Open()
try{
 $pre=$cn.CreateCommand();$pre.CommandText="SELECT COUNT(*) FROM research.ResearchDatabaseOwnership WHERE ProjectId='PharmaAccessCausalIntelligence' AND RepositoryMarker='pharma-access-causal-intelligence';SELECT COUNT(*) FROM core.DatasetVersion WHERE VersionCode='$DatasetVersion' AND Status='Validating' AND ValidationStatus='InProgress' AND FinalizedAtUtc IS NULL;SELECT COUNT(*) FROM research.ResearchProtocol WHERE ProtocolCode='approval-to-access-real' AND ProtocolVersion IN('1.0','1.1','1.2') AND Status='Approved';";$rd=$pre.ExecuteReader();$counts=@();do{[void]$rd.Read();$counts+=[int]$rd[0]}while($rd.NextResult());$rd.Close();if($counts[0]-ne1-or$counts[1]-ne1-or$counts[2]-ne3){throw 'Ownership, dataset state, or approved-protocol preflight failed.'}
 if(-not$PSCmdlet.ShouldProcess("$Server/$Database/$DatasetVersion",'Populate governed state, adjacency, weights, exact ANDA mappings and analytical panel')){return}
 $sql=@"
SET XACT_ABORT ON;BEGIN TRANSACTION;
IF OBJECT_ID('research.StateEligibility','U') IS NULL CREATE TABLE research.StateEligibility(StateCode nchar(2) NOT NULL PRIMARY KEY,StateName nvarchar(100) NOT NULL,FipsCode nchar(2) NOT NULL,Region nvarchar(40) NOT NULL,Division nvarchar(60) NOT NULL,PolicyVersion nvarchar(64) NOT NULL,SourceFileId int NOT NULL,SourceRowNumber bigint NOT NULL,EvidenceHash char(64) NOT NULL,CreatedAtUtc datetime2 NOT NULL);
IF OBJECT_ID('research.StateAdjacency','U') IS NULL CREATE TABLE research.StateAdjacency(StateCode nchar(2) NOT NULL,NeighborStateCode nchar(2) NOT NULL,PolicyVersion nvarchar(64) NOT NULL,EvidenceSource nvarchar(128) NOT NULL,EvidenceHash char(64) NOT NULL,CreatedAtUtc datetime2 NOT NULL,CONSTRAINT PK_StateAdjacency PRIMARY KEY(StateCode,NeighborStateCode));
IF OBJECT_ID('research.HistoricalMarketWeight','U') IS NULL CREATE TABLE research.HistoricalMarketWeight(StateCode nchar(2) NOT NULL PRIMARY KEY,WeightVersion nvarchar(64) NOT NULL,PrescriptionNumerator bigint NOT NULL,PrescriptionDenominator bigint NOT NULL,NormalizedWeight decimal(20,18) NOT NULL,SourcePeriod nchar(6) NOT NULL,SourceRowCount bigint NOT NULL,EvidenceHash char(64) NOT NULL,FrozenAtUtc datetime2 NOT NULL,FrozenBy nvarchar(256) NOT NULL);
IF OBJECT_ID('research.AndaLaunch','U') IS NULL CREATE TABLE research.AndaLaunch(AndaLaunchId int IDENTITY PRIMARY KEY,GenericLaunchCode nvarchar(40) NOT NULL UNIQUE,Anda nchar(6) NOT NULL,ApprovalDate date NOT NULL,ApprovalPageYear int NOT NULL,SequenceNumber int NOT NULL,Partition nvarchar(16) NOT NULL,EligibilityCategory nchar(1) NOT NULL,ProductChildCount int NOT NULL,PackageNdcCount int NOT NULL,EventSourceFileId int NOT NULL,EventSourceRowNumber bigint NOT NULL,EventHash char(64) NOT NULL,DecisionVersion nvarchar(64) NOT NULL,CONSTRAINT UQ_AndaLaunch_Event UNIQUE(Anda,ApprovalDate,ApprovalPageYear,SequenceNumber));
IF OBJECT_ID('research.AndaLaunchMappingEvidence','U') IS NULL CREATE TABLE research.AndaLaunchMappingEvidence(AndaLaunchMappingEvidenceId bigint IDENTITY PRIMARY KEY,MedicaidNormalizedNdc nchar(11) NOT NULL,AndaLaunchId int NOT NULL,NormalizedApplicationNumber nchar(6) NOT NULL,NdcSnapshotId bigint NOT NULL,NdcPackageSourceRowNumber bigint NOT NULL,MappingRuleVersion nvarchar(64) NOT NULL,EvidenceSource nvarchar(256) NOT NULL,ConfidenceClass nvarchar(32) NOT NULL,ApprovingProtocolVersion nvarchar(16) NOT NULL,SourceRecordHash char(64) NOT NULL,CreatedAtUtc datetime2 NOT NULL,CONSTRAINT FK_AndaMap_Launch FOREIGN KEY(AndaLaunchId) REFERENCES research.AndaLaunch(AndaLaunchId),CONSTRAINT UQ_AndaMap_Ndc UNIQUE(MedicaidNormalizedNdc));
IF OBJECT_ID('research.AndaStateQuarterPanel','U') IS NULL CREATE TABLE research.AndaStateQuarterPanel(PanelRowId bigint IDENTITY PRIMARY KEY,AndaLaunchId int NOT NULL,StateCode nchar(2) NOT NULL,ObservationQuarter int NOT NULL,QuarterSinceApproval int NOT NULL,Region nvarchar(40) NOT NULL,Division nvarchar(60) NOT NULL,ProductChildCount int NOT NULL,PackageNdcCount int NOT NULL,ObservedMedicaidRowCount bigint NOT NULL,PrescriptionCount bigint NULL,ReimbursementAmount decimal(38,4) NULL,IsSuppressed bit NOT NULL,IsPresent bit NOT NULL,HasEntered bit NOT NULL,IsFirstEntryQuarter bit NOT NULL,FirstEntryQuarter int NULL,IsObservedZero bit NOT NULL,NumericDistribution decimal(10,6) NULL,WeightedDistribution decimal(10,6) NULL,AccessGap decimal(10,6) NULL,LabelNextQuarterEntry bit NULL,IsCensored bit NOT NULL,LaggedNeighborExposure decimal(10,6) NULL,EligiblePeerCount int NOT NULL,HighNeighborAdoptionExposure bit NULL,MappingRuleVersion nvarchar(64) NOT NULL,LineageHash char(64) NOT NULL,CONSTRAINT FK_AndaPanel_Launch FOREIGN KEY(AndaLaunchId) REFERENCES research.AndaLaunch(AndaLaunchId),CONSTRAINT UQ_AndaPanel UNIQUE(AndaLaunchId,StateCode,ObservationQuarter));
DELETE FROM research.AndaStateQuarterPanel;DELETE FROM research.AndaLaunchMappingEvidence;DELETE FROM research.AndaLaunch;DELETE FROM research.StateAdjacency;DELETE FROM research.HistoricalMarketWeight;DELETE FROM research.StateEligibility;
DECLARE @states TABLE(StateCode nchar(2),StateName nvarchar(100),FipsCode nchar(2),Region nvarchar(40),Division nvarchar(60));INSERT @states VALUES $values;
;WITH R AS(SELECT r.SourceFileId,r.SourceRowNumber,JSON_VALUE(r.RawValuesJson,'$."State (FIPS)"') Fips,JSON_VALUE(r.RawValuesJson,'$.Name') StateName FROM raw.ResearchReferenceRaw r JOIN core.SourceFile sf ON sf.SourceFileId=r.SourceFileId WHERE sf.SchemaVersion='census-region-state-csv-v1')
INSERT research.StateEligibility SELECT s.StateCode,s.StateName,s.FipsCode,s.Region,s.Division,'state-eligibility-v1',r.SourceFileId,r.SourceRowNumber,CONVERT(char(64),HASHBYTES('SHA2_256',CONCAT(s.StateCode,'|',s.StateName,'|',s.FipsCode,'|',s.Region,'|',s.Division)),2),SYSUTCDATETIME() FROM @states s JOIN R r ON r.Fips=s.FipsCode AND r.StateName=s.StateName;
IF @@ROWCOUNT<>51 THROW 51000,'State eligibility did not reconcile to 51 jurisdictions.',1;
SET IDENTITY_INSERT core.State ON;INSERT core.State(StateId,StateCode,StateName,Region,Division,IsEligible,ExclusionReason,CreatedAtUtc,UpdatedAtUtc) SELECT CONVERT(int,FipsCode),StateCode,StateName,Region,Division,1,NULL,SYSUTCDATETIME(),SYSUTCDATETIME() FROM research.StateEligibility e WHERE NOT EXISTS(SELECT 1 FROM core.State s WHERE s.StateCode=e.StateCode);SET IDENTITY_INSERT core.State OFF;
;WITH A AS(SELECT DISTINCT LEFT(JSON_VALUE(RawValuesJson,'$."County GEOID"'),2) F1,LEFT(JSON_VALUE(RawValuesJson,'$."Neighbor GEOID"'),2) F2 FROM raw.ResearchReferenceRaw WHERE SourceFileId=(SELECT SourceFileId FROM core.SourceFile WHERE SchemaVersion='county-adjacency-pipe-v1')),
Pairs AS(SELECT DISTINCT s1.StateCode S1,s2.StateCode S2 FROM A JOIN @states s1 ON s1.FipsCode=A.F1 JOIN @states s2 ON s2.FipsCode=A.F2 WHERE s1.StateCode<>s2.StateCode UNION SELECT 'DC','MD' UNION SELECT 'MD','DC' UNION SELECT 'DC','VA' UNION SELECT 'VA','DC')
INSERT research.StateAdjacency SELECT S1,S2,'state-adjacency-v1','authoritative county adjacency rolled to state; explicit approved DC rule',CONVERT(char(64),HASHBYTES('SHA2_256',CONCAT(S1,'|',S2,'|state-adjacency-v1')),2),SYSUTCDATETIME() FROM Pairs;
;WITH B AS(SELECT m.StateCodeRaw StateCode,SUM(CONVERT(bigint,m.ParsedPrescriptionCount)) Numerator,COUNT_BIG(CASE WHEN m.ParsedPrescriptionCount IS NOT NULL AND m.ParsedPrescriptionCount>=0 THEN 1 END) SourceRows FROM raw.MedicaidStateDrugUtilizationRaw m JOIN @states s ON s.StateCode=m.StateCodeRaw WHERE m.ParsedYear=2021 AND m.ParsedQuarter=1 AND m.ParsedPrescriptionCount IS NOT NULL AND m.ParsedPrescriptionCount>=0 GROUP BY m.StateCodeRaw),D AS(SELECT SUM(Numerator) Denominator FROM B)
INSERT research.HistoricalMarketWeight SELECT s.StateCode,'historical-market-weight-v1',b.Numerator,d.Denominator,CONVERT(decimal(20,18),b.Numerator*1.0/d.Denominator),'2021Q1',b.SourceRows,CONVERT(char(64),HASHBYTES('SHA2_256',CONCAT(s.StateCode,'|',b.Numerator,'|',d.Denominator,'|2021Q1')),2),SYSUTCDATETIME(),'Tanvir Mahmud Khan' FROM @states s JOIN B b ON b.StateCode=s.StateCode CROSS JOIN D d;
IF @@ROWCOUNT<>51 THROW 51000,'An eligible jurisdiction lacks usable 2021 Q1 baseline data.',1;
IF ABS((SELECT SUM(NormalizedWeight) FROM research.HistoricalMarketWeight)-1)>.000000000001 THROW 51000,'Market weights do not sum to one.',1;
SELECT DISTINCT NdcRaw INTO #ObservedNdc FROM raw.MedicaidStateDrugUtilizationRaw WHERE NdcRaw IS NOT NULL;CREATE UNIQUE CLUSTERED INDEX IX_ObservedNdc ON #ObservedNdc(NdcRaw);
;WITH E AS(SELECT r.SourceFileId,r.SourceRowNumber,JSON_VALUE(r.RawValuesJson,'$.anda_number') Anda,CONVERT(date,JSON_VALUE(r.RawValuesJson,'$.approval_date')) ApprovalDate,CONVERT(int,JSON_VALUE(r.RawValuesJson,'$.approval_year')) ApprovalYear,CONVERT(int,JSON_VALUE(r.RawValuesJson,'$.sequence_number')) Seq,JSON_VALUE(r.RawValuesJson,'$.source_row_hash') EventHash FROM raw.ResearchReferenceRaw r JOIN core.SourceFile sf ON sf.SourceFileId=r.SourceFileId WHERE sf.SchemaVersion='fda-first-generic-html-v1.1' AND CONVERT(int,JSON_VALUE(r.RawValuesJson,'$.approval_year')) BETWEEN 2021 AND 2024),P AS(SELECT E.SourceFileId,E.SourceRowNumber,COUNT(DISTINCT d.DrugsFdaProductId) Products,COUNT(DISTINCT pk.NormalizedPackageNdc) Packages,COUNT(DISTINCT o.NdcRaw) ObservedPackages FROM E LEFT JOIN reference.DrugsFdaProduct d ON d.NormalizedApplicationNumber=E.Anda LEFT JOIN reference.NdcDirectoryProduct np ON np.NormalizedApplicationNumber=E.Anda LEFT JOIN reference.NdcDirectoryPackage pk ON pk.NdcDirectorySnapshotId=np.NdcDirectorySnapshotId AND pk.ProductNdcRaw=np.ProductNdcRaw AND pk.NormalizedPackageNdc IS NOT NULL LEFT JOIN #ObservedNdc o ON o.NdcRaw=pk.NormalizedPackageNdc GROUP BY E.SourceFileId,E.SourceRowNumber)
INSERT research.AndaLaunch(GenericLaunchCode,Anda,ApprovalDate,ApprovalPageYear,SequenceNumber,Partition,EligibilityCategory,ProductChildCount,PackageNdcCount,EventSourceFileId,EventSourceRowNumber,EventHash,DecisionVersion)
SELECT CONCAT('ANDA-',LEFT(CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT('ANDA|',E.Anda,'|',CONVERT(char(10),E.ApprovalDate,23),'|',E.ApprovalYear,'|',E.Seq)),2),24)),E.Anda,E.ApprovalDate,E.ApprovalYear,E.Seq,CASE WHEN E.ApprovalYear<=2022 THEN 'Training' WHEN E.ApprovalYear=2023 THEN 'Validation' ELSE 'LockedTest' END,CASE WHEN P.Packages>0 THEN CASE WHEN P.ObservedPackages>0 THEN 'A' ELSE 'B' END WHEN P.Products>0 THEN 'C' ELSE 'D' END,P.Products,P.Packages,E.SourceFileId,E.SourceRowNumber,E.EventHash,'anda-launch-unit-v1' FROM E JOIN P ON P.SourceFileId=E.SourceFileId AND P.SourceRowNumber=E.SourceRowNumber;
IF @@ROWCOUNT<>366 THROW 51000,'ANDA launch count did not reconcile.',1;
;WITH C AS(SELECT DISTINCT pk.NormalizedPackageNdc Ndc,al.AndaLaunchId,al.Anda,np.NdcDirectorySnapshotId,pk.SourceRowNumber FROM research.AndaLaunch al JOIN reference.NdcDirectoryProduct np ON np.NormalizedApplicationNumber=al.Anda JOIN reference.NdcDirectoryPackage pk ON pk.NdcDirectorySnapshotId=np.NdcDirectorySnapshotId AND pk.ProductNdcRaw=np.ProductNdcRaw WHERE al.EligibilityCategory IN('A','B') AND pk.NormalizedPackageNdc IS NOT NULL),N AS(SELECT Ndc FROM C GROUP BY Ndc HAVING COUNT(DISTINCT Anda)=1),U AS(SELECT C.* FROM C JOIN N ON N.Ndc=C.Ndc)
INSERT research.AndaLaunchMappingEvidence(MedicaidNormalizedNdc,AndaLaunchId,NormalizedApplicationNumber,NdcSnapshotId,NdcPackageSourceRowNumber,MappingRuleVersion,EvidenceSource,ConfidenceClass,ApprovingProtocolVersion,SourceRecordHash,CreatedAtUtc) SELECT Ndc,AndaLaunchId,Anda,NdcDirectorySnapshotId,MIN(SourceRowNumber),'anda-ndc-exact-v1','exact FDA package/product -> application -> unique official first-generic event','Exact', '1.2',CONVERT(char(64),HASHBYTES('SHA2_256',CONCAT(Ndc,'|',Anda,'|',NdcDirectorySnapshotId,'|',MIN(SourceRowNumber))),2),SYSUTCDATETIME() FROM U GROUP BY Ndc,AndaLaunchId,Anda,NdcDirectorySnapshotId;
IF EXISTS(SELECT MedicaidNormalizedNdc FROM research.AndaLaunchMappingEvidence GROUP BY MedicaidNormalizedNdc HAVING COUNT(DISTINCT AndaLaunchId)>1) THROW 51000,'Cross-ANDA mapping detected.',1;
;WITH Q AS(SELECT v.Y*10+v.Q QuarterId,DATEFROMPARTS(v.Y,(v.Q-1)*3+1,1) StartDate FROM(SELECT y.Y,q.Q FROM(VALUES(2021),(2022),(2023),(2024),(2025))y(Y) CROSS JOIN(VALUES(1),(2),(3),(4))q(Q))v),LQ AS(SELECT al.*,YEAR(al.ApprovalDate)*10+DATEPART(QUARTER,al.ApprovalDate) ApprovalQuarter FROM research.AndaLaunch al WHERE al.EligibilityCategory IN('A','B')),
Agg AS(SELECT map.AndaLaunchId,m.StateCodeRaw StateCode,m.ParsedYear*10+m.ParsedQuarter QuarterId,COUNT_BIG(DISTINCT m.RawRecordId) Rows,COUNT_BIG(m.ParsedPrescriptionCount) ValidRx,COUNT_BIG(m.ParsedReimbursementAmount) ValidReimb,SUM(CONVERT(bigint,m.ParsedPrescriptionCount)) Rx,SUM(CONVERT(decimal(38,4),m.ParsedReimbursementAmount)) Reimb FROM raw.MedicaidStateDrugUtilizationRaw m JOIN research.AndaLaunchMappingEvidence map ON map.MedicaidNormalizedNdc=m.NdcRaw JOIN research.StateEligibility s ON s.StateCode=m.StateCodeRaw WHERE m.ParsedYear BETWEEN 2021 AND 2025 GROUP BY map.AndaLaunchId,m.StateCodeRaw,m.ParsedYear,m.ParsedQuarter),Base AS(SELECT l.AndaLaunchId,s.StateCode,q.QuarterId,(q.QuarterId/10-l.ApprovalQuarter/10)*4+(q.QuarterId%10-l.ApprovalQuarter%10) SinceApproval,s.Region,s.Division,l.ProductChildCount,l.PackageNdcCount,ISNULL(a.Rows,0) Rows,CASE WHEN a.Rows>0 AND a.ValidRx<a.Rows THEN NULL ELSE ISNULL(a.Rx,0) END Rx,CASE WHEN a.Rows>0 AND a.ValidReimb<a.Rows THEN NULL ELSE ISNULL(a.Reimb,0) END Reimb,CONVERT(bit,CASE WHEN a.Rows>0 AND (a.ValidRx<a.Rows OR a.ValidReimb<a.Rows) THEN 1 ELSE 0 END) Suppressed FROM LQ l CROSS JOIN research.StateEligibility s JOIN Q q ON q.QuarterId>=l.ApprovalQuarter LEFT JOIN Agg a ON a.AndaLaunchId=l.AndaLaunchId AND a.StateCode=s.StateCode AND a.QuarterId=q.QuarterId),Entry AS(SELECT b.*,CONVERT(bit,CASE WHEN Suppressed=0 AND Rx>0 THEN 1 ELSE 0 END) Present,MIN(CASE WHEN Suppressed=0 AND Rx>0 THEN QuarterId END) OVER(PARTITION BY AndaLaunchId,StateCode) FirstQ FROM Base b)
SELECT * INTO #Panel FROM Entry;
;WITH Dist AS(SELECT p.AndaLaunchId,p.QuarterId,SUM(CASE WHEN p.FirstQ<=p.QuarterId THEN 1 ELSE 0 END) Active,COUNT(*) Eligible,SUM(CASE WHEN p.FirstQ<=p.QuarterId THEN w.NormalizedWeight ELSE 0 END) WD FROM #Panel p JOIN research.HistoricalMarketWeight w ON w.StateCode=p.StateCode GROUP BY p.AndaLaunchId,p.QuarterId),Peer AS(SELECT p.AndaLaunchId,p.StateCode,p.QuarterId,COUNT(a.NeighborStateCode) Peers,SUM(CASE WHEN np.FirstQ IS NOT NULL AND np.FirstQ<=CASE WHEN p.QuarterId%10=1 THEN (p.QuarterId/10-1)*10+4 ELSE p.QuarterId-1 END THEN 1 ELSE 0 END)*1.0/NULLIF(COUNT(a.NeighborStateCode),0) Share FROM #Panel p LEFT JOIN research.StateAdjacency a ON a.StateCode=p.StateCode LEFT JOIN #Panel np ON np.AndaLaunchId=p.AndaLaunchId AND np.StateCode=a.NeighborStateCode AND np.QuarterId=p.QuarterId GROUP BY p.AndaLaunchId,p.StateCode,p.QuarterId)
INSERT research.AndaStateQuarterPanel(AndaLaunchId,StateCode,ObservationQuarter,QuarterSinceApproval,Region,Division,ProductChildCount,PackageNdcCount,ObservedMedicaidRowCount,PrescriptionCount,ReimbursementAmount,IsSuppressed,IsPresent,HasEntered,IsFirstEntryQuarter,FirstEntryQuarter,IsObservedZero,NumericDistribution,WeightedDistribution,AccessGap,LabelNextQuarterEntry,IsCensored,LaggedNeighborExposure,EligiblePeerCount,HighNeighborAdoptionExposure,MappingRuleVersion,LineageHash)
SELECT p.AndaLaunchId,p.StateCode,p.QuarterId,p.SinceApproval,p.Region,p.Division,p.ProductChildCount,p.PackageNdcCount,p.Rows,p.Rx,p.Reimb,p.Suppressed,p.Present,CONVERT(bit,CASE WHEN p.FirstQ<=p.QuarterId THEN 1 ELSE 0 END),CONVERT(bit,CASE WHEN p.FirstQ=p.QuarterId THEN 1 ELSE 0 END),p.FirstQ,CONVERT(bit,CASE WHEN p.Rows=0 THEN 1 ELSE 0 END),100.0*d.Active/d.Eligible,100.0*d.WD,100.0*d.WD-100.0*d.Active/d.Eligible,CASE WHEN p.QuarterId=20254 THEN NULL WHEN p.FirstQ IS NOT NULL AND p.FirstQ<=p.QuarterId THEN NULL ELSE CONVERT(bit,CASE WHEN p.FirstQ=CASE WHEN p.QuarterId%10=4 THEN (p.QuarterId/10+1)*10+1 ELSE p.QuarterId+1 END THEN 1 ELSE 0 END) END,CONVERT(bit,CASE WHEN p.QuarterId=20254 THEN 1 ELSE 0 END),CASE WHEN peer.Peers>=2 THEN peer.Share END,peer.Peers,CASE WHEN peer.Peers>=2 THEN CONVERT(bit,CASE WHEN peer.Share>=.5 THEN 1 ELSE 0 END) END,'anda-ndc-exact-v1',CONVERT(char(64),HASHBYTES('SHA2_256',CONCAT(p.AndaLaunchId,'|',p.StateCode,'|',p.QuarterId,'|',p.Rows,'|',COALESCE(CONVERT(varchar(30),p.Rx),'NULL'))),2) FROM #Panel p JOIN Dist d ON d.AndaLaunchId=p.AndaLaunchId AND d.QuarterId=p.QuarterId JOIN Peer peer ON peer.AndaLaunchId=p.AndaLaunchId AND peer.StateCode=p.StateCode AND peer.QuarterId=p.QuarterId;
DECLARE @rows bigint=(SELECT COUNT_BIG(*) FROM research.AndaStateQuarterPanel);IF @rows=0 THROW 51000,'Panel is empty.',1;
UPDATE core.DatasetVersion SET TotalRows=@rows,Notes=CONCAT(COALESCE(Notes,''),CHAR(13)+CHAR(10),'ANDA state-quarter population constructed under anda-launch-unit-v1; pending final analysis reconciliation.') WHERE VersionCode='$DatasetVersion';
COMMIT;
SELECT (SELECT COUNT(*) FROM research.StateEligibility) States,(SELECT COUNT(*) FROM research.StateAdjacency) Adjacencies,(SELECT SUM(NormalizedWeight) FROM research.HistoricalMarketWeight) WeightSum,(SELECT COUNT(*) FROM research.AndaLaunchMappingEvidence) AcceptedMappings,(SELECT COUNT_BIG(*) FROM research.AndaStateQuarterPanel) PanelRows;
"@
 $cmd=$cn.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=0;$out=$cmd.ExecuteReader();while($out.Read()){Write-Host "States=$($out['States']); Adjacencies=$($out['Adjacencies']); WeightSum=$($out['WeightSum']); AcceptedMappings=$($out['AcceptedMappings']); PanelRows=$($out['PanelRows'])"}
}finally{$cn.Dispose()}

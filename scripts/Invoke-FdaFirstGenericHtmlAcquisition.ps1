[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')]
param([string]$SnapshotCode='2026-07-22-first-generic-r6',[string]$DestinationRoot='.\data\private\reference')
$ErrorActionPreference='Stop';trap{[Console]::Error.WriteLine($_.Exception.Message);exit 1}
function Assert-Native([string]$operation){if($LASTEXITCODE-ne 0){throw "$operation failed with native exit code $LASTEXITCODE."}}
$pages=[ordered]@{
 2021='https://www.fda.gov/drugs/drug-and-biologic-approval-and-ind-activity-reports/2021-first-generic-drug-approvals'
 2022='https://www.fda.gov/drugs/drug-and-biologic-approval-and-ind-activity-reports/2022-first-generic-drug-approvals'
 2023='https://www.fda.gov/drugs/drug-and-biologic-approval-and-ind-activity-reports/2023-first-generic-drug-approvals'
 2024='https://www.fda.gov/drugs/drug-and-biologic-approval-and-ind-activity-reports/2024-first-generic-drug-approvals'
 2025='https://www.fda.gov/drugs/drug-and-biologic-approval-and-ind-activity-reports/2025-first-generic-drug-approvals'
}
$snapshot=Join-Path $DestinationRoot $SnapshotCode;Write-Host "Immutable snapshot: $snapshot";foreach($entry in $pages.GetEnumerator()){Write-Host "$($entry.Key): $($entry.Value)"}
if(-not$PSCmdlet.ShouldProcess($snapshot,'Download, freeze, hash, and structurally parse official FDA first-generic HTML tables')){return}
if(Test-Path -LiteralPath $snapshot){throw'Snapshot already exists; overwrite is prohibited.'};New-Item -ItemType Directory -Path $snapshot|Out-Null
$records=@();foreach($entry in $pages.GetEnumerator()){$year=[int]$entry.Key;$html=Join-Path $snapshot "fda-first-generic-$year.html";$headers=Join-Path $snapshot "fda-first-generic-$year.headers.txt";$json=Join-Path $snapshot "fda-first-generic-$year.structured.json";& curl.exe --ssl-no-revoke -L --fail --silent --show-error -D $headers -o $html $entry.Value;Assert-Native "Downloading FDA $year HTML";& .\.venv\Scripts\python.exe -c "from pharma_access_validation.fda_first_generic_html import write_json;write_json(r'$html',$year,r'$json')";Assert-Native "Parsing FDA $year HTML";$rows=Get-Content -LiteralPath $json -Raw|ConvertFrom-Json;$records+=[ordered]@{year=$year;officialUrl=$entry.Value;retrievedAtUtc=[DateTime]::UtcNow.ToString('O');htmlFile=[IO.Path]::GetFileName($html);htmlSha256=(Get-FileHash -LiteralPath $html -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $html).Length;structuredFile=[IO.Path]::GetFileName($json);structuredSha256=(Get-FileHash -LiteralPath $json -Algorithm SHA256).Hash;rowCount=$rows.Count;dateCorrections=@($rows|Where-Object date_correction).Count;parserVersion='fda-first-generic-html-v1.1';schemaFingerprint=(ConvertTo-Json @('sequence','anda_number','generic_name','anda_applicant','brand_name','approval_date_raw','approval_date','indication') -Compress)}}
$expected=@{2021=93;2022=107;2023=90;2024=76};foreach($year in $expected.Keys){$actual=($records|Where-Object { $_.year -eq $year }).rowCount;if($actual-ne$expected[$year]){throw "Structured FDA $year reconciliation failed: expected $($expected[$year]), got $actual."}}
$manifest=[ordered]@{snapshotCode=$SnapshotCode;parserVersion='fda-first-generic-html-v1.1';sources=$records};$path=Join-Path $snapshot 'm9-fda-first-generic-html-manifest.private.json';[IO.File]::WriteAllText($path,($manifest|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false));Write-Host ($manifest|ConvertTo-Json -Depth 8)

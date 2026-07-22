[CmdletBinding(SupportsShouldProcess,ConfirmImpact='High')]
param([string]$PlanPath='.\config\research-reference-source-plan.json',[string]$DestinationRoot='.\data\private\reference')
$ErrorActionPreference='Stop'; trap { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }
$plan=Get-Content -LiteralPath $PlanPath -Raw|ConvertFrom-Json
if($plan.destinationRoot-cne'data/private/reference'-or $DestinationRoot-cne'.\data\private\reference'){throw'Exact private reference destination is required.'}
foreach($source in $plan.sources){if(([uri]$source.officialPage).Host-notin @('www.fda.gov','open.fda.gov','www.nlm.nih.gov')){throw"Non-authoritative source host: $($source.code)"}}
Write-Host "Plan: $($plan.version)"; $plan.sources|ForEach-Object{Write-Host "$($_.code): $($_.role) [$($_.snapshot)]"}
if(-not$PSCmdlet.ShouldProcess($DestinationRoot,'Acquire immutable authoritative snapshots, hash them, and create a private validation report')){return}
throw 'Confirmed acquisition is intentionally blocked until a human records immutable release-specific download URLs/versions and approves their initial SHA-256 hashes. Existing snapshots will never be overwritten.'

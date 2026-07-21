[CmdletBinding()]
param([string]$StudyCode='synthetic-peer-exposure',[Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$ValidationRunCode)
$ErrorActionPreference='Stop';$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path;$python=Join-Path $root '.venv\Scripts\python.exe'
if($StudyCode-ne'synthetic-peer-exposure'){throw 'Milestone 7 automation accepts only the deterministic synthetic study.'};if(-not(Test-Path -LiteralPath $python)){throw 'Run scripts/setup-python-validation.ps1 first.'}
$bundle=Join-Path $root "artifacts\causal-validation\$ValidationRunCode";$reports=Join-Path $root "artifacts\reports\milestone-7\$ValidationRunCode"
& dotnet run --no-build --project "$root\src\PharmaAccess.Worker\PharmaAccess.Worker.csproj" -- export-m7-synthetic "$root\artifacts\causal-validation" $ValidationRunCode
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE};& $python -m pharma_access_validation.cli --bundle $bundle --reports $reports
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE};Write-Host "Milestone 7 synthetic validation passed. Bundle: $bundle Reports: $reports"

[CmdletBinding()]
param([Parameter(Mandatory)][string]$BundlePath,[Parameter(Mandatory)][string]$ReportPath)
$ErrorActionPreference='Stop';$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path;$python=Join-Path $root '.venv\Scripts\python.exe'
if(-not(Test-Path -LiteralPath $python)){throw 'Run scripts/setup-python-validation.ps1 first.'}
& $python -m pharma_access_validation.cli --bundle $BundlePath --reports $ReportPath
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}

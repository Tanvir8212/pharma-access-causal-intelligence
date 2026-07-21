[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$version = & python -c "import sys; print('.'.join(map(str, sys.version_info[:3])))"
if (-not $version.StartsWith('3.12.')) { throw "Python 3.12 is required; found $version" }
$venv = Join-Path $root '.venv'
if (-not (Test-Path -LiteralPath $venv)) { & python -m venv $venv }
$python = Join-Path $venv 'Scripts\python.exe'
& $python -m pip install --upgrade 'pip==25.3'
if ($LASTEXITCODE -ne 0) { throw 'Failed to install the pinned pip version.' }
& $python -m pip install -e "$root\python[test]"
if ($LASTEXITCODE -ne 0) { throw 'Failed to resolve/install Python validation dependencies.' }
& $python -m pip freeze --all | Sort-Object | Set-Content -LiteralPath "$root\python\requirements.lock" -Encoding utf8
& $python -m pip check
if ($LASTEXITCODE -ne 0) { throw 'pip check failed.' }
Write-Host "Python validation environment ready: $(& $python --version)"

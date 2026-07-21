[CmdletBinding()]
param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$FreezeCode='m8-synthetic-freeze-v1')
$ErrorActionPreference='Stop';$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path;$pythonLock=Join-Path $root 'python\requirements.lock';$packages=Join-Path $root 'Directory.Packages.props';if(-not(Test-Path -LiteralPath $pythonLock)){throw 'Python requirements lock is required.'}
& dotnet run --no-build --no-restore --project "$root\src\PharmaAccess.Worker\PharmaAccess.Worker.csproj" -- freeze-m8-synthetic "$root\artifacts\research-freezes" $FreezeCode $pythonLock $packages
if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}

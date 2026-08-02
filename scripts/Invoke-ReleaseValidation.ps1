[CmdletBinding()]
param([switch]$SkipContainers)
$ErrorActionPreference='Stop';$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path;Push-Location $root
try {
    & dotnet restore .\PharmaAccess.sln --locked-mode;if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
    & dotnet build .\PharmaAccess.sln --configuration Release --no-restore;if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
    & dotnet test .\PharmaAccess.sln --configuration Release --no-build --no-restore;if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
    $python=Join-Path $root '.venv\Scripts\python.exe';if(-not(Test-Path $python)){throw 'Python validation environment is missing; run scripts/setup-python-validation.ps1.'}
    & $python -m pytest python/tests;if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
    & $PSScriptRoot\Test-ReleaseContents.ps1 -RepositoryOnly
    & git diff --check;if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
    if(-not $SkipContainers){if(-not(Get-Command docker -ErrorAction SilentlyContinue)){throw 'Docker is unavailable; rerun with -SkipContainers only when recording that limitation.'};$sha=(& git rev-parse --verify HEAD).Trim();$timestamp=[DateTimeOffset]::UtcNow.ToString('O');foreach($target in @(@('src/PharmaAccess.Api/Dockerfile','pharmaaccess-api:local'),@('src/PharmaAccess.Web/Dockerfile','pharmaaccess-web:local'))){& docker build --build-arg APP_VERSION=0.0.0-local --build-arg COMMIT_SHA=$sha --build-arg BUILD_TIMESTAMP=$timestamp -f $target[0] -t $target[1] .;if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}};& $PSScriptRoot\Test-ReleaseContents.ps1;& $PSScriptRoot\Test-ContainerSmoke.ps1}
}
finally{Pop-Location}

[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & dotnet test .\PharmaAccess.sln --configuration Release --no-restore --filter 'Category=LocalResearchIntegration'
    if($LASTEXITCODE-ne 0){exit $LASTEXITCODE}
}
finally { Pop-Location }

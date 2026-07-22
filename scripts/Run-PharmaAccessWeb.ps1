[CmdletBinding()]
param()
$ErrorActionPreference='Stop';$root=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path;$web=Join-Path $root 'src\PharmaAccess.Web\PharmaAccess.Web.csproj';$api=Join-Path $root 'src\PharmaAccess.Api\PharmaAccess.Api.csproj'
function Native([string]$name){if($LASTEXITCODE-ne0){throw "$name failed with exit code $LASTEXITCODE."}}
$secrets=@(dotnet user-secrets list --project $api);Native 'Reading User Secrets';if(-not($secrets-match'^ConnectionStrings:PharmaAccess\s*=\s*.+')){throw 'Configure ConnectionStrings:PharmaAccess in PharmaAccess.Api User Secrets before launching.'}
dotnet restore $web;Native 'Web restore';dotnet build $web --no-restore;Native 'Web build'
Write-Host 'Home:      http://localhost:5187/'
Write-Host 'Dashboard: http://localhost:5187/Dashboard'
Write-Host 'Ask:       http://localhost:5187/Ask'
Write-Host 'Health:    http://localhost:5187/health'
Write-Host 'HTTPS:     https://localhost:7187/'
dotnet run --no-build --project $web --urls 'http://localhost:5187;https://localhost:7187';Native 'Web application'

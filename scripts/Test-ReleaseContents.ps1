[CmdletBinding()]
param(
    [switch]$RepositoryOnly,
    [string[]]$Images = @('pharmaaccess-api:local', 'pharmaaccess-web:local')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    & git diff --exit-code -- README.md
    if ($LASTEXITCODE -ne 0) { throw 'README.md differs from the checked-in version.' }

    $trackedForbidden = & git ls-files -- 'docs/**' 'artifacts/**' 'data/private/**' 'prompts/**' 'reports/**' 'transcripts/**' '*.log' '.env'
    if ($trackedForbidden) { throw "Forbidden release files are tracked: $($trackedForbidden -join ', ')" }

    $secretPatterns = '(?i)(AIza[0-9A-Za-z_-]{30,}|password\s*[:=]\s*[^$\s{][^\s]*|Server=[^;]+;.*Password=)'
    $publicFiles = & git ls-files -- '*.cs' '*.csproj' '*.props' '*.json' '*.yml' '*.yaml' '*.ps1' 'Dockerfile' '*Dockerfile' '.env.example'
    foreach ($file in $publicFiles) {
        $matches=Select-String -LiteralPath $file -Pattern $secretPatterns
        if ($matches|Where-Object Line -NotMatch '123456789012345678901234567890') { throw "Potential embedded secret in $file" }
    }
    if ($RepositoryOnly) { return }

    foreach ($image in $Images) {
        $user = & docker image inspect $image --format '{{.Config.User}}'
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($user) -or $user -eq 'root' -or $user -eq '0') { throw "$image does not declare a non-root user." }
        $ports = & docker image inspect $image --format '{{json .Config.ExposedPorts}}'
        if ($ports -ne '{"8080/tcp":{}}') { throw "$image exposes unexpected ports: $ports" }
        $history = & docker history --no-trunc $image
        if ($history -match $secretPatterns) { throw "$image history contains a potential secret." }
        $container = (& docker create $image).Trim()
        $archive = Join-Path ([IO.Path]::GetTempPath()) ("pharmaaccess-" + [Guid]::NewGuid().ToString('N') + '.tar')
        try {
            & docker export --output $archive $container
            $entries = & tar -tf $archive
            if ($entries -match '(^|/)(docs|artifacts|data/private|prompts|reports|transcripts)(/|$)') { throw "$image contains forbidden content." }
            if ($entries -match '(^|/)src(/|$)|\.csproj$') { throw "$image contains source or project files." }
        }
        finally { & docker rm $container | Out-Null; Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue }
    }
}
finally { Pop-Location }

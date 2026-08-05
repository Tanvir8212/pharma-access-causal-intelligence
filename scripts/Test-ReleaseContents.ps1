[CmdletBinding()]
param(
    [switch]$RepositoryOnly,
    [string[]]$Images = @('pharmaaccess-api:local', 'pharmaaccess-web:local')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path

function Remove-EnvironmentReferences {
    param([AllowEmptyString()][string]$Text)

    return [regex]::Replace(
        $Text,
        '\$\{[A-Za-z_][A-Za-z0-9_]*(?:(?::-|:\?|\?)[^}\r\n]*)?\}',
        ''
    )
}

$secretPatterns = @(
    [pscustomobject]@{ Name = 'Gemini API key'; Pattern = '(?i)AIza[0-9A-Za-z_-]{30,}' }
    [pscustomobject]@{ Name = 'literal password'; Pattern = '(?i)password\s*[:=]\s*[^$\s{][^\s]*' }
    [pscustomobject]@{ Name = 'connection string password'; Pattern = '(?i)Server=[^;]+;.*Pass(?:word)=' }
)

function Assert-NoSecrets {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Lines,
        [switch]$AllowKnownPlaceholder
    )

    for ($index = 0; $index -lt $Lines.Count; $index++) {
        $sanitizedLine = Remove-EnvironmentReferences -Text ([string]$Lines[$index])
        if ($AllowKnownPlaceholder -and $sanitizedLine -match '123456789012345678901234567890') { continue }

        foreach ($secretPattern in $secretPatterns) {
            if ($sanitizedLine -match $secretPattern.Pattern) {
                $lineNumber = $index + 1
                throw "Potential embedded secret in $Source at line $lineNumber (matched pattern: $($secretPattern.Name))."
            }
        }
    }
}

Push-Location $root
try {
    & git diff --exit-code -- README.md
    if ($LASTEXITCODE -ne 0) { throw 'README.md differs from the checked-in version.' }

    $trackedForbidden = & git ls-files -- 'docs/**' 'artifacts/**' 'data/private/**' 'prompts/**' 'reports/**' 'transcripts/**' '*.log' '.env'
    if ($trackedForbidden) { throw "Forbidden release files are tracked: $($trackedForbidden -join ', ')" }

    $publicFiles = & git ls-files -- '*.cs' '*.csproj' '*.props' '*.json' '*.yml' '*.yaml' '*.ps1' 'Dockerfile' '*Dockerfile' '.env.example'
    foreach ($file in $publicFiles) {
        Assert-NoSecrets -Source $file -Lines @(Get-Content -LiteralPath $file) -AllowKnownPlaceholder
    }
    if ($RepositoryOnly) { return }

    foreach ($image in $Images) {
        $user = & docker image inspect $image --format '{{.Config.User}}'
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($user) -or $user -eq 'root' -or $user -eq '0') { throw "$image does not declare a non-root user." }
        $ports = & docker image inspect $image --format '{{json .Config.ExposedPorts}}'
        if ($ports -ne '{"8080/tcp":{}}') { throw "$image exposes unexpected ports: $ports" }
        $history = & docker history --no-trunc $image
        Assert-NoSecrets -Source "Docker image history for $image" -Lines @($history)
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

Set-StrictMode -Version Latest

function ConvertTo-NormalizedResearchSourceHash {
    param([Parameter(Mandatory)][string]$Hash)
    $Hash.Trim().ToUpperInvariant()
}

function Test-ResearchSourceAssignment {
    param(
        [Parameter(Mandatory)][string]$PrivateRoot,
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)][string]$ExpectedHash
    )

    $inheritedGlobalWhatIf = $global:WhatIfPreference
    $global:WhatIfPreference = $false
    try {
        $platformRelativePath = $RelativePath.Replace('/', [IO.Path]::DirectorySeparatorChar)
        $candidatePath = Join-Path -Path $PrivateRoot -ChildPath $platformRelativePath
        # These are integrity reads. Suppress inherited -WhatIf so path resolution
        # and hashing are identical in preview and confirmed modes.
        $resolvedPath = (Resolve-Path -LiteralPath $candidatePath -ErrorAction Stop).Path
        $normalizedExpected = ConvertTo-NormalizedResearchSourceHash -Hash $ExpectedHash
        $normalizedActual = ConvertTo-NormalizedResearchSourceHash -Hash ((Get-FileHash -LiteralPath $resolvedPath -Algorithm SHA256 -ErrorAction Stop).Hash)
        $status = if ($normalizedActual -ceq $normalizedExpected) { 'Match' } else { 'Changed' }
        [pscustomobject]@{ RelativePath=$RelativePath; Status=$status; ExpectedHash=$normalizedExpected; ActualHash=$normalizedActual }
    }
    catch [System.Management.Automation.ItemNotFoundException] {
        return [pscustomobject]@{ RelativePath=$RelativePath; Status='Missing'; ExpectedHash=$null; ActualHash=$null }
    }
    finally { $global:WhatIfPreference = $inheritedGlobalWhatIf }
}

Export-ModuleMember -Function ConvertTo-NormalizedResearchSourceHash,Test-ResearchSourceAssignment

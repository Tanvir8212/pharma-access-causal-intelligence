$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\scripts\modules\ResearchSourceIntegrity.psm1') -Force

function Assert-Equal($Expected,$Actual,[string]$Name) { if ($Expected -cne $Actual) { throw "$Name expected '$Expected' but got '$Actual'." } }
$root = Join-Path ([IO.Path]::GetTempPath()) ('pharma source integrity ' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $root 'folder with spaces') | Out-Null
try {
    $relative = 'folder with spaces/source file.csv'
    $file = Join-Path $root ($relative.Replace('/',[IO.Path]::DirectorySeparatorChar))
    [IO.File]::WriteAllText($file,"synthetic,private-like`n1,test`n")
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash

    Assert-Equal 'Match' (Test-ResearchSourceAssignment $root $relative $hash).Status 'uppercase hash'
    Assert-Equal 'Match' (Test-ResearchSourceAssignment $root $relative $hash.ToLowerInvariant()).Status 'lowercase hash'
    Assert-Equal 'Match' (Test-ResearchSourceAssignment $root $relative ("  $hash `t")).Status 'whitespace hash'

    $WhatIfPreference = $true
    $before = [IO.File]::ReadAllText($file)
    $preview = Test-ResearchSourceAssignment $root $relative $hash
    Assert-Equal 'Match' $preview.Status 'WhatIf hash verification'
    Assert-Equal $before ([IO.File]::ReadAllText($file)) 'WhatIf persistence guard'
    $WhatIfPreference = $false

    Assert-Equal 'Changed' (Test-ResearchSourceAssignment $root $relative ('0' * 64)).Status 'changed file'
    Assert-Equal 'Missing' (Test-ResearchSourceAssignment $root 'missing/file.csv' $hash).Status 'missing file'
    $messages = @((Test-ResearchSourceAssignment $root $relative $hash) | Out-String)
    if ($messages -match 'Source hash changed') { throw 'Matching hash emitted a false changed-hash message.' }
    'ResearchSourceIntegrity tests: PASS (9 assertions)'
}
finally {
    $WhatIfPreference = $false
    $global:WhatIfPreference = $false
    $resolvedRoot = [IO.Path]::GetFullPath($root)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedRoot.StartsWith($resolvedTemp,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolvedRoot) -notlike 'pharma source integrity *') { throw "Refusing to remove unexpected test path: $resolvedRoot" }
    Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
}

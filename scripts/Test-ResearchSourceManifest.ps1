[CmdletBinding()]
param(
    [string]$PrivateRoot = '.\data\private',
    [string]$ManifestPath = '.\data\private\mappings\research-sources.private.json'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'modules\ResearchSourceIntegrity.psm1') -Force
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$checked = $matches = $changed = $missing = 0
foreach ($assignment in $manifest.assignments) {
    $result = Test-ResearchSourceAssignment -PrivateRoot $PrivateRoot -RelativePath ([string]$assignment.relativePath) -ExpectedHash ([string]$assignment.sha256)
    $checked++
    switch ($result.Status) {
        'Match' { $matches++ }
        'Changed' { $changed++; Write-Output "Changed relative source: $($result.RelativePath); expected $($result.ExpectedHash); actual $($result.ActualHash)" }
        'Missing' { $missing++; Write-Output "Missing relative source: $($result.RelativePath)" }
    }
}
Write-Output "Checked: $checked"
Write-Output "Matched: $matches"
Write-Output "Changed: $changed"
Write-Output "Missing: $missing"
if ($changed -ne 0 -or $missing -ne 0) { exit 1 }

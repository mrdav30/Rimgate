<#
.SYNOPSIS
Restores and verifies pinned public mod references for portable builds.
.DESCRIPTION
Selects version-owned entries from .assets/build-references.json, downloads
missing DLLs and checks every cached file's SHA-256. A hash mismatch fails
rather than replacing the file. Unknown targets fail before creating a cache.
These references are build inputs, not player-package dependencies.
.PARAMETER Destination
Reference cache root. Defaults to .assets/references/<version> under the repository.
An explicit relative path is resolved against the caller's working directory.
.PARAMETER RimWorldVersion
Major.minor reference target. Defaults to 1.6; 1.7 contains experimental compile
inputs and does not establish game compatibility or a public support declaration.
.EXAMPLE
.\.assets\scripts\restore-build-references.ps1
Restores the pinned 1.6 references into the default ignored version-owned cache.
.EXAMPLE
.\.assets\scripts\restore-build-references.ps1 -RimWorldVersion 1.7
Restores the experimental 1.7 compile dependencies independently of 1.6.
#>
param (
    [string]$Destination,
    [ValidatePattern('^\d+\.\d+$')]
    [string]$RimWorldVersion = '1.6'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$references = @(Get-Content -LiteralPath (Join-Path $repoRoot '.assets/build-references.json') -Raw |
    ConvertFrom-Json | Where-Object { ($_.path -split '/')[2] -eq $RimWorldVersion })
if (!$references.Count) { throw "No pinned build references configured for RimWorld $RimWorldVersion." }
if (!$Destination) { $Destination = Join-Path $repoRoot ".assets/references/$RimWorldVersion" }
$Destination = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
foreach ($reference in $references) {
    $path = Join-Path $Destination $reference.path
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        Invoke-WebRequest -UseBasicParsing -Uri $reference.url -OutFile $path
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $reference.sha256) {
        throw "Reference hash mismatch: $path. Remove the cached file and restore again."
    }
}
Write-Host "Verified build references: $Destination"

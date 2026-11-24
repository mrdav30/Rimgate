<#
.SYNOPSIS
Restores and verifies pinned public mod references for portable builds.
.DESCRIPTION
Reads .assets/build-references.json, downloads missing DLLs and checks every
cached file's SHA-256. A hash mismatch fails rather than replacing the file.
These references are build inputs, not player-package dependencies.
.PARAMETER Destination
Reference cache root. Defaults to .assets/references under the repository.
An explicit relative path is resolved against the caller's working directory.
.EXAMPLE
.\.assets\scripts\restore-build-references.ps1
Restores the pinned references into the default ignored cache.
#>
param (
    [string]$Destination
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$Destination) { $Destination = Join-Path $repoRoot '.assets/references' }
$Destination = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
$references = Get-Content -LiteralPath (Join-Path $repoRoot '.assets/build-references.json') -Raw | ConvertFrom-Json
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

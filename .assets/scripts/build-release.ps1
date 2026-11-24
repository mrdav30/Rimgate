<#
.SYNOPSIS
Builds Rimgate bundles and its assembly for a supported version.
.DESCRIPTION
Validates the target, runs the asset build, then builds the C# assembly.
Replaces generated local outputs and writes the standard root build logs.
Does not create a published release, upload files or enable experimental 1.7.
.PARAMETER Configuration
Assembly configuration, Debug or Release. Defaults to Release.
.PARAMETER UnityPath
Unity.exe override for the configured builder editor. Defaults to the standard
Unity Hub installation of the version recorded in ProjectVersion.txt.
.PARAMETER RimWorldVersion
Supported major.minor game target. Defaults to 1.6.
.PARAMETER RimWorldManagedPath
Installed 1.6 RimWorldWin64_Data/Managed override. Defaults to the project's
sibling game-data directory when omitted.
.PARAMETER SteamModPath
Workshop content root above 294100. Defaults to the project's Steam library.
.EXAMPLE
.\.assets\scripts\build-release.ps1 -RimWorldVersion 1.6 -Configuration Release
Rebuilds 1.6 bundles and the release assembly locally.
#>
param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$UnityPath,
    [ValidatePattern('^\d+\.\d+$')]
    [string]$RimWorldVersion = '1.6',
    [string]$RimWorldManagedPath,
    [string]$SteamModPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build-common.ps1')
$null = Get-RimgateBuildTarget -RimWorldVersion $RimWorldVersion

Write-Host 'Step 1/2: Building asset bundles...'
& (Join-Path $PSScriptRoot 'build-assetbundle.ps1') -UnityPath $UnityPath -RimWorldVersion $RimWorldVersion

Write-Host 'Step 2/2: Building .NET assembly...'
& (Join-Path $PSScriptRoot 'build-assembly.ps1') -Configuration $Configuration -RimWorldVersion $RimWorldVersion -RimWorldManagedPath $RimWorldManagedPath -SteamModPath $SteamModPath

Write-Host "Local release build complete for RimWorld $RimWorldVersion."

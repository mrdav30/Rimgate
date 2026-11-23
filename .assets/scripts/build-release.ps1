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

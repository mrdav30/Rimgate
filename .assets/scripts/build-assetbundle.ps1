param (
    [string]$UnityPath,
    [ValidatePattern('^\d+\.\d+$')]
    [string]$RimWorldVersion = '1.6'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build-common.ps1')
$target = Get-RimgateBuildTarget -RimWorldVersion $RimWorldVersion
$projectPath = Join-Path $target.RepoRoot 'UnityAssetBuilder'
$versionFile = Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt'
$unityVersion = (Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion: (.+)$').Matches.Groups[1].Value
if (!$UnityPath) {
    $UnityPath = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$unityVersion/Editor/Unity.exe"
}
if (!(Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $unityVersion not found at $UnityPath. Supply -UnityPath to the matching editor."
}

Write-Host 'Preparing assets...'
foreach ($assetKind in @('Textures', 'Sounds')) {
    $rawPath = Join-Path $target.RepoRoot "AssetsRaw/${assetKind}Raw"
    if (!(Test-Path -LiteralPath $rawPath -PathType Container)) {
        throw "Missing raw asset directory: $rawPath"
    }
}
foreach ($assetKind in @('Textures', 'Sounds')) {
    $rawPath = Join-Path $target.RepoRoot "AssetsRaw/${assetKind}Raw"
    $destination = [System.IO.Path]::GetFullPath((Join-Path $projectPath "Assets/Data/Mrdav30.Rimgate/$assetKind"))
    $generatedRoot = [System.IO.Path]::GetFullPath((Join-Path $projectPath 'Assets/Data/Mrdav30.Rimgate')) + [System.IO.Path]::DirectorySeparatorChar
    if (!$destination.StartsWith($generatedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Asset staging path escapes the generated asset directory: $destination"
    }
    if (Test-Path -LiteralPath $destination) {
        Remove-Item -LiteralPath $destination -Recurse -Force
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $rawPath -Destination $destination -Recurse
}

$logPath = Join-Path $target.RepoRoot 'build-assetbundle.log'
$unityArgs = "-batchmode -quit -projectPath `"$projectPath`" -executeMethod BuildRimgateBundles.BuildBundlesFromCommandLine -rimgateVersion $RimWorldVersion -logFile `"$logPath`""
Write-Host "Building AssetBundle for RimWorld $RimWorldVersion with Unity $unityVersion..."
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -PassThru -Wait -WindowStyle Hidden
if ($unityProcess.ExitCode -ne 0) {
    throw "AssetBundle build failed (exit $($unityProcess.ExitCode)). See $logPath"
}
foreach ($output in @('rimgate_core', 'rimgate_core.manifest', 'AssetBundles', 'AssetBundles.manifest')) {
    if (!(Test-Path -LiteralPath (Join-Path $target.BundlePath $output) -PathType Leaf)) {
        throw "Unity returned success without producing $output. See $logPath"
    }
}
Write-Host "AssetBundle build complete: $($target.BundlePath)"

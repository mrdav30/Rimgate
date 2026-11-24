<#
.SYNOPSIS
Bakes a native asset pack for the prepared experimental RimWorld 1.7 package.
.DESCRIPTION
Stages AssetsRaw into the ignored 1.7 overlay, runs Waylith's own upload baker
without showing a window, checks its exit/output, then removes loose inputs and
legacy bundles from that candidate. Preserves the tracked 1.6 assets and builder.
Requires setup-rimbridge-dev.ps1 to have prepared the experimental overlay.
.PARAMETER GamePath
Installed or isolated 1.7 game root containing RimWorld.exe and Version.txt.
Defaults to the D: Steam installation; setup passes its private game root.
.PARAMETER ToolsPath
External development root. Uses Profile-1.7-Bake there for isolated cache/config.
Defaults to LOCALAPPDATA/RimgateDev. Cannot be inside the repository or game root.
.PARAMETER RimWorldVersion
Only experimental 1.7 is accepted. This does not declare player compatibility.
.EXAMPLE
.\.assets\scripts\build-waylith-assets.ps1 -RimWorldVersion 1.7
Bakes the already prepared experimental overlay with the installed Waylith game.
#>
[CmdletBinding()]
param (
    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\RimWorld',
    [string]$ToolsPath = (Join-Path $env:LOCALAPPDATA 'RimgateDev'),
    [ValidateSet('1.7')]
    [string]$RimWorldVersion = '1.7'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path
$ToolsPath = [IO.Path]::GetFullPath($ToolsPath)
foreach ($protectedRoot in @($repoRoot, $GamePath)) {
    if ($ToolsPath.Equals($protectedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $ToolsPath.StartsWith($protectedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ToolsPath must be outside the repository and game installation.'
    }
}
if (Get-Process RimWorldWin64, RimWorld -ErrorAction SilentlyContinue) {
    throw 'Stop RimWorld before baking the experimental package.'
}
$version = (Get-Content -LiteralPath (Join-Path $GamePath 'Version.txt') -Raw).Trim()
[xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'Source/1.7/Rimgate/Rimgate.csproj') -Raw
$reference = $project.Project.ItemGroup.PackageReference | Where-Object Include -EQ 'Krafs.Rimworld.Ref'
if ($version -notmatch '^1\.7\.' -or
    [regex]::Match($reference.Version, '^\d+\.\d+\.\d+').Value -ne [regex]::Match($version, '^\d+\.\d+\.\d+').Value) {
    throw 'Native baking requires the installed 1.7 build to match the experimental RimRef pin.'
}
$stageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '.assets/experimental/1.7/Mod'))
$contentRoot = Join-Path $stageRoot '1.7'
if (!(Test-Path -LiteralPath (Join-Path $stageRoot 'LoadFolders.xml')) -or
    !(Test-Path -LiteralPath (Join-Path $contentRoot 'Assemblies/Rimgate.dll'))) {
    throw 'Prepare the experimental 1.7 overlay with setup-rimbridge-dev.ps1 first.'
}
if ((Get-Item -LiteralPath $stageRoot).Attributes -band [IO.FileAttributes]::ReparsePoint -or
    (Get-ChildItem -LiteralPath $stageRoot -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | Select-Object -First 1)) {
    throw 'Refusing to bake or clean a linked experimental package.'
}
foreach ($kind in @('Textures', 'Sounds')) {
    $rawPath = Join-Path $repoRoot "AssetsRaw/${kind}Raw"
    if (!(Test-Path -LiteralPath $rawPath -PathType Container)) { throw "Missing raw input: $rawPath" }
}
foreach ($kind in @('Textures', 'Sounds')) {
    $destination = [IO.Path]::GetFullPath((Join-Path $contentRoot $kind))
    if (!$destination.StartsWith($contentRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Asset staging escaped the experimental content root.' }
    if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
    Copy-Item -LiteralPath (Join-Path $repoRoot "AssetsRaw/${kind}Raw") -Destination $destination -Recurse
}
$bakeProfile = Join-Path $ToolsPath 'Profile-1.7-Bake'
New-Item -ItemType Directory -Path $bakeProfile -Force | Out-Null
foreach ($file in @('Assets.zip', 'Assets.zip.stamps', 'Assets.zip.roles', 'Assets.zip.fingerprint')) {
    $path = Join-Path $contentRoot $file
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}
$logPath = Join-Path $repoRoot 'build-waylith-assets.log'
$arguments = @('-NoWindow', '-WindowSize=1280,720',
    ('"-savedatafolder={0}"' -f $bakeProfile), ('"-BakeModAssetPacks={0}"' -f $stageRoot))
$timer = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath (Join-Path $GamePath 'RimWorld.exe') -WorkingDirectory $GamePath `
    -ArgumentList $arguments -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput $logPath -RedirectStandardError (Join-Path $repoRoot 'build-waylith-assets-stderr.log')
# Retain the handle before waiting so Windows PowerShell 5.1 can read ExitCode.
$null = $process.Handle
if (!$process.WaitForExit(600000)) {
    $process.Kill()
    throw "Native asset bake exceeded ten minutes; inspect $logPath"
}
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Native asset bake failed (exit $($process.ExitCode)); inspect $logPath" }
foreach ($file in @('Assets.zip', 'Assets.zip.stamps', 'Assets.zip.roles', 'Assets.zip.fingerprint')) {
    $path = Join-Path $contentRoot $file
    if (!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
        throw "Native baker returned success without a nonempty $file."
    }
}
foreach ($kind in @('Textures', 'Sounds', 'AssetBundles')) {
    $destination = [IO.Path]::GetFullPath((Join-Path $contentRoot $kind))
    if (!$destination.StartsWith($contentRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Asset cleanup escaped the experimental content root.' }
    if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
}
Write-Host "Native 1.7 pack built in $([math]::Round($timer.Elapsed.TotalSeconds, 2)) seconds: $(Join-Path $contentRoot 'Assets.zip')"

<#
.SYNOPSIS
Prepares an isolated version-owned RimWorld/RimBridge view or checks player config.
.DESCRIPTION
With RimWorld stopped, backs up normal ModsConfig/Prefs bytes, restores pinned
GABS/RimBridge tools, builds Rimgate, refreshes private mod deployments and writes
the isolated profile/config. Registers the user-level rimgate-dev MCP server.
Game data and required dependencies are junctions to the selected installation.
For 1.7, builds the development-only RimBridge companions and raw asset inventory
under the private game's BridgeTools directory. The hidden-profile companion
requests graphics redraws when occluded; it does not show or focus the window.
Stages retained XML followed by authored Mod/1.7 Defs/Patches for genuine
version differences; the public 1.6 load folders do not select that content.
Native mode launches a bounded hidden bake process, not a playable session.
Does not change the Steam branch.
.PARAMETER GamePath
Installed game root matching RimWorldVersion. Defaults to D:\SteamLibrary\steamapps\common\RimWorld.
Unused when VerifyPlayerConfig is selected.
.PARAMETER SteamModPath
Workshop content root above 294100. Defaults to the D: Steam library's
workshop/content directory. Unused in verify mode.
.PARAMETER ToolsPath
Mutable development root for Game-<version>, Profile-<version>, tools, evidence and GABS
configuration. Defaults to LOCALAPPDATA/RimgateDev. Use a fresh external root
when selecting a different installation; existing junctions are not retargeted.
.PARAMETER VerifyPlayerConfig
Only compare normal config existence/hashes against ToolsPath's existing
player-config-baseline.json. Does not refresh setup or restore changed files.
.PARAMETER RimWorldVersion
1.6 or experimental 1.7. Defaults to 1.6 until public 1.7 acceptance. The 1.7
view uses RimWorld.exe and a private metadata overlay, with no support bump in Mod.
Waylith runs with -NoWindow and a fixed 1280x720 framebuffer for background
rendering and synthetic input. This is not graphics-free -Headless mode.
.PARAMETER AssetFormat
Defaults to WaylithPack for experimental 1.7 and UnityBundles for retained 1.6.
UnityBundles remains an explicit 1.7 comparison override.
WaylithPack bakes staged raw textures/audio using the game's native baker and
deploys pack-only content. This option does not change the retained 1.6 assets.
.EXAMPLE
.\.assets\scripts\setup-rimbridge-dev.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\RimWorld' -ToolsPath 'C:\RimgateDev16'
Prepares an isolated view of the installed 1.6 game and registers it with Codex.
.EXAMPLE
.\.assets\scripts\setup-rimbridge-dev.ps1 -RimWorldVersion 1.7
Builds, natively bakes and stages the experimental 1.7 package with Waylith.
.EXAMPLE
.\.assets\scripts\setup-rimbridge-dev.ps1 -ToolsPath 'C:\RimgateDev16' -VerifyPlayerConfig
After game shutdown, checks normal config against that view's backed-up baseline.
#>
[CmdletBinding()]
param (
    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\RimWorld',
    [string]$SteamModPath = 'D:\SteamLibrary\steamapps\workshop\content',
    [string]$ToolsPath = (Join-Path $env:LOCALAPPDATA 'RimgateDev'),
    [switch]$VerifyPlayerConfig,
    [ValidateSet('1.6', '1.7')]
    [string]$RimWorldVersion = '1.6',
    [ValidateSet('UnityBundles', 'WaylithPack')]
    [string]$AssetFormat = $(if ($RimWorldVersion -eq '1.7') { 'WaylithPack' } else { 'UnityBundles' })
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$ToolsPath = [IO.Path]::GetFullPath($ToolsPath)
$playerConfigBaselinePath = Join-Path $ToolsPath 'player-config-baseline.json'
if ($VerifyPlayerConfig) {
    $baseline = Get-Content -LiteralPath $playerConfigBaselinePath -Raw | ConvertFrom-Json
    foreach ($file in $baseline.files) {
        $exists = Test-Path -LiteralPath $file.path
        if ($exists -ne $file.existed -or ($exists -and (Get-FileHash -LiteralPath $file.path -Algorithm SHA256).Hash -ne $file.sha256)) {
            throw "Player config changed: $($file.path). Review the original backup at $($baseline.backupDirectory) before restoring; do not overwrite concurrent player edits."
        }
    }
    Write-Host "Player configuration matches the baseline. Backup: $($baseline.backupDirectory)"
    return
}
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path
$gameVersion = (Get-Content -LiteralPath (Join-Path $GamePath 'Version.txt') -Raw).Trim()
if ($gameVersion -notmatch ('^' + [regex]::Escape($RimWorldVersion) + '\.')) {
    throw "This development profile requires a RimWorld $RimWorldVersion installation; found $gameVersion."
}
$experimental = $RimWorldVersion -eq '1.7'
if (!$experimental -and $AssetFormat -ne 'UnityBundles') { throw 'WaylithPack is limited to the experimental 1.7 deployment.' }
$executable = if ($experimental) { 'RimWorld.exe' } else { 'RimWorldWin64.exe' }
$processName = [IO.Path]::GetFileNameWithoutExtension($executable)
if (!(Test-Path -LiteralPath (Join-Path $GamePath $executable) -PathType Leaf)) {
    throw "Missing game executable: $executable"
}
if ($experimental) {
    $runtime = Get-Content -LiteralPath (Join-Path $GamePath 'RimWorld.runtimeconfig.json') -Raw | ConvertFrom-Json
    if ($runtime.runtimeOptions.tfm -ne 'net10.0') { throw 'Expected the inspected .NET 10 Waylith runtime layout.' }
    [xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'Source/1.7/Rimgate/Rimgate.csproj') -Raw
    $reference = $project.Project.ItemGroup.PackageReference | Where-Object Include -EQ 'Krafs.Rimworld.Ref'
    if ([regex]::Match($reference.Version, '^\d+\.\d+\.\d+').Value -ne [regex]::Match($gameVersion, '^\d+\.\d+\.\d+').Value) {
        throw 'The experimental RimRef pin must match the installed game build before staging.'
    }
}
foreach ($protectedRoot in @($GamePath, $repoRoot)) {
    if ($ToolsPath.Equals($protectedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $ToolsPath.StartsWith($protectedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ToolsPath must be outside the game installation and repository.'
    }
}
$privateGame = Join-Path $ToolsPath "Game-$RimWorldVersion"
$profile = Join-Path $ToolsPath "Profile-$RimWorldVersion"
$gameId = 'rimgate' + $RimWorldVersion.Replace('.', '')
if (Get-Process RimWorldWin64, RimWorld -ErrorAction SilentlyContinue) {
    throw 'Stop RimWorld before refreshing the development deployment and taking the player-config backup.'
}

function Install-ReleaseArchive($Name, $Version, $FileName, $Sha256) {
    $archive = Join-Path $ToolsPath $FileName
    if (!(Test-Path -LiteralPath $archive)) {
        Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/pardeike/$Name/releases/download/$Version/$FileName" -OutFile $archive
    }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $Sha256) {
        throw "Release archive hash mismatch: $archive"
    }
    $packagePath = Join-Path $ToolsPath "$Name-$Version"
    if (!(Test-Path -LiteralPath $packagePath)) {
        Expand-Archive -LiteralPath $archive -DestinationPath $packagePath
    }
}

function Ensure-Junction($Path, $Target) {
    if (Test-Path -LiteralPath $Path) {
        $existing = Get-Item -LiteralPath $Path
        if ($existing.LinkType -ne 'Junction' -or [IO.Path]::GetFullPath($existing.Target) -ne [IO.Path]::GetFullPath($Target)) {
            throw "Existing path is not the expected development junction: $Path"
        }
    } else {
        New-Item -ItemType Junction -Path $Path -Target $Target | Out-Null
    }
}

New-Item -ItemType Directory -Path $ToolsPath, $privateGame, (Join-Path $privateGame 'Mods'),
    (Join-Path $profile 'Config'), (Join-Path $ToolsPath 'gabs'), (Join-Path $ToolsPath 'evidence') -Force | Out-Null
$backupDirectory = Join-Path $ToolsPath ('evidence/player-config-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backupDirectory | Out-Null
$normalConfig = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config'
$files = foreach ($name in @('ModsConfig.xml', 'Prefs.xml')) {
    $path = Join-Path $normalConfig $name
    $existed = Test-Path -LiteralPath $path
    $hash = $null
    if ($existed) {
        Copy-Item -LiteralPath $path -Destination $backupDirectory
        $hash = (Get-FileHash -LiteralPath (Join-Path $backupDirectory $name) -Algorithm SHA256).Hash
    }
    @{ path = $path; existed = $existed; sha256 = $hash }
}
@{ backupDirectory = $backupDirectory; files = @($files) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $playerConfigBaselinePath -Encoding UTF8
Install-ReleaseArchive 'GABS' 'v1.1.2' 'gabs-v1.1.2-windows-amd64.zip' 'c0337277789162657bcc577ed6a559136f3c1b0c38e074ae7cf5cb1223b3a3d8'
Install-ReleaseArchive 'RimBridgeServer' 'v2.5.0' 'RimBridgeServer.zip' '4b31843ff94e74a99d740916cabae82a3c17a96b6b31b668ab348cd53976859c'
$gabs = Join-Path $ToolsPath 'GABS-v1.1.2/gabs-v1.1.2-windows-amd64/gabs.exe'

$buildArgs = @{ RimWorldVersion = $RimWorldVersion; SteamModPath = $SteamModPath }
if ($experimental) { $buildArgs.Experimental = $true }
else { $buildArgs.RimWorldManagedPath = Join-Path $GamePath 'RimWorldWin64_Data/Managed' }
& (Join-Path $PSScriptRoot 'build-assembly.ps1') @buildArgs
$gameFiles = if ($experimental) { @(Get-ChildItem -LiteralPath $GamePath -File | Select-Object -ExpandProperty Name) }
    else { @('RimWorldWin64.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'steam_appid.txt', 'Version.txt') }
foreach ($file in $gameFiles) {
    Copy-Item -LiteralPath (Join-Path $GamePath $file) -Destination $privateGame -Force
}
$gameFolders = if ($experimental) { @('Data', 'Rendering', 'vulkan') } else { @('Data', 'RimWorldWin64_Data', 'MonoBleedingEdge') }
foreach ($folder in $gameFolders) {
    Ensure-Junction (Join-Path $privateGame $folder) (Join-Path $GamePath $folder)
}
if ($experimental) {
    $probeOutput = Join-Path $privateGame 'BridgeTools/Rimgate'
    New-Item -ItemType Directory -Path $probeOutput -Force | Out-Null
    & dotnet build (Join-Path $repoRoot '.assets/tools/RimgateAssetProbe/RimgateAssetProbe.csproj') `
        --configuration Release --output $probeOutput "-p:RimWorld17ManagedDir=$GamePath"
    if ($LASTEXITCODE -ne 0) { throw 'Development asset probe build failed.' }
    $inventory = @{ assetFormat = $AssetFormat }
    foreach ($kind in @('Textures', 'Sounds')) {
        $rawRoot = Join-Path $repoRoot "AssetsRaw/${kind}Raw"
        $extension = if ($kind -eq 'Textures') { '*.png' } else { '*.ogg' }
        $inventory[$kind.ToLowerInvariant()] = @(Get-ChildItem -LiteralPath $rawRoot -Filter $extension -File -Recurse |
            Sort-Object FullName | ForEach-Object {
                $relative = $_.FullName.Substring($rawRoot.Length + 1).Replace('\', '/')
                $relative.Substring(0, $relative.Length - $_.Extension.Length)
            })
    }
    [IO.File]::WriteAllText((Join-Path $probeOutput 'asset-manifest.json'),
        ($inventory | ConvertTo-Json -Depth 3), [Text.UTF8Encoding]::new($false))
}
Ensure-Junction (Join-Path $privateGame 'Mods/Harmony') (Join-Path $SteamModPath '294100/2009463077')
Ensure-Junction (Join-Path $privateGame 'Mods/VanillaExpandedFramework') (Join-Path $SteamModPath '294100/2023507013')

$rimgateSource = Join-Path $repoRoot 'Mod'
if ($experimental) {
    $rimgateSource = Join-Path $repoRoot '.assets/experimental/1.7/Mod'
    # Only the private test package declares 1.7 and reuses the retained XML/bundles.
    if (Test-Path -LiteralPath $rimgateSource) {
        if ((Get-Item -LiteralPath $rimgateSource).Attributes -band [IO.FileAttributes]::ReparsePoint -or
            (Get-ChildItem -LiteralPath $rimgateSource -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | Select-Object -First 1)) {
            throw 'Refusing to replace linked experimental package content.'
        }
        $expectedPackage = [IO.Path]::GetFullPath((Join-Path $repoRoot '.assets/experimental/1.7/Mod'))
        if ([IO.Path]::GetFullPath($rimgateSource) -ne $expectedPackage) { throw 'Unexpected experimental package path.' }
        Remove-Item -LiteralPath $rimgateSource -Recurse -Force
    }
    New-Item -ItemType Directory -Path (Join-Path $rimgateSource '1.7/Assemblies') -Force | Out-Null
    foreach ($folder in @('About', 'Languages')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot "Mod/$folder") -Destination $rimgateSource -Recurse
    }
    foreach ($folder in @('Defs', 'Patches', 'AssetBundles')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot "Mod/1.6/$folder") -Destination (Join-Path $rimgateSource '1.7') -Recurse
    }
    foreach ($folder in @('Defs', 'Patches')) {
        $versionContent = Join-Path $repoRoot "Mod/1.7/$folder"
        if (Test-Path -LiteralPath $versionContent) {
            Get-ChildItem -LiteralPath $versionContent |
                Copy-Item -Destination (Join-Path $rimgateSource "1.7/$folder") -Recurse -Force
        }
    }
    foreach ($file in @('Rimgate.dll', 'Rimgate.xml')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot ".assets/experimental/1.7/Assemblies/$file") -Destination (Join-Path $rimgateSource '1.7/Assemblies')
    }
    [xml]$about = Get-Content -LiteralPath (Join-Path $rimgateSource 'About/About.xml') -Raw
    $supportedVersions = $about.SelectSingleNode('/ModMetaData/supportedVersions')
    $supportedVersions.RemoveAll()
    $versionNode = $about.CreateElement('li'); $versionNode.InnerText = '1.7'
    $null = $supportedVersions.AppendChild($versionNode)
    $about.Save((Join-Path $rimgateSource 'About/About.xml'))
    Set-Content -LiteralPath (Join-Path $rimgateSource 'LoadFolders.xml') -Encoding UTF8 -Value '<loadFolders><v1.7><li>/</li><li>1.7</li></v1.7></loadFolders>'
    if ($AssetFormat -eq 'WaylithPack') {
        & (Join-Path $PSScriptRoot 'build-waylith-assets.ps1') -GamePath $privateGame -ToolsPath $ToolsPath
    }
}
foreach ($deployment in @(
    @{ Name = 'Rimgate'; Source = $rimgateSource },
    @{ Name = 'RimBridgeServer'; Source = (Join-Path $ToolsPath 'RimBridgeServer-v2.5.0/RimBridgeServer') }
)) {
    $destination = [IO.Path]::GetFullPath((Join-Path $privateGame "Mods/$($deployment.Name)"))
    $modsBoundary = [IO.Path]::GetFullPath((Join-Path $privateGame 'Mods')) + '\'
    if (!$destination.StartsWith($modsBoundary, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Deployment escapes the isolated Mods folder: $destination"
    }
    if (Test-Path -LiteralPath $destination) {
        if ((Get-Item -LiteralPath $destination).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Refusing to replace a linked deployment: $destination"
        }
        if (Get-ChildItem -LiteralPath $destination -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | Select-Object -First 1) {
            throw "Refusing to replace linked deployment content: $destination"
        }
        Remove-Item -LiteralPath $destination -Recurse -Force
    }
    Copy-Item -LiteralPath $deployment.Source -Destination $destination -Recurse
}

$mods = @('brrainz.harmony', 'ludeon.rimworld', 'ludeon.rimworld.royalty', 'ludeon.rimworld.ideology',
    'ludeon.rimworld.biotech', 'ludeon.rimworld.anomaly', 'ludeon.rimworld.odyssey',
    'oskarpotocki.vanillafactionsexpanded.core', 'mrdav30.rimgate', 'brrainz.rimbridgeserver')
$modList = ($mods | ForEach-Object { "<li>$_</li>" }) -join ''
Set-Content -LiteralPath (Join-Path $profile 'Config/ModsConfig.xml') -Encoding UTF8 -Value "<ModsConfigData><version>$RimWorldVersion</version><activeMods>$modList</activeMods><knownExpansions /></ModsConfigData>"
if (!(Test-Path -LiteralPath (Join-Path $profile 'Config/Prefs.xml'))) {
    Set-Content -LiteralPath (Join-Path $profile 'Config/Prefs.xml') -Encoding UTF8 -Value '<PrefsData><devMode>true</devMode><runInBackground>true</runInBackground><fullscreen>false</fullscreen><langFolderName>English</langFolderName><volumeGame>0</volumeGame><volumeMusic>0</volumeMusic></PrefsData>'
}
$launchArgs = if ($experimental) { @("-savedatafolder=$profile", '-NoWindow', '-WindowSize=1280,720') }
    else { @("-savedatafolder=$profile", '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '-logFile', (Join-Path $ToolsPath 'evidence/Player.log')) }
$config = @{
    version = '1.0'
    toolNormalization = @{ enableOpenAINormalization = $true; maxToolNameLength = 64; preserveOriginalName = $true }
    stripOutputSchema = $true
    timeouts = @{ startup = @{ processStartSeconds = 10; gabpConnectSeconds = 300 } }
    games = @{ $gameId = @{
        id = $gameId; name = "Rimgate isolated RimWorld $RimWorldVersion"; launchMode = 'DirectPath'
        target = (Join-Path $privateGame $executable); workingDir = $privateGame
        stopProcessName = $processName; defaultProfile = 'dev'
        profiles = @{ dev = @{
            description = 'Isolated Rimgate development saves and minimal mod list'
            args = @($launchArgs)
        } }
    } }
}
[IO.File]::WriteAllText((Join-Path $ToolsPath 'gabs/config.json'), ($config | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
& $gabs games doctor $gameId --configDir (Join-Path $ToolsPath 'gabs')
if ($LASTEXITCODE -ne 0) { throw 'GABS configuration validation failed.' }
& codex mcp add rimgate-dev -- $gabs server --configDir (Join-Path $ToolsPath 'gabs')
if ($LASTEXITCODE -ne 0) { throw 'Codex MCP registration failed.' }
Write-Host "Ready: use the rimgate-dev MCP server and gameId $gameId. Isolated data: $profile"

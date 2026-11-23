param (
    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\RimWorld',
    [string]$SteamModPath = 'D:\SteamLibrary\steamapps\workshop\content',
    [string]$ToolsPath = (Join-Path $env:LOCALAPPDATA 'RimgateDev'),
    [switch]$VerifyPlayerConfig
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$ToolsPath = [IO.Path]::GetFullPath($ToolsPath)
$baselinePath = Join-Path $ToolsPath 'player-config-baseline.json'
if ($VerifyPlayerConfig) {
    $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
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
if ((Get-Content -LiteralPath (Join-Path $GamePath 'Version.txt') -Raw) -notmatch '^1\.6\.') {
    throw 'This development profile requires a RimWorld 1.6 installation.'
}
foreach ($protectedRoot in @($GamePath, $repoRoot)) {
    if ($ToolsPath.Equals($protectedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $ToolsPath.StartsWith($protectedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'ToolsPath must be outside the game installation and repository.'
    }
}
$privateGame = Join-Path $ToolsPath 'Game-1.6'
if (Get-Process RimWorldWin64 -ErrorAction SilentlyContinue) {
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
    (Join-Path $ToolsPath 'Profile-1.6/Config'), (Join-Path $ToolsPath 'gabs'), (Join-Path $ToolsPath 'evidence') -Force | Out-Null
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
@{ backupDirectory = $backupDirectory; files = @($files) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $baselinePath -Encoding UTF8
Install-ReleaseArchive 'GABS' 'v1.1.2' 'gabs-v1.1.2-windows-amd64.zip' 'c0337277789162657bcc577ed6a559136f3c1b0c38e074ae7cf5cb1223b3a3d8'
Install-ReleaseArchive 'RimBridgeServer' 'v2.5.0' 'RimBridgeServer.zip' '4b31843ff94e74a99d740916cabae82a3c17a96b6b31b668ab348cd53976859c'
$gabs = Join-Path $ToolsPath 'GABS-v1.1.2/gabs-v1.1.2-windows-amd64/gabs.exe'

& (Join-Path $PSScriptRoot 'build-assembly.ps1') -RimWorldVersion 1.6 -RimWorldManagedPath (Join-Path $GamePath 'RimWorldWin64_Data/Managed') -SteamModPath $SteamModPath
foreach ($file in @('RimWorldWin64.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'steam_appid.txt', 'Version.txt')) {
    Copy-Item -LiteralPath (Join-Path $GamePath $file) -Destination $privateGame -Force
}
foreach ($folder in @('Data', 'RimWorldWin64_Data', 'MonoBleedingEdge')) {
    Ensure-Junction (Join-Path $privateGame $folder) (Join-Path $GamePath $folder)
}
Ensure-Junction (Join-Path $privateGame 'Mods/Harmony') (Join-Path $SteamModPath '294100/2009463077')
Ensure-Junction (Join-Path $privateGame 'Mods/VanillaExpandedFramework') (Join-Path $SteamModPath '294100/2023507013')

foreach ($deployment in @(
    @{ Name = 'Rimgate'; Source = (Join-Path $repoRoot 'Mod') },
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

$profile = Join-Path $ToolsPath 'Profile-1.6'
$mods = @('brrainz.harmony', 'ludeon.rimworld', 'ludeon.rimworld.royalty', 'ludeon.rimworld.ideology',
    'ludeon.rimworld.biotech', 'ludeon.rimworld.anomaly', 'ludeon.rimworld.odyssey',
    'oskarpotocki.vanillafactionsexpanded.core', 'mrdav30.rimgate', 'brrainz.rimbridgeserver')
$modList = ($mods | ForEach-Object { "<li>$_</li>" }) -join ''
Set-Content -LiteralPath (Join-Path $profile 'Config/ModsConfig.xml') -Encoding UTF8 -Value "<ModsConfigData><version>1.6</version><activeMods>$modList</activeMods><knownExpansions /></ModsConfigData>"
if (!(Test-Path -LiteralPath (Join-Path $profile 'Config/Prefs.xml'))) {
    Set-Content -LiteralPath (Join-Path $profile 'Config/Prefs.xml') -Encoding UTF8 -Value '<PrefsData><devMode>true</devMode><runInBackground>true</runInBackground><fullScreen>false</fullScreen><langFolderName>English</langFolderName><volumeGame>0</volumeGame><volumeMusic>0</volumeMusic></PrefsData>'
}
$config = @{
    version = '1.0'
    toolNormalization = @{ enableOpenAINormalization = $true; maxToolNameLength = 64; preserveOriginalName = $true }
    stripOutputSchema = $true
    timeouts = @{ startup = @{ processStartSeconds = 10; gabpConnectSeconds = 300 } }
    games = @{ rimgate16 = @{
        id = 'rimgate16'; name = 'Rimgate isolated RimWorld 1.6'; launchMode = 'DirectPath'
        target = (Join-Path $privateGame 'RimWorldWin64.exe'); workingDir = $privateGame
        stopProcessName = 'RimWorldWin64'; defaultProfile = 'dev'
        profiles = @{ dev = @{
            description = 'Isolated Rimgate development saves and minimal mod list'
            args = @("-savedatafolder=$profile", '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '-logFile', (Join-Path $ToolsPath 'evidence/Player.log'))
        } }
    } }
}
[IO.File]::WriteAllText((Join-Path $ToolsPath 'gabs/config.json'), ($config | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
& $gabs games doctor rimgate16 --configDir (Join-Path $ToolsPath 'gabs')
if ($LASTEXITCODE -ne 0) { throw 'GABS configuration validation failed.' }
& codex mcp add rimgate-dev -- $gabs server --configDir (Join-Path $ToolsPath 'gabs')
if ($LASTEXITCODE -ne 0) { throw 'Codex MCP registration failed.' }
Write-Host "Ready: use the rimgate-dev MCP server and gameId rimgate16. Isolated data: $profile"

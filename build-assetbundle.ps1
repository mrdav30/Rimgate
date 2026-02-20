param (
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
)

$ProjectPath = "UnityAssetBuilder"
$RawTexturePath = "AssetsRaw/TexturesRaw"
$RawSoundPath = "AssetsRaw/SoundsRaw"
$UnityTexturePath = "UnityAssetBuilder/Assets/Data/Mrdav30.Rimgate/Textures"
$UnitySoundPath = "UnityAssetBuilder/Assets/Data/Mrdav30.Rimgate/Sounds"
$LogPath = "build-assetbundle.log"

Write-Host "Preparing assets..."

# Clean previous assets
if (Test-Path $UnityTexturePath) {
    Remove-Item $UnityTexturePath -Recurse -Force
}

if (Test-Path $UnitySoundPath) {
    Remove-Item $UnitySoundPath -Recurse -Force
}

Copy-Item $RawTexturePath -Destination $UnityTexturePath -Recurse
Copy-Item $RawSoundPath -Destination $UnitySoundPath -Recurse

Write-Host "Building AssetBundle..."

# Run Unity in batch mode and wait for completion
$unityArgs = "-batchmode -quit -projectPath `"$ProjectPath`" -executeMethod BuildRimgateBundles.BuildBundles -logFile `"$LogPath`""
$unityProcess = Start-Process -FilePath "$UnityPath" -ArgumentList $unityArgs -PassThru

# Wait for Unity and its child processes to finish
while ((Get-WmiObject -Class Win32_Process | Where-Object {
    $_.ParentProcessID -eq $unityProcess.Id -and $_.Name -ne 'VBCSCompiler.exe'
}).Count -gt 0) {
    Start-Sleep -Seconds 1
}

# Final check and exit
if (!$unityProcess.HasExited) {
    Wait-Process -Id $unityProcess.Id
}

if ($unityProcess.ExitCode -eq 0) {
    Write-Host "AssetBundle build complete."
} else {
    Write-Host "Build failed. Check build.log"
    exit 1
}

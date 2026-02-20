param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$assetBundleScript = Join-Path $scriptRoot "build-assetbundle.ps1"
$assemblyScript = Join-Path $scriptRoot "build-assembly.ps1"

if (!(Test-Path $assetBundleScript)) {
    Write-Host "Missing script: $assetBundleScript"
    exit 1
}

if (!(Test-Path $assemblyScript)) {
    Write-Host "Missing script: $assemblyScript"
    exit 1
}

Write-Host "Step 1/2: Building asset bundles..."
& $assetBundleScript -UnityPath $UnityPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Asset bundle build failed."
    exit $LASTEXITCODE
}

Write-Host "Step 2/2: Building .NET assembly..."
& $assemblyScript -Configuration $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Assembly build failed."
    exit $LASTEXITCODE
}

Write-Host "Release build complete."

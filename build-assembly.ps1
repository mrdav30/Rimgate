param (
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$ProjectPath = "Source/Rimgate/Rimgate.csproj",
    [string]$ModAssemblyPath = "Mod/1.6/Assemblies",
    [string]$LogPath = "build-dotnet.log"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$projectFullPath = Join-Path $scriptRoot $ProjectPath
$modAssemblyFullPath = Join-Path $scriptRoot $ModAssemblyPath
$logFullPath = Join-Path $scriptRoot $LogPath

if (!(Test-Path $projectFullPath)) {
    Write-Host "Project file not found: $projectFullPath"
    exit 1
}

if (!(Test-Path $modAssemblyFullPath)) {
    New-Item -Path $modAssemblyFullPath -ItemType Directory -Force | Out-Null
}

if (Test-Path $logFullPath) {
    Remove-Item $logFullPath -Force
}

$projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFullPath)
$dllName = "$projectName.dll"

Write-Host "Building $projectName ($Configuration)..."

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue

if ($dotnetCommand) {
    & $dotnetCommand.Source build `
        $projectFullPath `
        /t:Build `
        /p:Configuration=$Configuration `
        /p:Platform=AnyCPU `
        /nologo `
        /verbosity:minimal 2>&1 | Tee-Object -FilePath $logFullPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Check $LogPath"
        exit $LASTEXITCODE
    }
}
else {
    Write-Host "dotnet was not found in PATH."
    exit 1
}

$candidateDlls = @(
    (Join-Path $scriptRoot ("Source/Assemblies/{0}" -f $dllName)),
    (Join-Path $scriptRoot ("Source/Rimgate/bin/{0}/{1}" -f $Configuration, $dllName)),
    (Join-Path $scriptRoot ("Source/Rimgate/bin/AnyCPU/{0}/{1}" -f $Configuration, $dllName))
)

$builtDllPath = $candidateDlls | Where-Object { Test-Path $_ } | Select-Object -First 1

if (!$builtDllPath) {
    Write-Host "Build completed but could not find $dllName in expected output paths."
    Write-Host "Checked:"
    $candidateDlls | ForEach-Object { Write-Host " - $_" }
    exit 1
}

$destinationDllPath = Join-Path $modAssemblyFullPath $dllName
Copy-Item $builtDllPath $destinationDllPath -Force

Write-Host "Assembly build complete."
Write-Host "Copied: $destinationDllPath"

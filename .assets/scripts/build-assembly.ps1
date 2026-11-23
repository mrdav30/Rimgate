param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidatePattern('^\d+\.\d+$')]
    [string]$RimWorldVersion = '1.6',
    [string]$RimWorldManagedPath,
    [string]$SteamModPath,
    [switch]$UseReferenceAssemblies,
    [string]$LogPath = 'build-dotnet.log'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build-common.ps1')
$target = Get-RimgateBuildTarget -RimWorldVersion $RimWorldVersion
$logFullPath = Join-Path $target.RepoRoot $LogPath
$dotnetCommand = Get-Command dotnet -ErrorAction Stop

$buildArgs = @(
    'build', $target.ProjectPath, '-c', $Configuration,
    "-p:RimWorldVersion=$RimWorldVersion",
    "-p:OutDir=$($target.AssemblyPath)/",
    '-p:Platform=AnyCPU', '--nologo', '--verbosity:minimal'
)
if ($RimWorldManagedPath) {
    $buildArgs += "-p:RefDir=$($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RimWorldManagedPath))"
}
if ($SteamModPath) {
    $buildArgs += "-p:SteamModDir=$($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SteamModPath))"
}
if ($UseReferenceAssemblies) {
    $buildArgs += '-p:UseReferenceAssemblies=true'
}

Write-Host "Building Rimgate for RimWorld $RimWorldVersion ($Configuration)..."
& $dotnetCommand.Source @buildArgs 2>&1 | Tee-Object -FilePath $logFullPath
if ($LASTEXITCODE -ne 0) {
    throw "Assembly build failed (exit $LASTEXITCODE). See $logFullPath"
}
if (!(Test-Path -LiteralPath (Join-Path $target.AssemblyPath 'Rimgate.dll') -PathType Leaf)) {
    throw "Build returned success without producing $($target.AssemblyPath)/Rimgate.dll."
}
Write-Host "Assembly build complete: $($target.AssemblyPath)/Rimgate.dll"

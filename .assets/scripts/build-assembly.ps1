<#
.SYNOPSIS
Builds the Rimgate assembly for a selected RimWorld version.
.DESCRIPTION
Builds supported 1.6 into Mod/1.6/Assemblies. The opt-in experimental 1.7
project uses pinned RimRef signatures and writes to .assets/experimental/1.7/Assemblies.
Checks the dotnet exit code and expected DLL; does not build assets or publish.
.PARAMETER Configuration
Debug or Release. Defaults to Release.
.PARAMETER RimWorldVersion
Major.minor game target. Defaults to supported 1.6; 1.7 requires Experimental.
.PARAMETER RimWorldManagedPath
Installed 1.6 RimWorldWin64_Data/Managed directory. Overrides the project's
sibling game-data default. Omit for portable references and experimental 1.7.
.PARAMETER SteamModPath
Workshop content root, above 294100, or the restored .assets/references/<version> root.
Required for experimental 1.7; use matching packages with 1.7 dependency folders.
Supported 1.6 otherwise uses the project's default Steam library.
.PARAMETER UseReferenceAssemblies
Use pinned public RimRef signatures for portable 1.6 builds. Mod references
must still be available through SteamModPath. Experimental 1.7 always uses RimRef.
.PARAMETER Experimental
Allow only the separate unvalidated 1.7 assembly project. Does not declare
support, modify player metadata or enable experimental asset/release builds.
.PARAMETER LogPath
Build log path relative to the repository root. Defaults to build-dotnet.log.
.EXAMPLE
.\.assets\scripts\build-assembly.ps1 -UseReferenceAssemblies -SteamModPath .assets/references/1.6
Builds 1.6 with public references after running restore-build-references.ps1.
.EXAMPLE
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.7 -Experimental -SteamModPath 'D:\SteamLibrary\steamapps\workshop\content'
Compiles the experimental port using the installed packages' 1.7 folders.
#>
param (
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidatePattern('^\d+\.\d+$')]
    [string]$RimWorldVersion = '1.6',
    [string]$RimWorldManagedPath,
    [string]$SteamModPath,
    [switch]$UseReferenceAssemblies,
    [switch]$Experimental,
    [string]$LogPath = 'build-dotnet.log'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build-common.ps1')
$target = Get-RimgateBuildTarget -RimWorldVersion $RimWorldVersion -Experimental:$Experimental
if ($Experimental -and $RimWorldManagedPath) {
    throw 'The experimental 1.7 project uses pinned RimRef assemblies; do not pass installed 1.6 references.'
}
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

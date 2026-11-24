<#
.SYNOPSIS
Provides shared version/source/output resolution for Rimgate build scripts.
.DESCRIPTION
Dot-source this file to load Get-RimgateBuildTarget. Normal resolution requires
supported metadata, load folders, content and a version-owned project.
Experimental resolution is limited to the separate 1.7 assembly project.
.EXAMPLE
. .\.assets\scripts\build-common.ps1
Get-RimgateBuildTarget -RimWorldVersion 1.6
Loads the helper and resolves supported 1.6 inputs and outputs without building.
#>


function Get-RimgateBuildTarget {
    <#
    .SYNOPSIS
    Resolves and validates a version-owned Rimgate build target.
    .DESCRIPTION
    Returns RepoRoot, ProjectPath, AssemblyPath and BundlePath. Experimental
    assemblies stay outside Mod, and their BundlePath is null.
    .PARAMETER RimWorldVersion
    Major.minor target to resolve. Required; normal builds must declare support.
    .PARAMETER Experimental
    Resolve only the unvalidated 1.7 assembly project without requiring public
    support metadata or Mod/1.7 content. Does not enable asset/release builds.
    .EXAMPLE
    Get-RimgateBuildTarget -RimWorldVersion 1.7 -Experimental
    Resolves the experimental project and its ignored assembly output directory.
    #>
    param (
        [Parameter(Mandatory)]
        [ValidatePattern('^\d+\.\d+$')]
        [string]$RimWorldVersion,
        [switch]$Experimental
    )

    $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $projectPath = Join-Path $repoRoot "Source/$RimWorldVersion/Rimgate/Rimgate.csproj"
    if ($Experimental) {
        if ($RimWorldVersion -ne '1.7' -or !(Test-Path -LiteralPath $projectPath -PathType Leaf)) {
            throw 'Only the version-owned 1.7 project is configured for experimental assembly builds.'
        }
        return [pscustomobject]@{
            RepoRoot = $repoRoot
            ProjectPath = $projectPath
            AssemblyPath = Join-Path $repoRoot '.assets/experimental/1.7/Assemblies'
            BundlePath = $null
        }
    }
    [xml]$about = Get-Content -LiteralPath (Join-Path $repoRoot 'Mod/About/About.xml') -Raw
    [xml]$loadFolders = Get-Content -LiteralPath (Join-Path $repoRoot 'Mod/LoadFolders.xml') -Raw
    $versionFolder = Join-Path $repoRoot "Mod/$RimWorldVersion"
    $loadEntry = $loadFolders.loadFolders.SelectSingleNode("v$RimWorldVersion")
    if ($about.ModMetaData.supportedVersions.li -notcontains $RimWorldVersion -or
        !$loadEntry -or @($loadEntry.li) -notcontains $RimWorldVersion -or
        !(Test-Path -LiteralPath $versionFolder -PathType Container)) {
        throw "RimWorld $RimWorldVersion is not configured in About.xml, LoadFolders.xml and Mod/$RimWorldVersion. No compatibility is implied by the version parameter."
    }

    if (!(Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "Missing source project for RimWorld ${RimWorldVersion}: $projectPath"
    }

    [pscustomobject]@{
        RepoRoot = $repoRoot
        ProjectPath = $projectPath
        AssemblyPath = Join-Path $versionFolder 'Assemblies'
        BundlePath = Join-Path $versionFolder 'AssetBundles'
    }
}

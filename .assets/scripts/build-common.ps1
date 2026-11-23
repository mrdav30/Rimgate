function Get-RimgateBuildTarget {
    param (
        [Parameter(Mandatory)]
        [ValidatePattern('^\d+\.\d+$')]
        [string]$RimWorldVersion
    )

    $repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    [xml]$about = Get-Content -LiteralPath (Join-Path $repoRoot 'Mod/About/About.xml') -Raw
    [xml]$loadFolders = Get-Content -LiteralPath (Join-Path $repoRoot 'Mod/LoadFolders.xml') -Raw
    $versionFolder = Join-Path $repoRoot "Mod/$RimWorldVersion"
    $loadEntry = $loadFolders.loadFolders.SelectSingleNode("v$RimWorldVersion")
    if ($about.ModMetaData.supportedVersions.li -notcontains $RimWorldVersion -or
        !$loadEntry -or @($loadEntry.li) -notcontains $RimWorldVersion -or
        !(Test-Path -LiteralPath $versionFolder -PathType Container)) {
        throw "RimWorld $RimWorldVersion is not configured in About.xml, LoadFolders.xml and Mod/$RimWorldVersion. No compatibility is implied by the version parameter."
    }

    $projectPath = Join-Path $repoRoot "Source/$RimWorldVersion/Rimgate/Rimgate.csproj"
    if (!(Test-Path -LiteralPath $projectPath -PathType Leaf) -and $RimWorldVersion -eq '1.6') {
        $projectPath = Join-Path $repoRoot 'Source/Rimgate/Rimgate.csproj'
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

<#
.SYNOPSIS
Builds the supported Rimgate library, DocFX guides and API reference.
.DESCRIPTION
Selects the highest supported version in About.xml, verifies/restores pinned
public references and the local DocFX tool, then builds the library and site.
Replaces generated docs/obj content, treats DocFX warnings as errors and runs
test-docs.ps1. Logs stay at the repository root. Does not deploy or publish.
.EXAMPLE
.\.assets\scripts\build-docs.ps1
Builds and validates the site locally without a Steam installation or Unity.
#>
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'build-common.ps1')
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[xml]$about = Get-Content -LiteralPath (Join-Path $repoRoot 'Mod/About/About.xml') -Raw
$version = ($about.ModMetaData.supportedVersions.li | Sort-Object { [version]$_ } -Descending | Select-Object -First 1)
$target = Get-RimgateBuildTarget -RimWorldVersion $version
$referenceRoot = Join-Path $repoRoot ".assets/references/$version"
$generatedRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'docs/obj'))
if (!$generatedRoot.StartsWith((Join-Path $repoRoot 'docs') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Generated documentation path escapes docs: $generatedRoot"
}
if (Test-Path -LiteralPath $generatedRoot) {
    if ((Get-Item -LiteralPath $generatedRoot).Attributes -band [IO.FileAttributes]::ReparsePoint -or
        (Get-ChildItem -LiteralPath $generatedRoot -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } | Select-Object -First 1)) {
        throw "Refusing to clean linked documentation output: $generatedRoot"
    }
    Remove-Item -LiteralPath $generatedRoot -Recurse -Force
}

& (Join-Path $PSScriptRoot 'restore-build-references.ps1') -RimWorldVersion $version -Destination $referenceRoot
& (Join-Path $PSScriptRoot 'build-assembly.ps1') -RimWorldVersion $version -UseReferenceAssemblies -SteamModPath $referenceRoot

Push-Location $repoRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Documentation tool restore failed.' }

    $inputPath = Join-Path $repoRoot 'docs/obj/assembly'
    New-Item -ItemType Directory -Path $inputPath -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $target.AssemblyPath 'Rimgate.dll'), (Join-Path $target.AssemblyPath 'Rimgate.xml') -Destination $inputPath -Force

    # DocFX resolves inherited game/mod types from assemblies beside Rimgate.dll.
    $cache = (& dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
    if ($LASTEXITCODE -ne 0) { throw 'Could not locate the NuGet package cache.' }
    [xml]$project = Get-Content -LiteralPath $target.ProjectPath -Raw
    foreach ($name in @('Krafs.Rimworld.Ref', 'Lib.Harmony.Ref')) {
        $package = $project.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $name }
        $packageRoot = Join-Path $cache.Trim() "$($name.ToLowerInvariant())/$($package.Version)"
        Get-ChildItem -LiteralPath $packageRoot -Recurse -Filter '*.dll' | Copy-Item -Destination $inputPath -Force
    }
    foreach ($package in $project.Project.ItemGroup.PackageDownload) {
        if (!$package.Include) { continue }
        $packageRoot = Join-Path $cache.Trim() "$($package.Include.ToLowerInvariant())/$($package.Version.Trim('[]'))/lib/netstandard2.0"
        Get-ChildItem -LiteralPath $packageRoot -Filter '*.dll' | Copy-Item -Destination $inputPath -Force
    }
    Get-ChildItem -LiteralPath $referenceRoot -Recurse -Filter '*.dll' | Copy-Item -Destination $inputPath -Force
    @{ _appFooter = "Rimgate documentation | RimWorld $version | main" } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $repoRoot 'docs/obj/site-metadata.json') -Encoding UTF8

    & dotnet tool run docfx docs/docfx.json --warningsAsErrors 2>&1 | Tee-Object -FilePath (Join-Path $repoRoot 'build-docs.log')
    if ($LASTEXITCODE -ne 0) { throw 'DocFX failed. See build-docs.log.' }
    & (Join-Path $PSScriptRoot 'test-docs.ps1')
} finally {
    Pop-Location
}

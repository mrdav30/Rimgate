<#
.SYNOPSIS
Validates the generated DocFX site before publication.
.DESCRIPTION
Requires expected guides/API pages and theme assets, rejects dependency
binaries, and verifies local HTML link targets stay inside the rendered site.
Reads existing outputs only; run build-docs.ps1 to generate them first.
.PARAMETER SitePath
Rendered site root. Defaults to docs/obj/_site under the repository.
An explicit relative path is resolved against the caller's working directory.
.EXAMPLE
.\.assets\scripts\test-docs.ps1
Checks the default generated site without rebuilding it.
#>
param ([string]$SitePath)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$SitePath) { $SitePath = Join-Path $repoRoot 'docs/obj/_site' }
$SitePath = (Resolve-Path -LiteralPath $SitePath).Path
foreach ($required in @('index.html', 'api/Rimgate.html', 'api/Rimgate.Building_Gate.html', 'api/Rimgate.RimgateModSettings.html', 'api/Global.CompProperties_GlyphParchment.html', 'guides/index.html', 'assets/ModIcon.png', 'public/main.css', 'public/main.js')) {
    if (!(Test-Path -LiteralPath (Join-Path $SitePath $required) -PathType Leaf)) { throw "Missing documentation output: $required" }
}
foreach ($guide in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'docs/guides') -Filter '*.md') {
    if (!(Test-Path -LiteralPath (Join-Path $SitePath "guides/$($guide.BaseName).html") -PathType Leaf)) { throw "Missing rendered guide: $($guide.Name)" }
}
$siteFiles = @(Get-ChildItem -LiteralPath $SitePath -Recurse -File)
if ($siteFiles | Where-Object { $_.Extension -in @('.dll', '.pdb', '.nupkg') }) {
    throw 'The published site must not contain dependency binaries.'
}
$published = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($file in $siteFiles) { $null = $published.Add($file.FullName.Substring($SitePath.Length + 1).Replace('\', '/')) }

$broken = [Collections.Generic.List[string]]::new()
foreach ($page in Get-ChildItem -LiteralPath $SitePath -Recurse -Filter '*.html') {
    $html = Get-Content -LiteralPath $page.FullName -Raw
    foreach ($match in [regex]::Matches($html, 'href="([^"]+)"')) {
        $href = [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
        if ($href -match '^[a-zA-Z][a-zA-Z\d+.-]*:' -or $href.StartsWith('//')) { continue }
        $path = [Uri]::UnescapeDataString(($href -split '[#?]', 2)[0])
        if (!$path) { continue }
        $basePath = if ($path.StartsWith('/')) { $SitePath } else { $page.DirectoryName }
        $target = [IO.Path]::GetFullPath((Join-Path $basePath $path.TrimStart('/')))
        if (!$target.StartsWith($SitePath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            $broken.Add("$($page.Name) -> $href (outside site)")
            continue
        }
        if (Test-Path -LiteralPath $target -PathType Container) { $target = Join-Path $target 'index.html' }
        $relative = $target.Substring($SitePath.Length + 1).Replace('\', '/')
        if (!$published.Contains($relative)) { $broken.Add("$($page.Name) -> $href") }
    }
}
if ($broken.Count) { throw "Broken site links:`n$($broken -join "`n")" }
Write-Host 'Documentation outputs, guide inventory, local links and binary exclusion passed.'

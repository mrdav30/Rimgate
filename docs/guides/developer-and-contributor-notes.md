# Developer and Contributor Notes

Build Rimgate for RimWorld **1.6**, then validate the gameplay you changed.
Rimgate is a public GitHub source project used locally; it has not been published
as a mod release or on Workshop. The release script produces local build outputs.

For gameplay, start at [Home](index.md). For dependencies and implemented integration
patches, see [Compatibility Matrix](compatibility-matrix.md). The repository's
`AGENTS.md` links the shared RimWorld skill and Rimgate specialization under
`.agents/skills/`; these files are versioned with the main repository.
For local skill discovery, link the two package directories into your Codex
user skills directory. Keep the repository copies authoritative so local
installation does not create a second set of instructions to maintain.

## Repository ownership

| Path | Owner/purpose |
| --- | --- |
| `Mod/About/About.xml`, `Mod/LoadFolders.xml` | Declared versions, dependencies and loaded folders |
| `Mod/1.6/Defs/`, `Mod/1.6/Patches/` | 1.6 gameplay XML and compatibility patches |
| `Source/1.6/Rimgate/Rimgate.csproj` | Retained 1.6 C# project, targeting .NET Framework 4.8 |
| `Source/1.7/Rimgate/Rimgate.csproj` | Experimental .NET 10/C# 14 port; compilation does not establish game compatibility |
| `Source/Rimgate.slnx` | XML solution with both version-owned projects; default solution builds select only 1.6 |
| `Mod/1.6/Assemblies/Rimgate.dll` | Generated 1.6 runtime assembly |
| `AssetsRaw/TexturesRaw/`, `AssetsRaw/SoundsRaw/` | Raw asset sources |
| `UnityAssetBuilder/` | Built-in-render-pipeline AssetBundle builder |
| `Mod/1.6/AssetBundles/` | Version-owned generated 1.6 bundles; four existing baseline files retained |
| `Mod/Languages/` | Shared localization |
| `docs/guides/` | Authored player and developer guides |
| `docs/feature-work/` | Repository-only feature overview, plans, issue tracker and performance signals; completed records under `done/` |
| `docs/docfx.json`, `docs/toc.yml`, `docs/templates/` | Authored DocFX site configuration, navigation and theme |
| `docs/obj/` | Ignored API metadata, documentation inputs and rendered site |
| `.assets/build-references.json` | Commit/hash pins for public mod build references |
| `.assets/scripts/` | PowerShell build/development tools and Lua runtime smoke cases |
| `.assets/experimental/` | Ignored experimental assembly outputs; excluded from the player package |

Bundles go to **`Mod/<version>/AssetBundles/`** (`Mod/1.6/AssetBundles/` today).
The shared root is loaded alongside `1.6` by `LoadFolders.xml`, so keep the old
`Mod/AssetBundles/` location empty. Do not put a second copy of the same assembly,
defs, patches or bundle in both loaded locations. The existing load configuration
already selects the version-owned bundle folder.

## Plan and track changes

Use the repository's
[feature work overview](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/feature-work-overview.md)
to find active, deferred and completed work. Significant new capabilities use a
focused plan alongside that overview, with observable outcomes, scope, affected
owners and relevant acceptance checks. Small fixes and routine documentation
edits can proceed without separate plans.

Record defects and investigations in the
[issue tracker](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/issue-tracker.md),
and actual timing, allocation, scaling or measurement-reliability signals in the
[performance backlog](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/performance-signal-backlog.md).
Features use descriptive plan names; issue IDs use `RG-Issue-NNN` and performance
IDs use `RG-Perf-NNN`. Assign those IDs at intake and preserve them after closure.
Link related records and external reports; keep each concern's status and
detailed evidence in its owning record.

Game investigations need the source revision, actual RimWorld build/platform,
DLC/mod versions and load order, settings, save or fresh-colony setup, expected
behavior and observed result. Confirm gameplay prerequisites and preserve the
original save. Include relevant startup, gameplay, interruption and save/load
checks; record missing evidence. Classify verified fixes, mitigations and
unreproduced/decision closures explicitly. Performance claims also need a
controlled baseline, units and repeated equivalent work; tick completion alone
does not measure performance.

Move completed plans into `docs/feature-work/done/` and update incoming/relative
links. Keep closed issue and performance indexes compact, linking the existing
completion record, regression or fix commit for detail. Preserve reproduction,
important output, decisions, uncertainty and reopening criteria in tracked text;
temporary logs/captures are supporting evidence. Completion normally ends at
implementation and relevant verification/documentation. Include publication only
when it is the requested outcome.

Guides describe current supported behavior. Dated work and verification history
stay in feature-work records, including the completed
[development tooling hardening](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/done/2026-11-23-development-tooling-hardening.md)
and [DocFX migration](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/done/2026-11-23-docfx-migration.md).
Tracking records are deliberately excluded from DocFX content and navigation.
Use GitHub source URLs, as above, when published guides link to them; a relative
link would target an unpublished site page. Archive dates and earlier green runs
do not establish a fresh validation run or a published mod release.

## Prerequisites

- RimWorld 1.6 and the DLC/mod dependencies listed in `Mod/About/About.xml`.
- A .NET SDK supporting the project's C# language level and .NET Framework 4.8
  compilation. The game/mod reference binaries must match the selected target.
- Build-time references to Vanilla Expanded Framework (`VEF.dll`, `KCSG.dll`)
  and Dubs Bad Hygiene (`BadHygiene.dll`). DBH is an optional runtime integration;
  its reference binary is still needed to compile the current project.
- Unity **6000.3.9f1**, as recorded in
  `UnityAssetBuilder/ProjectSettings/ProjectVersion.txt`, for bundles. The script
  reads that file to find the matching standard Hub installation. The earlier
  2022.3 LTS prose did not match this project.

Keep the builder on the built-in render pipeline, `StandaloneWindows64` and
`ChunkBasedCompression` (LZ4). No scenes are needed. Do not convert to URP/HDRP.
The configured editor version is not proof of asset compatibility with another
game version; verify loading in-game when changing the pipeline or assets.

## Build the selected version

Open `Source/Rimgate.slnx` for both projects in version folders. The experimental
1.7 project is excluded from default solution builds. The SDK-style projects
use their paths as solution identity and do not carry legacy `ProjectGuid` values.

From the repository root:

```powershell
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.6
.\.assets\scripts\build-assetbundle.ps1 -RimWorldVersion 1.6
.\.assets\scripts\build-release.ps1 -RimWorldVersion 1.6
```

All default to `1.6`. `-Configuration Debug` is available for assembly/full builds.
Each PowerShell script includes a synopsis, parameter help where applicable and
usage examples. Inspect a script without executing it with
`Get-Help .\.assets\scripts\build-assembly.ps1 -Full` or `-Examples`.
The release script builds bundles first, then the assembly; it does not publish.
Scripts in `.assets/scripts/` resolve repository inputs two levels above their
own directory, so invoking a script by its full path from another working
directory is also supported.

The asset wrapper passes `-rimgateVersion <version>` to Unity's parameterless
`BuildRimgateBundles.BuildBundlesFromCommandLine` entry point, which calls
`BuildBundles(string rimWorldVersion)`. The typed method derives
`Mod/<version>/AssetBundles/` from the builder project location and rejects an
invalid version format or missing version content folder before creating output.
The PowerShell wrapper owns the full metadata/load-folder/source validation.
There is no hard-coded 1.6 fallback or arbitrary output-directory override in the
Unity method. Use the wrapper for normal builds so those target checks run first.

The assembly project defaults to a sibling `../RimWorldWin64_Data/Managed`
directory relative to the repository and Workshop content under
`D:/SteamLibrary/steamapps/workshop/content`. Override local paths explicitly:

```powershell
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.6 -Configuration Debug `
  -RimWorldManagedPath 'D:\SteamLibrary\steamapps\common\RimWorld\RimWorldWin64_Data\Managed' `
  -SteamModPath 'D:\SteamLibrary\steamapps\workshop\content'
```

The full build accepts the same dependency overrides. Both asset/full builds
accept `-UnityPath` for the matching editor in a different location. Assembly logs
go to `build-dotnet.log` (override with `-LogPath`); Unity logs go to
`build-assetbundle.log`. Check the child exit code, log and expected outputs when
diagnosing a failure. The scripts stop on failed tools or missing outputs.
Both default build logs stay at the repository root and are gitignored, including
when the scripts are invoked from another working directory. The 1.6 game's redirected
`Player.log` stays under `ToolsPath/evidence`. Waylith 1.7 uses its application
`Logs/Player.log`; copy that log into external evidence after each test run.

Assembly output is fixed to the selected version's `Mod/<version>/Assemblies`;
the previous arbitrary `-ProjectPath` and `-ModAssemblyPath` parameters are
replaced by configured version/source selection. The script no longer searches
unrelated output directories for a fallback DLL or cleans the whole output folder.

## Build documentation and API reference

From the repository root, with the .NET 10 SDK installed:

```powershell
.\.assets\scripts\build-docs.ps1
dotnet tool run docfx serve docs/obj/_site
```

Open the local address printed by DocFX to preview the same site built in CI.
The build script works from another working directory when invoked by full path.
It restores the pinned local DocFX tool, verifies public mod references against
`.assets/build-references.json`, and builds the latest configured RimWorld target
using `Krafs.Rimworld.Ref`. A Steam/game installation, Workshop login and Unity
editor are not required for this build. Restored DLLs stay in the ignored
`.assets/references/<version>/` caches and are not published with the site or player mod.

For an assembly-only build using the same portable references:

```powershell
.\.assets\scripts\restore-build-references.ps1
.\.assets\scripts\build-assembly.ps1 -UseReferenceAssemblies -SteamModPath .assets/references/1.6
```

Ordinary assembly/release builds still use the installed reference paths described
above. The reference package targets RimWorld **1.6.4871**, with Harmony **2.3.6**;
the public VEF/KCSG and Bad Hygiene commits/hashes are recorded in the manifest.
Portable builds download pinned metadata-only dependency packages for DocFX's
transitive type resolution. These are not new player dependencies.

The C# project emits `Rimgate.xml` beside the DLL. DocFX reads that compiled
assembly and its available XML comments to generate public type/member pages;
it does not publish dependency assemblies or generate API pages for them. Missing
XML comments do not fail compilation, while malformed comments and DocFX warnings
remain errors. Add useful API comments when changing extension points, and keep
the related guide in the same PR.

The API configuration names the global namespace `Global` so the existing
unnamespaced `CompProperties_GlyphParchment` type also receives a page and valid
incoming links. This is a documentation setting; runtime namespaces stay intact.

The site has one current version: `main`, matching the latest supported target
in `Mod/About/About.xml`. Its footer is generated from that metadata. There are
no legacy wiki mirrors or versioned documentation snapshots. Author guides under
`docs/guides/`, include each in `docs/toc.yml`, and use explicit `.md` links or
API xrefs. `docs/obj/` is cleaned and regenerated so removed APIs/pages do not
linger. Build logs remain at the repository root (`build-dotnet.log` and
`build-docs.log`).

The Documentation workflow builds the library/site and validates generated local
links on every PR and push to `main`. Only successful builds of `main` deploy to
GitHub Pages. This repository's Pages source is **GitHub Actions**, **Wikis** are
disabled, and its website is `https://mrdav30.github.io/Rimgate/`. Configure the
equivalent Pages source when setting up a fork. The README documentation badge and
in-game Docs button point there. No commit, push or deployment is implied by a
local documentation edit.

## Version ownership and the 1.7 port

Only **1.6 is declared supported**. `.assets/scripts/build-common.ps1` checks `About.xml`,
`LoadFolders.xml`, the selected `Mod/<version>` folder and its source project
before running build tools. Passing `-RimWorldVersion 1.7` fails; it does not
create content, change support metadata or make the current code compatible.
Each project also rejects a direct MSBuild target belonging to the other version.

`Source/1.6/Rimgate/` retains the existing implementation and compiler settings;
`Source/1.7/Rimgate/` owns the .NET 10/C# 14 port. Each project compiles its own
source tree, with independent intermediates and outputs. Both retain assembly
name `Rimgate` and existing save/type identities. Avoid cross-version source
includes or project references; a change to 1.7 must not silently change 1.6.

An experimental 1.7 assembly build accepts a restored public reference root or
an explicit Workshop `content` root containing Harmony, VEF/KCSG and DBH under
their `1.7/Assemblies` folders. It uses pinned RimRef 1.7.5014-unstable for game signatures and the SDK's
normal .NET 10 references. It does not use the installed 1.6 game references:

```powershell
.\.assets\scripts\restore-build-references.ps1 -RimWorldVersion 1.7
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.7 -Experimental `
  -SteamModPath .assets/references/1.7 -LogPath build-experimental-1.7.log
```

Select packages with 1.7 assemblies matching the target being validated. The
manifest records commit URLs and SHA-256 for both versions. Restoration selects
only the requested version and defaults to its own cache; unknown targets fail
before writing files. CI compiles the experimental 1.7 project with those public
inputs in addition to building the supported 1.6 documentation site. Runtime
acceptance remains migration work. Outputs go to ignored
`.assets/experimental/1.7/Assemblies/`. There is no `Mod/1.7` support declaration,
experimental release/bundle switch or automatic DocFX target change. Default
solution builds exclude 1.7; select its project or the command above explicitly.

The [migration plan](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/rimworld-1.7-migration-plan.md)
owns the baseline evidence, runtime gates and 1.6 freeze milestone. Freeze ongoing
1.6 development once the agreed 1.7 baseline is accepted, retaining explicitly
scoped 1.6 maintenance when needed.

At acceptance, retain the final 1.6 source/content in Git and move active builds,
dependencies, RimBridge validation, CI and documentation to 1.7. There is no
ongoing requirement to maintain a parallel 1.6 runtime or update its dependencies.
The existing local snapshot is temporary migration insurance, kept untouched
outside Git; no maintained capture/restore workflow is required. Normal player
config backups and post-run checks still apply to the active development target.

Bundle outputs are version-owned under `Mod/<version>/AssetBundles/`. Raw assets
and builder inputs still have a shared 1.6 owner in `AssetsRaw/` and
`UnityAssetBuilder/`. Before diverging assets for a new version, explicitly
separate raw sources and builder/toolchain requirements while preserving 1.6.
The output path alone does not provide another engine's asset pipeline. Keep
shared root loading free of version-specific bundles. Do not add 1.7 folders,
APIs or support declarations as part of routine 1.6 maintenance.

## Asset workflow

Edit raw art/audio only in `AssetsRaw/`, then run the selected asset build. It
stages copies under `UnityAssetBuilder/Assets/Data/Mrdav30.Rimgate/` and generates:

```text
Mod/1.6/AssetBundles/rimgate_core
Mod/1.6/AssetBundles/rimgate_core.manifest
Mod/1.6/AssetBundles/AssetBundles
Mod/1.6/AssetBundles/AssetBundles.manifest
```

Do not manually copy raw textures/audio into `Mod/` or manually edit bundle
contents. The four existing baseline files were moved byte-for-byte from the
shared root into the 1.6 folder. `.gitignore` keeps these four paths visible for
version control while ignoring other generated bundle outputs. Do not regenerate
baseline files for a path-only move or add/update generated artifacts in commits
without explicit instruction. Check the diff after a build and keep source
changes separate from generated output changes.

## Implement and verify the affected behavior

Preserve existing XML category structure and `Rimgate_*` naming. Reuse existing
defs and extension points, centralize XML-linked C# definitions in `Rimgate_DefOf`,
and keep Harmony patches narrow. Conditional XML patches belong under
`Mod/1.6/Patches/`; check activation with the external mod absent and present.
Update the compatibility matrix when an integration changes, distinguishing
implemented support from a tested dependency combination.

Start with a minimal mod list containing the required dependencies. Record the
game build, relevant dependency versions, load order, reproduction and expected
result. Parse edited XML, compile affected C#, then inspect startup logs for def
resolution, patch and assembly errors. Exercise the complete affected gameplay
path. A debugger is useful when behavior diverges; attaching it is not a test.

Choose checks relevant to the change:

| System | Representative acceptance checks |
| --- | --- |
| Gates and addresses | Dial, transfer a pawn/item, check arrival/cleanup and cross-map references; save/reload and continue the affected state |
| Sarcophagus | Enter/treat/eject a patient, check access/prerequisites, interruption and saved patient state |
| Cloning | Stage the host, run the affected cycle, check resource use and pending clone/container ownership across reload |
| ZPM/housing | Check reserve/power behavior, facility linking and saved broadcast/energy state |
| Mobile containers | Load, move and unload; check interruption, item/pawn ownership and reload |
| Optional integration | Check its activation and affected operation with the dependency absent and present |
| UI/assets | Check English keyed strings, actual texture/sound loading and the relevant display/action |

Use a copy of an existing save for persistence checks. Preserve def names and
Scribe keys/defaults or deliberately migrate them. Newly created cases alone do
not prove old saves survive. Keep concise evidence in the owning tracker entry or
feature plan, including checks that could not be run. Guides retain the current
usage/compatibility contract. Documentation changes need no game run.

## Live validation with RimBridge

[RimBridgeServer](https://github.com/pardeike/RimBridgeServer) and
[GABS](https://github.com/pardeike/GABS) are development tools, separate from
Rimgate's player dependencies. The active migration profile is experimental
`rimgate17`. Its graphics-enabled hidden-window smoke verifies gate selection,
iris mutation/save/reload, 120 exact ticks and a rendered screenshot without
desktop focus. Full asset, transit and optional-integration acceptance remain
migration gates.
This does not declare Rimgate 1.7-compatible. Historical 1.6 smoke evidence is in
the completed hardening record; runtime checks must match the actual target.

### Prepare or refresh the isolated deployment

With all RimWorld instances stopped, install the matching 1.7 game, then run:

```powershell
.\.assets\scripts\setup-rimbridge-dev.ps1 -RimWorldVersion 1.7
```

Ludeon provides 1.7 on Steam's `unstable` branch. Changing the branch is a user
choice; the setup script does not change it. On November 24 the owner authorized
that switch, yielding **1.7.5014 rev1195**. Keep Steam running during validation.
The experimental RimRef pin must match the installed game build before staging.

Overrides are `-GamePath`, `-SteamModPath` (Workshop `content`, above `294100`)
and `-ToolsPath`. Defaults use the D: Steam library and
`%LOCALAPPDATA%/RimgateDev`. The script:

1. Checks the selected version, executable and 1.7 .NET 10 runtime layout; rejects
   setup while either game executable is running. Backs up normal-player
   `ModsConfig.xml` and `Prefs.xml` bytes/hashes before preparing the deployment.
2. Downloads and verifies pinned GABS/RimBridge archives, then builds the selected
   project against the installed dependency set. Use public reference restoration
   for portable compile checks; it is not a runtime mod installer.
3. Copies the 1.7 runtime's root files into `Game-1.7`, junctions `Data`, `Rendering`
   and `vulkan` to the installed game, and links required Workshop dependencies.
   These are mutable development views, not preserved installations.
4. Creates an ignored `.assets/experimental/1.7/Mod` overlay using the retained
   1.6 XML/bundle inputs, shared About/languages and new assembly. Only that test
   package declares 1.7, with a 1.7-only LoadFolders entry; tracked player metadata
   and 1.6 assets remain unchanged. The private copy is deployed under
   `Game-1.7/Mods/Rimgate` alongside the bridge.
5. Creates `Profile-1.7` and configures/registers GABS as `rimgate17`. Registration
   selects this active profile. Waylith uses `-NoWindow` and
   `-WindowSize=1280,720`, keeping rendering and synthetic input available without
   showing a native window. Graphics-free `-Headless` is unsuitable for this
   visual case. Old 1.6 view/profile files are not removed. Do not
   launch the old Unity view after its Steam-linked game data has moved to Waylith.

The helper still accepts `-RimWorldVersion 1.6` for an actual 1.6 installation;
its default stays 1.6 until public 1.7 acceptance. Use the explicit 1.7 argument
for current migration work. Setup never writes the normal Steam `Mods/Rimgate`
copy or player config/save directory. Its generated prefs use the exact lowercase
`fullscreen` field required by the 1.7 XML loader.

The minimal active list is Harmony, Core, five DLC, Vanilla Expanded Framework,
Rimgate and RimBridgeServer. DBH remains absent for the initial runtime case;
its assembly is a compile input for the optional integration.

Pins verified on November 24, 2026:

| Component | Version | Release archive SHA-256 |
| --- | --- | --- |
| GABS Windows x64 | `1.1.2` | `c0337277789162657bcc577ed6a559136f3c1b0c38e074ae7cf5cb1223b3a3d8` |
| RimBridgeServer package | `2.5.0` | `4b31843ff94e74a99d740916cabae82a3c17a96b6b31b668ab348cd53976859c` |

The observed 1.7 bridge is **2.5.0.0**, SDK **2.0.0.0**, exposing **130 tools**.
Discover schemas from the live runtime. The package's historical 1.6 payload
reported different versions; package version alone does not identify the loaded SDK.

### Run the acceptance case through MCP

1. Use `games_list`, then `games_start` with `gameId: rimgate17`, `profile: dev`,
   `timeout: 240`. A `started_bridge_pending` result means startup continues;
   inspect status/connect instead of starting another process.
2. Discover names with `games_tool_names` and inspect schemas with
   `games_tool_detail`. If a hot configuration change makes detail lookup report
   an unknown game despite a live connection, `games_tools` with a narrow `query`
   exposes the live schemas. Fully qualified names without a redundant `gameId`
   worked with `games_call_tool` in this session.
3. Confirm `rimworld/get_mod_configuration_status`: private `configPath` and
   Rimgate `rootDir`, matching loaded session, no version/dependency/configuration
   issues. Confirm bridge/SDK state. GABS context reporting was `unknown` even on
   1.7; verify OS process arguments and live profile paths independently.
4. From the main menu, compile `.assets/scripts/rimbridge-smoke.lua` with
   `rimbridge/compile_lua_file`, then run it with `rimbridge/run_lua_file` and a
   240-second outer timeout. Supply the actual profile/deployment/evidence paths:

   ```json
   {
     "scriptPath": "F:/gamedevrepos/rimworld mods/Rimgate/.assets/scripts/rimbridge-smoke.lua",
     "parameters": {
       "configPath": "C:\\Users\\david\\AppData\\Local\\RimgateDev\\Profile-1.7\\Config\\ModsConfig.xml",
       "modRoot": "C:\\Users\\david\\AppData\\Local\\RimgateDev\\Game-1.7\\Mods\\Rimgate",
       "evidenceDirectory": "C:\\Users\\david\\AppData\\Local\\RimgateDev\\evidence\\1.7"
     }
   }
   ```

5. Check operation status **and** nested gameplay/script success. A completed
   handler may report `success: false`. Require the complete smoke assertions,
   inspect the actual screenshot for rendered content and affected textures,
   and read the native log as well as the bridge journal. A successful capture
   does not establish that every asset loaded correctly. Longer gate-transit,
   optional-integration and old-save checks remain separate migration gates.
6. Stop the private game with `games_stop`, then run:

   ```powershell
   .\.assets\scripts\setup-rimbridge-dev.ps1 -VerifyPlayerConfig
   ```

   Use the same ToolsPath override as setup. Review any mismatch against the
   recoverable backup before restoring; do not overwrite concurrent player edits.
   Run this check before another setup refresh replaces the recorded baseline.

Waylith's log is observed at
`%USERPROFILE%/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Logs/Player.log`.
It is shared application output even with an isolated save-data argument. Copy it
into the external evidence directory after shutdown, and inspect startup errors
as well as the bridge journal: errors before bridge initialization can be absent
from that journal. The 1.7 launch passes `-savedatafolder=<profile>`, `-NoWindow`
and `-WindowSize=1280,720`; it does not reuse Unity's screen/log flags. Keep
background checks on this hidden graphics profile and coordinate any foreground
validation with the user. Normal player-config backup checks still apply.

## Local packaging and review

For a local playable copy, build and use the `Mod/` directory with its existing
layout. Do not redistribute game/mod dependency DLLs, source tooling or raw assets
as runtime dependencies. If Workshop publishing is requested later, package only
`Mod/` after explicit compatibility validation and publication authorization.

Inspect `git status --short`. Guides, API configuration and code now belong to
the same repository and commit. Leave changes unstaged/uncommitted for review
unless instructed otherwise. Do not commit, push or publish as an implied step
of editing these docs.

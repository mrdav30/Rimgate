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
   These are mutable development views, not preserved installations. For 1.7,
   also builds the SDK 2.0.0 development asset probe into
   `Game-1.7/BridgeTools/Rimgate/` and generates `asset-manifest.json` from
   `AssetsRaw`. Its source project is under `.assets/tools/RimgateAssetProbe/`;
   it references the actual installed game. Only its own DLL/deps file and
   manifest are deployed there, outside the Rimgate player package.
4. Creates an ignored `.assets/experimental/1.7/Mod` overlay using the retained
   1.6 XML inputs, selected asset format, shared About/languages and new assembly. Only that test
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

The observed 1.7 bridge is **2.5.0.0**, SDK **2.0.0.0**, with 130 core tools
plus the deployed development companions.
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

### Verify the experimental 1.7 assets

After the smoke case creates `Rimgate-bridge-smoke.rws`, compile and run
[`.assets/scripts/rimbridge-assets-1.7.lua`](https://github.com/mrdav30/Rimgate/blob/main/.assets/scripts/rimbridge-assets-1.7.lua)
through the same Lua tools and with the same three path parameters. Use an outer
timeout of 180 seconds and inspect both script and nested gameplay results.
The case reloads that owned save, reveals its map, spawns/claims a Vanometric
power cell at `(105,130)` outside the gate footprint, waits for power, and orders
a real colonist to close and reopen the iris. It captures both states and
requires a clean bridge error journal. It does not overwrite the smoke save;
reruns reload it to reset the fixture. An obstructed map or failed pawn job must
be investigated as a fixture/gameplay failure, not bypassed with a state mutation.
Keep this case on the isolated English profile, whose labels it asserts.

Setup deploys the read-only `rimgate/asset_report` companion tool for this case.
It reads the generated manifest, resolves each texture/audio object from Rimgate's
selected owned format (Unity bundles or the mounted native pack), checks texture
dimensions and clip metadata, and checks
that normal `ContentFinder` lookup returns the same objects. It also inspects
`RimgateTex` materials/shaders and selected gate command icons. An unselected
menu report can check the inventory, but does not validate gate commands; the Lua
case explicitly requires an equipped selected gate and its real iris control.
Discover the installed schema before calling the tool directly.

The red X glyphs on the developer-only **Add/remove iris** and **Force close**
commands are inherited null-icon defaults in both 1.6 and 1.7. They are separate
from the player's **Close iris/Open iris** command, which must resolve its real
`RGIris_Close`/`RGIris_Open` textures. A nonnull bad texture is still a failure.
Do not infer a bundle-loading defect from an intentionally unassigned dev icon.

This check establishes asset names, ownership, loading and the rendered states
exercised by the scenario. It does not compare raw files with decoded bundle
bytes or establish visual fidelity for every asset. The profile is muted; valid
clip metadata does not prove audible playback. Active gate/DHD effects, other
rotations and audio playback belong to the remaining migration checks. The
separate transit case below checks a known-address gate operation.
The retained 1.6 bundles and Unity builder remain unchanged. The native-pack
comparison below is experimental and does not declare public 1.7 support.

### Verify the experimental 1.7 gate operation

After the smoke case creates its owned save, compile and run
[`.assets/scripts/rimbridge-transit-1.7.lua`](https://github.com/mrdav30/Rimgate/blob/main/.assets/scripts/rimbridge-transit-1.7.lua)
with the same `configPath`, `modRoot` and `evidenceDirectory` parameters. Use
`phase: prepare` first, then pass its returned `fixture` object unchanged to
`dial`, `connect`, `send`, `receive` and `close`, in that order. Each invocation
is bounded separately; use an outer 180-second timeout and inspect gameplay
payloads as well as operation status. The isolated English profile and native
Waylith package are required. These phases overwrite only their owned
`Rimgate-transit-*` saves. Preserve copies before investigating a failed run.

The development companion prepares a lazy transit site through normal address
registration, a powered source gate and staged colonists with a unique inventory
item. Dialing must generate the destination map through the real DHD job. The
case reloads queued and active links, then uses real entry jobs and saves at
both send and receive boundaries. It checks object identity, one actual owner,
inventory, redraft intent, resumed arrival, empty buffers and normal DHD closure.
`send` and `receive` each reload the owned active checkpoint. An additional
`send` invocation with `cancelPending: true` checks pending-matter cleanup through
the existing developer **Force close** command while paused; run it before the
normal send/receive/close sequence. This cancellation check is separate from the
normal pawn-driven close job.

Discover `rimgate/prepare_transit_fixture`, `transit_status`,
`stage_transit_approach` and `view_transit_map` from the installed companion.
Fixture tools guard the private profile/deployment and only prepare same-map
actors or change the viewed map; they do not activate links or transfer travelers.
Status resolves IDs anew after every reload and distinguishes queue references
from the deep owner. Bridge frame stepping can stop when lazy map generation
starts a long event; the connect phase accepts only that specific busy result,
waits for readiness and resumes checks. Do not treat arbitrary stepping failures
as success.

This is known-address registration, not glyph/quest discovery. Inventory cargo
does not cover loose-item hauling, transporter loading, containers or every
interruption/blocked-placement path. Use the owning migration plan for dated
evidence, remaining acceptance and saved-state defects. The companion and its
Rimgate reference stay outside the player mod.

### Verify planetary discovery and quest persistence

Compile and run
[`.assets/scripts/rimbridge-discovery-1.7.lua`](https://github.com/mrdav30/Rimgate/blob/main/.assets/scripts/rimbridge-discovery-1.7.lua)
with the same three private paths, a 180-second outer timeout and separate
`prepare`, `decode`, `connect`, `visit` invocations. Pass prepare's returned
`fixture` into later phases and decode's returned `site` into connect/visit.
The native package and owned smoke save are required. The case writes only
`Rimgate-discovery-*` checkpoints; it does not overwrite the smoke/transit saves.

The guarded companion provides two planetary parchments, prerequisite research
and a healthy Intellectual-20 colonist. The real **Decode symbols** menu job
must consume one parchment and generate the quest/site/address. Preparation
does not insert any discovery outcome. The scenario saves/reloads an unfinished
decode job, verifies the completed quest and lazy site's persistent binding,
dials the discovered address, and checks generated receiving gates and map count.
The visit phase orders real entry and saves/reloads the visited site. Hostile
quest contents still apply; the fixture does not suppress combat or force survival.
Orbital discovery and quest completion are separate acceptance work.

The connect phase handles a bounded number of deferred long events, accepting
only the bridge's specific busy response and resuming the remaining ticks. Other
failures remain failures. Its screenshot and visit's real context-menu input
require working hidden rendering/input. Current capture/input limitations and
the distinction between verified manual steps and whole-script results are
recorded in the [migration plan](https://github.com/mrdav30/Rimgate/blob/main/docs/feature-work/rimworld-1.7-migration-plan.md).

### Observe production audio without speaker output

The development companion exposes `rimgate/begin_audio_probe`,
`audio_probe_status` and `end_audio_probe`. Discover their installed schemas.
Begin requires the exact private config/mod paths, a comma-separated list of
Rimgate-owned SoundDef names and a duration of 1000..60000 ms. It mutes master
output before enabling effect gain, observes only the requested production
voices and restores both gains on end, timeout or a handled probe failure.
It does not play sounds or capture system/microphone audio. Check
`preferencesRestored` even when the requested sounds were not observed.

Use separate bridge invocations and normal unpaused engine frames for this case:

1. Load `Rimgate-discovery-active` after the discovery connect phase. View the
   source map with its camera near the gate. Begin a 60-second probe for
   `Rimgate_GateIdle,Rimgate_IrisClose,Rimgate_IrisOpen,Rimgate_GateClose`.
2. Request **Close iris**, order the real **Prioritize toggling switch** job,
   run at Normal speed for roughly three seconds and pause. Assert the iris
   closed. Repeat **Open iris** with its normal job and about 1.5 seconds.
3. Request **Close Dwarfgate** on the DHD, order the normal closing job, run
   at Normal for roughly three seconds and pause. Assert the connection closed.
   End the probe and require no missing definitions, finite/nonzero source
   windows and restored gains. Allow more time within the bound if pathing
   requires it; validate state instead of assuming a delay completed the job.
4. For opening, begin a separate 20-second probe for
   `Rimgate_GateOpen,Rimgate_GateIdle`, order real DHD dialing to the already
   loaded discovered site, run Normal for roughly 10.5 seconds, pause, then end.
   Map generation should be finished before this timed audio case.

Do not put the whole audio observation interval inside one Lua run or a long
tick-stepping call: these can starve background sampling. Leave real time between
separate calls and inspect `readCalls`/`maximumReadGapMs`. Idle deliberately mutes
while paused, and a voice on the other map is normally silent. A probe with no
requested event must report missing definitions; timeout cleanup is not positive
audio acceptance. Volume setters invoke the game's broader preference application,
so keep probes on the guarded private profile and verify configuration afterward.

These are overlapping per-voice engine-output windows, not a continuous recording,
final device mix, speaker check or human listening assessment. Keep those limits
when interpreting the returned peak/RMS statistics.

### Inspect additional native presentation states

[`.assets/scripts/rimbridge-presentation-1.7.lua`](https://github.com/mrdav30/Rimgate/blob/main/.assets/scripts/rimbridge-presentation-1.7.lua)
provides `prepare`, `open` and optional `iris` phases. Supply the private paths,
`rotation: 0..3`, `controller: Ancient|Tauri` and a unique `capturePrefix`; pass
prepare's `fixture` to later phases. Preparation respawns only the inactive
empty source gate/controller; normal jobs must dial and operate the iris.
The Tau'ri fixture adds enough power for its 800 W controller plus the 500 W iris.
`open` polls the real approach in 20-tick steps, bounded at 400 ticks, then drafts
the dialer at the controller through the lethal opening effect. It verifies the
pawn survives without moving; `iris` undrafts before requesting the real toggle
job. This isolates presentation from ordinary post-job wandering; it does not
test automatic evacuation or make the vortex harmless.

Captures use the normal minimum camera root size, 11. Review inactive, queued,
opening, active and closed-iris pixels for all four ancient-controller rotations
and North with Tau'ri. Compare the same paused state at root size 24 when checking
zoom-dependent defects. Material success is separate from rendered pixels; the
read-only status includes camera depth and base/overlay pitch. Preserve the
incoming draw altitude once and match overlay mesh pitch to the base. Check
chevrons remain above the closed iris. Opening stills sample the repeated vortex
effect; conditional iris glow and continuous animation require separate cases.

After an input/capture timeout, use the guarded read-only
`rimgate/render_capture_status` to record native capture support, queued files,
capture flags and render/framebuffer dimensions. Do not clear the queue or
infer successful rendering from frame counters or a readiness flag. Preserve
the failed payload and native log. Follow the migration plan's bounded
investigation; coordinate any foreground/manual checks with the user.

### Keep hidden input and capture progressing

Experimental setup deploys a 1.7 companion that registers a public
`EngineApplication.PrePollEvents` callback during SDK provider discovery. When
the private `Profile-1.7` developer window is hidden, graphics-enabled and has a
nonzero surface, the callback requests redraws if normal presentation is
suppressed. It neither shows nor focuses the window, changes OS session/display
state, patches gameplay nor clears screenshot queues. Non-occluded operation
continues through the normal engine path. `-Headless` remains unsuitable for
rendered checks, and a missing/lost graphics surface needs its own diagnosis.

RimBridge 2.5.0 constructs tool owners with instance methods during discovery;
all-static metadata loading does not guarantee startup initialization. Keep the
existing render-status owner constructor as the idempotent registration point.
The native log records **Hidden background redraw hook initialized** before
companion tool calls; `render_capture_status` reports installation, requests
and the actual render predicate. The additional `Waylith.Windowing` reference
is non-copying and belongs only to the development companion.

Run the regression through
[`.assets/scripts/rimbridge-background-1.7.lua`](https://github.com/mrdav30/Rimgate/blob/main/.assets/scripts/rimbridge-background-1.7.lua)
using the same private paths and a unique `capturePrefix`:

1. Run `phase: prepare`; retain its returned `fixture`. It loads the owned smoke
   save and stages a real DHD menu target. It does not overwrite that save.
2. Call `rimgate/start_render_occlusion_fixture` with the exact config/mod paths,
   `suppressRedraw: true`, `durationMs: 25000`. Run `phase: negative` with the
   returned preparation fixture. Success here means the expected capture/input
   failures were reproduced while updates continued, not that those actions passed.
3. In caller `finally` cleanup, call `rimgate/end_render_occlusion_fixture` and
   require `restored: true`. Use a separate bridge invocation for cleanup.
4. Start another fixture with `suppressRedraw: false`, `durationMs: 60000`, then
   run `phase: positive`. It checks five real UI/capture cycles, iris mutation,
   save/reload and 120 exact ticks while still occluded, hidden and unfocused.
   End/verify cleanup in the same way, including when a phase fails.

The fixture reports occlusion only inside the engine and restores actual OS
conditions automatically on timeout (maximum 60 seconds). It does not minimize
or cloak a real window, lock the computer or change display power. A timed-out
negative capture can finish after cleanup restores rendering; retain it as a
late artifact, not a passing negative operation. Use unique capture names across
runs to keep that distinction clear. The script writes only its owned
`Rimgate-background-check` save. The migration plan owns dated evidence and the
limits of this controlled test.

### Compare native Waylith packs with Unity bundles

Setup defaults to native Waylith packs when `-RimWorldVersion 1.7` is selected;
retained 1.6 continues to default to Unity bundles. With all game instances
stopped, prepare native 1.7 or explicitly select the legacy comparison:

```powershell
.\.assets\scripts\setup-rimbridge-dev.ps1 -RimWorldVersion 1.7
.\.assets\scripts\setup-rimbridge-dev.ps1 -RimWorldVersion 1.7 -AssetFormat UnityBundles
```

Run and stop one deployment before preparing the other. Both use the same private
game/profile and owned smoke save. Keep their evidence directories separate and
pass `assetFormat` (`UnityBundles` or `WaylithPack`) with the three path parameters
to `rimbridge-assets-1.7.lua`; its optional format assertion prevents a requested
comparison from silently using the other format. Direct `rimgate/asset_report`
returns complete texture/audio metadata for comparisons; large script-step
payloads may be omitted, so preserve the direct report as well as script status.

Native setup stages raw PNGs/OGGs only inside the ignored experimental overlay,
then calls `.assets/scripts/build-waylith-assets.ps1`. That helper launches the
matching game's bounded hidden `-BakeModAssetPacks=<staged-mod-root>` process
with a separate `Profile-1.7-Bake`. It writes `build-waylith-assets.log` and
`build-waylith-assets-stderr.log` at the repository root. After successful exit
and nonempty output checks it removes staged loose `Textures`, `Sounds` and
`AssetBundles`; the private candidate contains `1.7/Assets.zip` with `.stamps`,
`.roles` and `.fingerprint` sidecars. The native probe requires the exact mounted
Rimgate pack and rejects loose files, legacy bundles, missing entries and shadowed
objects. Do not create this pack with a generic ZIP tool or copy source files
into tracked `Mod/` manually.

The helper can also rebake an already prepared overlay directly:

```powershell
.\.assets\scripts\build-waylith-assets.ps1 -RimWorldVersion 1.7
```

It accepts `-GamePath` and `-ToolsPath` for alternate installed/private roots.
A standalone rebake updates the ignored overlay; rerun setup with the chosen
format to redeploy it. The helper deletes only its four known prior outputs
before baking, so stale files cannot establish a successful new build. Setup
rejects native format for 1.6. Normal release/docs/Unity bundle builds retain
their existing supported-target behavior.

Native baking consumes raw inputs rather than Unity importer metadata. The
current comparison preserved all texture dimensions, but native results use
mip chains, trilinear/repeat sampling and BC7/BC4, while the retained bundle uses
one mip, bilinear/clamp sampling and DXT/RGBA formats. The gate looks softer at
the tested zoom. Audio metadata can also differ from the old imported clips.
The owner selected native as the 1.7 development default to remove the Unity
build dependency. Preserve the known softer sampling observation and verify
texture appearance in subsequent scenarios; passing asset checks does not
establish identical presentation. The migration plan owns the dated comparison,
default decision and remaining visual/effect/audio gates. Setup leaves the
selected format deployed until the next setup. The retained 1.6 Unity pipeline
and tracked player package are unchanged.

## Local packaging and review

For a local playable copy, build and use the `Mod/` directory with its existing
layout. Do not redistribute game/mod dependency DLLs, source tooling or raw assets
as runtime dependencies. If Workshop publishing is requested later, package only
`Mod/` after explicit compatibility validation and publication authorization.

Inspect `git status --short`. Guides, API configuration and code now belong to
the same repository and commit. Leave changes unstaged/uncommitted for review
unless instructed otherwise. Do not commit, push or publish as an implied step
of editing these docs.

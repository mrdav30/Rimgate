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
| `Source/Rimgate/Rimgate.csproj` | Current 1.6 C# source project, targeting .NET Framework 4.8 |
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

From the repository root:

```powershell
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.6
.\.assets\scripts\build-assetbundle.ps1 -RimWorldVersion 1.6
.\.assets\scripts\build-release.ps1 -RimWorldVersion 1.6
```

All default to `1.6`. `-Configuration Debug` is available for assembly/full builds.
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
when the scripts are invoked from another working directory. The isolated game's
`Player.log` stays under its existing external `ToolsPath/evidence` directory.

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
`.assets/references/` cache and are not published with the site or player mod.

For an assembly-only build using the same portable references:

```powershell
.\.assets\scripts\restore-build-references.ps1
.\.assets\scripts\build-assembly.ps1 -UseReferenceAssemblies -SteamModPath .assets/references
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

## Version ownership and a future 1.6 freeze

Only **1.6 is configured now**. `.assets/scripts/build-common.ps1` checks `About.xml`,
`LoadFolders.xml`, the selected `Mod/<version>` folder and its source project
before running build tools. Passing `-RimWorldVersion 1.7` fails; it does not
create content, change support metadata or make the current code compatible.
The current project also rejects a direct MSBuild target other than 1.6.

Keep `Source/Rimgate/` as the current 1.6 owner rather than moving every file
before there is another implementation. The resolver also recognizes
`Source/<version>/Rimgate/Rimgate.csproj` for an explicitly configured target.
This provides a place for version-specific source when it is needed, without
compiling all version folders together or scattering version conditionals.

When a migration is requested, establish the last accepted 1.6 baseline and its
validation evidence first. If 1.6 is then frozen, preserve its source, defs,
patches, dependency references and assets. Add a separately owned project for
the new version; do not point a new output folder at 1.6 code and call it a port.
Moving the existing 1.6 source into the versioned layout can happen then if useful.

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

## Live 1.6 validation with RimBridge

[RimBridgeServer](https://github.com/pardeike/RimBridgeServer) and
[GABS](https://github.com/pardeike/GABS) are verified development tools for live
Rimgate checks. They remain separate from the player mod's dependencies and
ordinary release builds. DecompilerServer is an optional upstream code-inspection
tool; it is not needed or installed by this workflow.

### Prepare or refresh the isolated deployment

Stop any running RimWorld instance, keep the selected Steam installation on
RimWorld 1.6, then run from the repository:

```powershell
.\.assets\scripts\setup-rimbridge-dev.ps1
```

Overrides are `-GamePath`, `-SteamModPath` (Workshop `content`, above `294100`)
and `-ToolsPath`. Defaults match the local D: Steam library and
`%LOCALAPPDATA%/RimgateDev`. Start Steam before launching the game and keep it
running during validation. Avoid a simultaneous normal-game session.
The setup script:

1. Rejects a game installation outside 1.6 or any running RimWorld instance,
   and refuses linked content inside a mod deployment before replacing it.
   Backs up the actual normal-player `ModsConfig.xml` and `Prefs.xml` bytes in a
   timestamped evidence directory and records their hashes/existence in
   `player-config-baseline.json` before preparing the tools.
2. Downloads the pinned release archives if missing and checks their SHA-256.
   Extracted version-specific package directories are reusable installation caches.
3. Builds the 1.6 Rimgate assembly against the selected game's references.
4. Creates a private game view with copied player binaries, read-only-use
   junctions to installed game data/runtime and required Workshop dependencies,
   and independent copies of Rimgate and RimBridgeServer in its `Mods` folder.
5. Writes the dedicated minimal mod list and GABS configuration, validates it,
   and registers the user-level Codex MCP server `rimgate-dev` using GABS stdio.

The game view is `Game-1.6`, save/config data is `Profile-1.6`, and captures/logs
are under `evidence`, all beneath `ToolsPath`. The ten active entries are Harmony,
Core, the five DLC, Vanilla Expanded Framework, Rimgate and RimBridgeServer.
Setup does not write the normal Steam `Mods/Rimgate` copy or player save/config
directory. Verify configuration preservation after each live run as described
below; the first adoption run detected a normal `Prefs.xml` change. Game data
and Workshop dependencies are linked, not frozen copies;
they must remain compatible with 1.6. Run setup before a new validation cycle,
especially after upstream updates. A future 1.7 setup is separate work.

Pins verified on November 23, 2026:

| Component | Version | Release archive SHA-256 |
| --- | --- | --- |
| GABS Windows x64 | `1.1.2` | `c0337277789162657bcc577ed6a559136f3c1b0c38e074ae7cf5cb1223b3a3d8` |
| RimBridgeServer package | `2.5.0` | `4b31843ff94e74a99d740916cabae82a3c17a96b6b31b668ab348cd53976859c` |

The package's **loaded 1.6 bridge and SDK are `2.3.0.0`**, not the active 1.7
source/SDK described by upstream main. The live 1.6 connection exposed 128 tools.
Discover schemas from the connected runtime before calling them or writing a
companion. No Rimgate-specific companion or new runtime reference is needed for
the smoke case below.

### Run the acceptance case through MCP

Use the registered server's GABS tools:

1. `games_start` with `gameId: rimgate16`, `profile: dev`, `timeout: 240`.
   `started_bridge_pending` means startup continues; use `games_status` and
   `games_connect` instead of starting a second process.
2. `games_tool_names` for `rimgate16`, then `games_tool_detail` for the needed
   tools. Pass returned names unchanged to `games_call_tool`.
3. Confirm `rimworld/get_mod_configuration_status`: correct isolated `configPath`
   and Rimgate `rootDir`, matching loaded session, zero configuration/dependency
   issues. Confirm the actual bridge/SDK with `rimbridge/get_bridge_status`.
4. Start from the main menu; `rimworld/go_to_main_menu` returns there if needed.
   Compile `.assets/scripts/rimbridge-smoke.lua` using `rimbridge/compile_lua_file`, then run it
   with `rimbridge/run_lua_file`, allowing a 240-second outer call timeout.
5. Check **both** the operation status and gameplay/script `success` payloads.
   A completed handler can return `success: false`; a successful transport call
   alone does not prove the action worked. Inspect the saved screenshot and
   startup `Player.log` in addition to the bridge error journal.
6. Stop the isolated game with `games_stop` after capturing results, then run:

   ```powershell
   .\.assets\scripts\setup-rimbridge-dev.ps1 -VerifyPlayerConfig
   ```

   Use the same `-ToolsPath` override as setup, if any. A mismatch fails with the
   original backup location. Review differences before restoring the file; do
   not overwrite player edits made after the snapshot. Run this check before
   another setup refresh records a new baseline.

The older 1.6 payload does not implement GABS's newer launch-context reporting;
on Windows, `contextDelivery` can therefore be `unknown`. Verify the process
arguments and the live configuration paths above as independent delivery evidence.

For default paths, the file-tool arguments are:

```json
{
  "scriptPath": "F:/gamedevrepos/rimworld mods/Rimgate/.assets/scripts/rimbridge-smoke.lua",
  "parameters": {
    "configPath": "C:\\Users\\david\\AppData\\Local\\RimgateDev\\Profile-1.6\\Config\\ModsConfig.xml",
    "modRoot": "C:\\Users\\david\\AppData\\Local\\RimgateDev\\Game-1.6\\Mods\\Rimgate",
    "evidenceDirectory": "C:\\Users\\david\\AppData\\Local\\RimgateDev\\evidence"
  }
}
```

Replace paths for another checkout or `ToolsPath`. The Lua case rejects a
different profile/deployment and an already-loaded game before creating its fresh
test colony. It spawns a gate and DHD, claims the gate, adds an iris through its
live gizmo, saves, removes the iris, and reloads. It asserts the same thing ID and
inspect/address state were restored, advances exactly 120 ticks across 120
frames, captures a screenshot and rejects errors in the bridge journal. It
creates/replaces only the owned `Rimgate-bridge-smoke` save in the isolated profile.
The profile uses English UI labels and dev mode for the iris gizmo.

Selection-scoped gizmo IDs must be rediscovered after selection/state changes.
Use `click_cell` for plain map selection; `open_context_menu` expects a selected
pawn when issuing orders. Keep longer feature-specific cases separate: this
smoke verifies bridge integration, asset rendering, UI mutation, tick control and
save/load restoration. It does **not** validate gate transit, combat balance,
every optional integration or multiplayer.

## Local packaging and review

For a local playable copy, build and use the `Mod/` directory with its existing
layout. Do not redistribute game/mod dependency DLLs, source tooling or raw assets
as runtime dependencies. If Workshop publishing is requested later, package only
`Mod/` after explicit compatibility validation and publication authorization.

Inspect `git status --short`. Guides, API configuration and code now belong to
the same repository and commit. Leave changes unstaged/uncommitted for review
unless instructed otherwise. Do not commit, push or publish as an implied step
of editing these docs.

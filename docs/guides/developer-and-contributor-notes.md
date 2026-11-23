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
| `docs/docfx.json`, `docs/toc.yml`, `docs/templates/` | Authored DocFX site configuration, navigation and theme |
| `docs/obj/` | Ignored API metadata, documentation inputs and rendered site |
| `.assets/build-references.json` | Commit/hash pins for public mod build references |
| `.assets/scripts/` | PowerShell build/development tools and Lua runtime smoke cases |

Bundles go to **`Mod/<version>/AssetBundles/`** (`Mod/1.6/AssetBundles/` today).
The shared root is loaded alongside `1.6` by `LoadFolders.xml`, so keep the old
`Mod/AssetBundles/` location empty. Do not put a second copy of the same assembly,
defs, patches or bundle in both loaded locations. The existing load configuration
already selects the version-owned bundle folder.

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
not prove old saves survive. Keep concise evidence in the owning issue/plan/page,
including checks that could not be run. Documentation changes need no game run.

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

## Build tooling verification: November 23, 2026

This is a tooling snapshot, not new gameplay or version-compatibility evidence.
Environment: Windows, .NET SDK `10.0.302`, Windows PowerShell `5.1`, Unity
`6000.3.9f1`. The sibling 1.6 `Assembly-CSharp.dll` used for the Release check
reported file version `1.6.9328.31620`.

- `.assets/scripts/build-assembly.ps1 -RimWorldVersion 1.6` passed in Release with zero warnings
  and errors. Debug also passed under Windows PowerShell with explicit
  `-RimWorldManagedPath` and `-SteamModPath` overrides.
- `.assets/scripts/build-release.ps1 -RimWorldVersion 1.6` passed under Windows PowerShell from
  an unrelated working directory, using a short isolated root containing spaces.
  The fixture copied the four build scripts and metadata, created `Mod/1.6`,
  and linked the real `Source/Rimgate`, `AssetsRaw` and `UnityAssetBuilder` inputs.
  It supplied the real reference paths explicitly. The assembly and all four
  bundle outputs were produced only inside the fixture; checked-in bundles were
  unchanged. Unity exited zero and reported bundle completion; the final log
  contained no exception or C# compiler error.
- All three entry points rejected unconfigured `1.7` before tool execution.
  The source project's `ValidateRimWorldVersion` target rejected `1.7` directly.
  A synthetic target fixture confirmed separate source/output selection,
  rejection of a missing source project, and rejection of a missing load entry.
- Child-process controls confirmed that a failed tool stops the asset build and
  a zero-exit tool producing no bundles is rejected. PowerShell parsing, skill
  frontmatter validation, local documentation links and both repository diff
  whitespace checks passed.

The first, longer temporary root logged a package-cache
`DirectoryNotFoundException` despite a successful build. Repeating from a short
root eliminated it. Use a short workspace when reproducing Unity package-cache
path issues and inspect the log as well as the exit code.

This initial build-only snapshot did not include in-game asset-loading or
save/load validation. The later live RimBridge adoption record below covers
those checks for its smoke scenario. RimWorld 1.7 migration remains unimplemented.

### Bundle relocation and repeated smoke checks

The follow-up relocation moved all four existing files from `Mod/AssetBundles/`
to `Mod/1.6/AssetBundles/` without regeneration. Each file retained its byte count,
SHA-256 and Git content identity. The old folder was removed; `LoadFolders.xml`
already selects `1.6`, so its content selection needed no change.

| File | Bytes | Preserved SHA-256 |
| --- | ---: | --- |
| `AssetBundles` | 1,575 | `d882253f6a663f9a3e173f76ecf886d7d601c5cc3949b3cb7d0770e9248250f5` |
| `AssetBundles.manifest` | 181 | `f2a487707dc9a78dc670159886237075cd1e47566548471ddc96008206ccd64a` |
| `rimgate_core` | 10,016,071 | `55cadf3d0d84bdf9c6030e65a2a5d2c0c70dad7d85d6ae50e369f7eb585f0ab4` |
| `rimgate_core.manifest` | 80,118 | `e6245f22b64031a1cabd185e66665cfe33049e0d4203169898d33664777c99e5` |

Repeated checks used the same toolchain and a short isolated fixture containing
spaces in its path, invoked from an unrelated working directory:

- `.assets/scripts/build-assembly.ps1 -RimWorldVersion 1.6 -Configuration Debug`, with the real
  reference-path overrides, passed with zero warnings and errors.
- `.assets/scripts/build-release.ps1 -RimWorldVersion 1.6` invoked the actual asset and assembly
  scripts. It produced all four files under the fixture's
  `Mod/1.6/AssetBundles/` and the Release DLL under `Mod/1.6/Assemblies/`, with
  no output in `Mod/AssetBundles/`. Unity reported bundle completion and exited
  zero with no exception or C# compiler error in the log. The assembly build
  passed with zero warnings and errors. Generated smoke outputs stayed in the
  fixture; they did not replace the relocated baseline.
- All three entry points still rejected unconfigured `1.7` before invoking
  tools. Script parsing, skill validation, documentation links and ignore-rule
  checks passed. The four relocated paths are visible for ordinary staging;
  other generated bundle paths remain ignored.

Staged-entry fingerprints in both the main and wiki repositories were preserved;
the relocation and follow-up edits were left unstaged/uncommitted. These smoke
checks verify tooling and output routing, not in-game asset loading or save/load.

### Explicit Unity version argument verification

The next follow-up made `BuildBundles(string rimWorldVersion)` the typed builder
API and added `BuildBundlesFromCommandLine` as Unity's parameterless adapter.
The wrapper supplies `-rimgateVersion`; the Unity method derives the output path
without a hard-coded version or output override.

The full release script passed under Windows PowerShell 5.1 in a fresh isolated
builder project copied from the real editor assets, Packages and ProjectSettings.
The real raw assets and runtime source were linked into that fixture. Unity
reported the selected `1.6` target and produced all four bundles in the fixture's
`Mod/1.6/AssetBundles/`; the Release assembly build passed with zero warnings and
errors. The positive Unity log contained no exception or C# compiler failure.

One-off controls inside the fixture's Unity editor verified that null, empty,
path-traversal and three-component versions are rejected, and a missing `9.9`
content folder creates no output. A separate real Unity invocation omitted
`-rimgateVersion`: it exited with code 1 and the intended missing-argument error,
leaving the existing fixture bundles unchanged. All three PowerShell entry points
also continued to reject unconfigured `1.7` before invoking tools.

The repository's baseline bundle hashes, staged entries and HEADs were preserved.
That version-argument follow-up remained unstaged/uncommitted. Its build checks
preceded the separate live RimBridge adoption below.

### Live RimBridge adoption verification

The pinned GABS and RimBridgeServer packages above were installed and exercised
against real RimWorld **1.6.4871 rev591** (the installation's `Version.txt` reports
rev590). Runtime introspection confirmed bridge/SDK **2.3.0.0**, 128 tools and the
ten-entry isolated mod list. It reported zero configuration, dependency, ordering
or session mismatch issues and the expected private Rimgate root/config paths.

The initial live check spawned a gate and DHD and rendered the gate textures from
the relocated baseline bundle. Its iris was added through the actual gizmo,
saved, removed, and restored by loading the save. The same thing ID and gate
address returned; real save XML independently recorded `HasIris=True`. Exact
stepping reported 120 ticks across 120 Unity frames, and the bridge error journal
was empty. The actual screenshot was inspected for the gate, DHD and iris UI.

The checked-in Lua case then compiled and passed three times: once through a temporary
local HTTP MCP validation endpoint, and twice after running the setup script
through the **exact stdio command registered as `rimgate-dev`**. All three runs checked
the post-save mutation and restored state, produced the screenshot/save, and
rejected error-journal entries. The stdio run returned gate
`Thing_Rimgate_Dwarfgate9903`, address `P5H-304`, and 120 completed ticks. GABS
reported the final game stop as `terminated` and removed its runtime claim.

Setup was exercised with Windows PowerShell 5.1, including cached repeat
deployment, refusal to refresh a running isolated game, and a nested-junction
control whose target file remained intact. Configuration is
written as UTF-8 without BOM for GABS. The source build completed with zero
warnings and errors. The normal player's `ModsConfig.xml`, both repositories'
staged entries/HEADs, and all four baseline bundle hashes were preserved.
The normal `Prefs.xml` hash changed at the first launch; it remained stable
through the later stdio smoke run. The initial snapshot retained its hash but
not its bytes, so the exact prior preferences could not be restored and the
cause was not established. The changed file was retained in local evidence;
the workflow now takes recoverable byte copies and verifies them after shutdown.
The final stdio run passed that verification for both player config files. A
deliberately mismatched fixture failed verification without modifying its file.
The final player log contained two Mono fallback-library diagnostics; no XML or
bundle-loading failures or managed exceptions were observed.
Runtime source and player dependency metadata were not changed.

Local run artifacts are retained under `%LOCALAPPDATA%/RimgateDev/evidence` and
the owned save under `Profile-1.6/Saves/Rimgate-bridge-smoke.rws`. The pinned setup,
Lua source and steps above reproduce the check without those captures. Keep
bridge endpoint tokens and raw private logs outside version control. Coverage is
the smoke scenario described above; full gate-transit and optional-mod matrices
remain feature-specific validation rather than claims of this adoption.

### Script layout verification

On November 23, 2026, the five PowerShell scripts and Lua smoke case were moved
to `.assets/scripts/`. Windows PowerShell 5.1 setup passed when invoked by its
full path from an unrelated working directory, including the real assembly
build and GABS configuration validation. The full release script then passed in
the fresh `C:/rimgate layout-15008425` fixture using copied source/assets/builder
inputs. Unity produced all four version-owned bundle files, and the assembly
build completed with zero warnings and errors. Both default build logs were at
the fixture root, with no logs under `.assets/scripts/`.

The relocated Lua case compiled and passed through the registered stdio MCP
server, including iris mutation/save/reload and 120 ticks. After shutdown, the
relocated setup verifier confirmed both normal-player config files matched
their backups. All three build entry points rejected unconfigured 1.7 before
running tools, and setup still rejected a `ToolsPath` anywhere inside the
repository. The Lua content was unchanged, documentation references were
updated, and both repositories' staged entries/HEADs and all four repository
baseline bundle hashes were preserved.

### DocFX migration verification

On November 23, 2026, all 23 wiki Markdown articles were migrated into the
repository's Guides section, with normalized filenames and explicit `.md` links.
Three stale Wraith biomass links were corrected and a source-backed settings
reference added for the previously missing target. The wiki worktree and
`.gitmodules` entry were removed after checking every original file against a
local backup; existing module history remains recoverable in Git's object store.

The documentation build compiled against public RimWorld 1.6.4871 references and
pinned mod binaries, then generated 332 public type pages across three namespaces
and 24 guides. The ordinary installed-game reference build also passed. Compiler
and DocFX checks completed with zero warnings and errors. The full script passed
under Windows PowerShell 5.1 when invoked from an unrelated working directory,
and in an independent checkout fixture with an empty NuGet package cache and no
game files or pre-restored mod references. Generated-site checks rejected both
a missing-link control and a wrong-case-link control; dependency binaries were
absent from the rendered site.

The local browser preview covered the home page, API navigation, the global
glyph-parchment type, topic search and the gate guide at a 390-pixel viewport
without horizontal overflow. The search index contains both guides and API
pages. The in-game documentation URL and English button label now target the
site. The Git index, HEAD and all four baseline bundle hashes were preserved;
no content migration to RimWorld 1.7 was made. A hosted Actions/deployment run
still requires these source changes to land on `main`; local build success is
not recorded as a hosted CI run.

GitHub settings were verified with Pages enabled in workflow mode, Wiki disabled
and the repository website set to the documentation URL. No commit or push was
made and no hosted deployment was triggered during this migration.

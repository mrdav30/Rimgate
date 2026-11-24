# RimWorld 1.7 Migration Plan

- **Status:** In progress. Versioned source split and experimental 1.7 compilation
  implemented; the existing local snapshot is temporary migration insurance.
  Live 1.7 startup/profile and hidden-window smoke checks pass;
  [RG-Issue-002](issue-tracker.md#rg-issue-002) is closed. Owned asset loading and
  powered iris close/open rendering pass. Active effects/audio, transit,
  optional-integration and old-save acceptance and the 1.6 freeze/support
  milestone remain pending.
- **Created:** November 23, 2026. **Scope reconciled:** November 24, 2026.
- **Planning baseline:** `14a15f6` (feature-work tracking); clean worktree before
  this plan. Current playable/configured target remains RimWorld 1.6.
- **Tracking:** [Feature work overview](feature-work-overview.md).
- **Historical investigation:** [RG-Issue-001](issue-tracker.md#rg-issue-001), normal
  preferences preservation during isolated launches; closed by decision without
  a reproduced cause. Keep byte-backed config preservation checks.

## Outcome and boundaries

Port Rimgate to RimWorld 1.7/Waylith with a separately owned .NET 10 project,
while freezing the accepted 1.6 implementation and its version-owned content in Git.
Preserve gameplay, def names, saved keys, runtime namespaces and object ownership
unless a specific migration decision and its acceptance evidence require change.

This plan authorizes no support declaration by itself. Current work preserves
accepted 1.6 source/content and brings up a private experimental 1.7 package.
The owner explicitly authorized changing Steam to `unstable` on November 24.
Player configuration, the asset builder and shipped support metadata remain
unchanged. Workshop publication,
unrelated gameplay redesign and broad API prose enrichment are outside scope.

## Framework decision and evidence

**Recommended 1.7 target: `net10.0`, with `LangVersion` pinned to `14.0`.** Use a
compatible .NET 10 SDK and pin the tested SDK when implementation is established.
The game supplies its own runtime; this is a mod class library, not a standalone
or self-contained .NET application.

| Contract | Current 1.6 owner | Proposed 1.7 owner |
| --- | --- | --- |
| Target framework | Existing `net48` | `net10.0` |
| Language selection | Existing `preview`, built with SDK 10.0.302 in the verified baseline | Explicit `14.0`; stable supported pairing with .NET 10 |
| Game reference pin | `Krafs.Rimworld.Ref` 1.6.4871 | Candidate 1.7.5014-unstable; match the actual runtime selected for validation |
| Framework libraries | Existing game/framework references | Standard SDK .NET 10 reference assemblies, plus matching game references |
| Output assembly | `Mod/1.6/Assemblies/Rimgate.dll` | `Mod/1.7/Assemblies/Rimgate.dll` |
| Runtime identity | `Rimgate`, current namespaces and persisted identities | Preserve those identities; version ownership is in directories/projects |

The 1.6 project already uses a modern compiler and `preview` language selection.
The important new capability is the .NET 10 runtime/library contract, not simply
raising a language flag. Leave the 1.6 settings intact during migration. Do not
select a newer TFM just because it is available on the development machine.
Microsoft maps .NET 10 to C# 14 and .NET 11 to C# 15, and warns against a newer
language/runtime pairing or machine-dependent `latest` selection.
[Language defaults](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning),
[configuration guidance](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version).

Ludeon's [announcement](https://store.steampowered.com/news/app/294100/view/672882362547178633)
describes the engine transition, modern C# support and testing before declaring
mod compatibility. The [RimRef maintainer's explanation](https://github.com/krafs/RimRef/discussions/108)
identifies the custom .NET 10 runtime and the change to SDK framework references.
Avoid replacing SDK core-library references with the game's private core library;
that can impose incompatible type identities on other mods. Custom `WaylithClr`
APIs are not assumed available through the SDK references, and this port does not
currently require them.

The [1.7.5014-unstable reference package](https://www.nuget.org/packages/Krafs.Rimworld.Ref/1.7.5014-unstable)
targets .NET 10 and is explicitly prerelease. Inspection of its
`Assembly-CSharp.dll`, `Waylith.dll` and `UnityStubs.dll` found
`TargetFrameworkAttribute = .NETCoreApp,Version=v10.0`. Its Unity compatibility
assembly exposes `AssetBundle.LoadFromFile`, `LoadAsset` and `GetAllAssetNames`.
Those signatures establish an available API surface, not successful loading of
Rimgate's bundles or shaders.

## Proposed source and content ownership

```text
Source/
  1.6/Rimgate/Rimgate.csproj   # net48; retained implementation
  1.7/Rimgate/Rimgate.csproj   # net10.0; active port
Mod/
  1.6/                       # Existing defs, patches, DLL and baseline bundles
  1.7/                       # Validated version-owned content and outputs
```

Start the 1.7 tree from the accepted 1.6 implementation and evolve it separately.
Do not multi-target a single changing source tree or introduce a shared source
project as part of the initial split. Both projects retain assembly name
`Rimgate`; each compiles only its own files and has separate intermediates and
outputs. LoadFolders must select one version's implementation, with no duplicate
assemblies, defs, patches or bundles in the shared mod root.

The build resolver uses `Source/<version>/Rimgate/Rimgate.csproj` without the old
unversioned fallback. Relocation added a directory level: the 1.6 `ModDir`
calculation now uses `../../..`. Preserve C# source and 1.6 defs/patches/bundle bytes during the move;
rebuild and verify its behavior rather than assuming path changes are harmless.
Record artifact identities, accounting for embedded debug/source-path changes.

Record the accepted post-relocation 1.6 commit and retain its source/content.
Once 1.7 is accepted, move active builds, dependencies, RimBridge validation,
CI and documentation forward to 1.7; later 1.6 changes require explicitly scoped
maintenance work. No ongoing 1.6 runtime capture, restoration or live-validation
environment is required. The existing local snapshot remains untouched as
temporary migration insurance. Shared
localization/raw assets and the Unity builder also need an ownership decision
before a 1.7 change could alter the frozen 1.6 result. Retain the original inputs
at the baseline; version assets/tooling only where their contracts diverge.

## Implementation phases and exit evidence

### 0. Establish the actual runtime and protected baselines

- Retain the accepted 1.6 source/content, four baseline bundle hashes and
  tool/reference pins in the revision record. Current local
  pre-migration `Version.txt` reported **1.6.4871 rev590**. The current authorized
  `unstable` installation is **1.7.5014 rev1195**, inspected November 24.
- Use a licensed 1.7 installation and record its executable,
  runtime configuration, game assemblies/build and dependency versions. Do not
  silently switch the player's current Steam branch to obtain it; the November
  24 switch was explicitly authorized.
- The original `Game-1.6` dev view uses junctions to live game data/runtime and
  Workshop dependencies. The existing independent local snapshot is retained
  only as transition insurance; maintaining a frozen 1.6 runtime is not a gate.
- Establish a separate `rimgate17` profile/deployment with recoverable normal
  config backups and verified loaded paths. Move the active workflow to validated
  1.7 and retire routine 1.6 validation at the freeze. Inspect
  actual 1.7 launch arguments/log locations instead of copying Unity conventions.

**Exit:** Exact 1.7 target and dependency graph recorded; an isolated 1.7 profile
and config-preservation checks proven. The compile probe below covers
only the framework portion of this phase.

### 1. Separate projects and make experimental builds explicit

- Perform the source split and path corrections above, then verify source/content
  preservation and a 1.6 build. Runtime changes require their affected gameplay
  checks; a source-path move does not establish ongoing 1.6 live validation.
- Configure the 1.7 project for .NET 10/C# 14 and matching references. Audit
  Harmony 3 against the chosen runtime and required mods; the current 1.6
  Harmony 2.3.6 pin is not an automatic 1.7 decision. Upstream
  [RimBridgeServer](https://github.com/pardeike/RimBridgeServer/blob/main/Source/RimBridgeServer.csproj)
  provides a .NET 10/Harmony 3 example, not a tested Rimgate dependency profile.
- Make dependency restoration version-aware. Preserve all 1.6 hashes and use
  independently pinned 1.7 VEF/KCSG, Bad Hygiene and metadata dependencies.
  Verify native or compatibility-supported dependencies rather than treating
  every existing 1.6 DLL as interchangeable with a 1.7 build.
- Resolve the current helper's coupling to `About.xml` support declarations:
  experimental 1.7 development needs an explicit configured/opt-in path and an
  isolated test-package metadata overlay before shipped metadata claims support.
  Normal release/docs selection must retain supported-target validation; unknown
  versions still fail before tools run. Keep this distinction small and explicit.
- Keep game/framework reference DLLs and SDK runtime files out of mod outputs.
  Do not bundle `System.Private.CoreLib`, `mscorlib` or old Unity engine DLLs.

**Exit:** Both source owners compile independently; 1.6 source/content is
preserved; experimental 1.7 output is correctly routed without a public support
bump. CLI/CI checks exercise target rejection, paths with spaces and clean output.

### 2. Prove startup, assets, patching and one complete gate operation

- Audit direct engine calls and Harmony targets against the actual 1.7 binaries.
  `Harmony_PawnInventoryOption_GenerateThings` patches an enumerator's IL and is
  a specific inspection target; signature-only reference assemblies cannot prove
  its instruction pattern. Verify affected patches were applied successfully.
- Use the required dependency set first. Validate defs, localization, bundle
  enumeration, gate/DHD textures, effects, shaders and sound in a rendered game.
  Keep the 1.6 Unity builder/bundles intact. Decide on legacy-bundle loading or
  a 1.7 asset pipeline from this experiment, not the engine name alone.
- Exercise a gate and DHD through discovery/dialing, pawn/item transfer,
  arrival/cleanup, iris controls and save/reload continuation. The existing iris
  smoke is useful evidence but does not cover transit.
- Discover the installed 1.7 bridge schemas and extend a version-owned scenario
  under `.assets/scripts/`. A headless check cannot establish rendered assets.

**Exit:** One full production path works in the isolated 1.7 game, with patch/log,
visual and save/load evidence plus normal-player config preservation.

### 3. Verify the affected systems and saved-state contracts

Apply the [contributor acceptance matrix](../guides/developer-and-contributor-notes.md#implement-and-verify-the-affected-behavior):
gates/address ownership, cloning, sarcophagus, mobile containers, ZPM/housing,
incidents/quests, UI and relevant optional integrations. Test interruption and
cleanup as well as successful use. Exercise optional integrations absent and
present. Verify a new colony and a copy of an existing 1.6 save, preserving the
original. Record incompatibilities and any deliberate migrations explicitly.

Put defects in the [issue tracker](issue-tracker.md), measured regressions in the
[performance backlog](performance-signal-backlog.md), and keep migration status
here. Engine-wide performance announcements do not establish Rimgate performance.

**Exit:** Relevant behavior/save/dependency checks pass or have an explicit
documented disposition; no unresolved introduced defect is silently accepted.

### 4. Declare support and align documentation/CI

After the compatibility gates pass, update About.xml/LoadFolders, normal build
selection, runtime deployment and the compatibility matrix together. Freeze the
accepted 1.6 source/content and move default builds, dependencies, RimBridge and
CI to the 1.7 project with its own pins. Do not require a standing 1.6 compile or
runtime job after the freeze; later 1.6 maintenance is explicitly scoped.
The single DocFX site on `main` then follows the latest supported target (1.7),
with current guides and that assembly's API reference. It must not switch merely
because an experimental source folder exists. Update scripts, AGENTS and the
specialization when the actual contracts change; archive this plan on completion.

**Exit:** Support metadata matches verified behavior; final 1.6 source/content is
retained; active 1.7 builds/docs/CI select inputs without cross-version outputs.
Workshop publication remains a separately requested action.

## Planning verification and reproducible probe

SDK **10.0.302** compiled the following external class library against RimRef
**1.7.5014-unstable** with zero warnings/errors. The `field` accessor is a C# 14
control; `ThrowIfNull` and span use exercise modern framework references, and
`Verse.Mod`/`UnityEngine.Vector3` exercise game/compatibility references. No mod
code was loaded or executed in RimWorld. The package archive's inspected SHA-256
was `7709fd10e646c82f269ec8d016b879f5ff1772d4473d7f263421e688af6ab76e`.

In an empty directory outside the checkout, create `FrameworkProbe.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.7.5014-unstable" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Create `Probe.cs` alongside it:

```csharp
using System;
using UnityEngine;
using Verse;

public sealed class FrameworkProbeMod : Mod
{
    public FrameworkProbeMod(ModContentPack content) : base(content)
    {
        ArgumentNullException.ThrowIfNull(content);
    }
    public int Amount { get; set => field = Math.Clamp(value, 0, 10); }
    public static Vector3 Origin => Vector3.zero;
    public static ReadOnlySpan<int> Samples => [1, 2, 3];
}
```

Run `dotnet build FrameworkProbe.csproj -c Release --nologo --verbosity:minimal`.
This reproduces compile-time feasibility and does not establish installed-game,
Rimgate, dependency, asset or save compatibility. The unstable pin is a dated
planning input; recheck the actual runtime and corresponding references before
implementation. This earlier planning probe did not relocate source, declare
support, regenerate assets or change player data; implementation evidence follows.

## November 23 implementation: independent baseline and source split

### Preserved 1.6 inputs

Before relocating source, captured the baseline at
`C:\Users\david\AppData\Local\RimgateDev\Baselines\1.6-20261123` as a one-time
capture. The capture tooling was removed on November 24; the snapshot remains
untouched as temporary migration insurance. The verified manifest covers
**8,678 files**, with no reparse points, junctions or shared file links created.
Its SHA-256 is
`dca313bedd8944bd913a1a01440dd30dac2a1a6517c72a59923261855c11ac2d`.

Inputs are the full local **1.6.4871 rev590** game, complete installed Harmony
(`2009463077`), VEF (`2023507013`) and DBH (`836308268`) Workshop packages,
the isolated Profile-1.6 including both bridge test saves, GABS 1.1.2,
RimBridgeServer package 2.5.0, the normal config's backed-up bytes, authored
source/mod/raw-asset/builder/tool inputs, original deployed DLL and a Git history
bundle. Commit baseline is `14a15f6bfa08351efbc774a1fb83a7947e52614c`;
SDK **10.0.302** was recorded. The private profile's ModsConfig reports
`1.6.4871 rev591`; this is preserved historical profile metadata, not a fresh
observation from a running game. This is the minimal development stack, not a
capture of every optional player mod.

The snapshot is separate from the mutable original `Game-1.6` view and is never
the build/output directory. Its source and original artifacts remain recoverable
even when working source changes. The original view, MCP registration, Steam
branch and player config have not been retargeted or rewritten. The Steam app
manifest was not part of the first captured snapshot. The actual Steam manifest
and all normal config files were separately
hashed before/after this change and remained unchanged.

Original compile inputs remain available in the retained snapshot. These commands
can reproduce the initial compile checks without capture/restore tooling; they
are migration evidence, not a standing 1.6 maintenance workflow:

```powershell
$baseline = Join-Path $env:LOCALAPPDATA 'RimgateDev/Baselines/1.6-20261123'
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.6 `
  -RimWorldManagedPath "$baseline/Game/RimWorldWin64_Data/Managed" `
  -SteamModPath "$baseline/WorkshopContent" -LogPath build-baseline-after.log
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.7 -Experimental `
  -SteamModPath "$baseline/WorkshopContent" -LogPath build-experimental-1.7.log
.\.assets\scripts\build-docs.ps1
```

### Source and dependency ownership

- Moved all authored 1.6 source into `Source/1.6/Rimgate/`. All **312 C# files**
  retain their exact bytes; net48, language setting, dependency pins and runtime
  identity are unchanged. Only project root/error paths changed. The solution
  lists both projects and builds only supported 1.6 by default. A locked,
  ignored old `.vs` cache was retained rather than removing user IDE state.
  Follow-up maintenance converts the solution to `Source/Rimgate.slnx`, grouped
  under 1.6/1.7 solution folders with the same 1.7 build exclusion, and removes
  legacy `ProjectGuid` properties from both SDK-style projects.
- Seeded `Source/1.7/Rimgate/` with its own files and net10.0/C# 14 project.
  No shared Compile items or project references connect the two versions.
  Two 1.7-only compile fixes qualify `HarmonyLib.MethodInvoker` to resolve the
  new framework name collision and call `PatchAll(typeof(RimgateMod).Assembly)`
  because Harmony 3 rejects implicit assembly discovery as obsolete. Other
  C# source remains byte-identical to the captured implementation.
- `build-assembly.ps1 -Experimental -RimWorldVersion 1.7` uses pinned game
  references and an explicit captured Workshop root, emitting only to ignored
  `.assets/experimental/1.7/Assemblies/`. Normal assembly/asset/release builds
  still reject 1.7 before invoking a toolchain. About.xml and LoadFolders remain
  unchanged, with no `Mod/1.7` folder. DocFX continues to build supported 1.6.
- A snapshot-backed setup option was initially added and fixture-checked without
  changing the existing deployment or registration. It was removed on November
  24 along with capture tooling; normal installed-game setup and player-config
  backup/verification remain.

The captured packages already include 1.7 dependency folders. Initial compilation
used these exact provisional inputs; no claim is made that they match a tested
1.7 game or are portable CI pins:

| Assembly | Assembly version | SHA-256 |
| --- | --- | --- |
| Harmony `0Harmony.dll` | 3.0.0.0 | `7252fbe77bc96fcfb5e6aa446a4957f4547de312d889046fe3ec7d15f7f0fcf5` |
| `VEF.dll` | 1.1.7.0 | `8557af77a8e4cc2915c2de710f2f6a4d167f2989704014918a8369f133dbf13a` |
| `KCSG.dll` | 1.1.2.0 | `aa61be424b21d1a2a47bf414cac0746a3ffcf1930cf71983ebb742cd64e3fab9` |
| `BadHygiene.dll` | 1.0.0.0 | `475ee45786db312118e88d5f060da9cb8d45ef9ff3b041c26c85d0d3ed3e7b0b` |

### Verification and remaining work

- Installed-reference 1.6 builds before and after relocation: **zero warnings,
  zero errors**, both using the independent game/Workshop copies.
- Experimental 1.7 Release and Debug builds: **zero warnings, zero errors** after the two
  compile fixes. No dependency DLLs are copied into its output directory.
- Full portable 1.6 library/DocFX build: **zero warnings, zero errors**;
  generated-site guide inventory, local links and binary-exclusion checks pass.
- MSBuild evaluation finds 312 independent C# inputs per project. Direct wrong
  version guards, normal 1.7 assembly/asset/release rejection and unsupported
  experimental versions were checked. All PowerShell scripts parse.
- Default solution build succeeds and selects only 1.6. The relocated 1.6 build
  also succeeds when invoked by full script path from outside the repository,
  using paths containing spaces.
- PowerShell entry points and the shared resolver now expose native comment-based
  help, with parameter descriptions and runnable usage examples. Help discovery
  and syntax checks are separate from executing tools with side effects.
- Before removal, a separate small game/tool fixture exercised fresh capture in the dirty,
  relocated worktree, existing-destination rejection, deliberate hash corruption
  detection and refusal to put mutable setup output inside a snapshot. The
  config-verification mode is checked with fixture config; the real 8,678-file
  snapshot and complete Git bundle were verified successfully. These are
  historical checks of the removed tooling, not ongoing workflow requirements.
- Source preservation, four baseline bundle bytes, support metadata, normal
  config, Steam app manifest and unstaged Git state are checked separately.
  Rebuilt DLL bytes can differ because embedded debug/source paths changed;
  the original deployed DLL remains in the snapshot.

No game was launched, no 1.7 installation obtained and no live smoke/save-load,
asset, patch-IL or optional-integration acceptance is claimed. Keep those phase
0/2/3 gates open; compilation does not freeze 1.6 or establish public 1.7 support.

## November 24 scope correction: freeze source/content and move tooling forward

Removed maintained baseline capture and snapshot-backed setup support. Keep the
existing local snapshot as temporary migration insurance; no restore command or
recurring captures are planned. Acceptance of 1.7 moves active development,
dependencies, RimBridge, documentation and CI forward. The accepted 1.6 source
and content remain in Git, with later changes limited to explicitly scoped
maintenance. Normal player-config backups and post-run checks are still required.

Cleanup verification: all eight remaining PowerShell scripts parse and expose
help; the removed setup option is rejected during parameter binding before work
starts. Fixture config verification accepts unchanged files and detects changes
without restoring them. Skill validation, experimental 1.7 compilation and the
supported 1.6 library/DocFX build pass; builds report zero warnings/errors and
generated-site checks pass. All 8,678 local snapshot files and its manifest remain
unchanged. The staged index, prior working edits, gameplay source, bundles, Steam
metadata and player configuration are preserved. No game or setup refresh ran.

## November 24 runtime bring-up and portable 1.7 builds

Base revision: `6c0eab2`, clean worktree before this step. The owner authorized
switching the Steam branch after confirming no independent 1.7 copy was present.
Ludeon's documented public testing route is `unstable`; Steam completed the
download as build **26793372**, version **1.7.5014 rev1195**. The old 1.6 snapshot
remains untouched migration insurance. No public support declaration was added.

### Implemented and observed

- Added four public 1.7 compile inputs to `.assets/build-references.json`, using
  immutable commit URLs and SHA-256: Harmony `dc540c7e2674f3c03dc938fb0b33fbe60cfba2fa`,
  VEF/KCSG `090437424d981b0a130c819dee27b174aa30633e`, DBH
  `96ec4b51efaf9ef4baa98aa832a8078be09d3f45`. All existing 1.6 pins are unchanged.
  Restoration selects the requested version into `.assets/references/<version>`;
  the documentation workflow now also compiles experimental 1.7 without Steam.
- Version-aware setup inspects the installed build before writing a deployment.
  The 1.7 view copies the self-contained runtime's root files and junctions
  `Data`, `Rendering` and `vulkan`. `RimWorld.runtimeconfig.json` confirms
  `net10.0` and runtime **10.0.15-waylithclr.dev**. The private executable is
  `Game-1.7/RimWorld.exe`; the only launch argument is
  `-savedatafolder=<ToolsPath>/Profile-1.7`.
- The actual `EarlyPrefs.PrefsFilePath`, `GenFilePaths.SaveDataFolderPath` and
  cache override read `savedatafolder`. Setup's generated XML now uses the exact
  lowercase `fullscreen` field. An initial case-mismatch error was corrected in
  the private prefs and setup template; the clean restart has zero ERROR headers.
- The private mod overlay reuses 1.6 XML/bundle bytes, shared About/languages and
  the new assembly. Only this ignored overlay declares/loads 1.7. Source-owned
  `Mod/About/About.xml`, `Mod/LoadFolders.xml`, 1.6 source and baseline bundles
  remain unchanged. No Unity rebuild or new asset pipeline was introduced.
- Installed 1.7 VEF remains the earlier `8557af77...` binary recorded above;
  the public pinned VEF binary is
  `d3559813de4e3f91ad9862d756ca2f01548c7bf8e5f01fdc5afadd7fdf485fd9`.
  Builds with both sets pass; they are distinct compile/runtime input records,
  not an assertion that the binaries are identical. Runtime acceptance of the
  public dependency set remains separate from its CI compilation.
- The real `PawnInventoryOption/<GenerateThings>d__6.MoveNext` contains one call
  to `ThingMaker.MakeThing(ThingDef, ThingDef)`, matching the transpiler's expected
  signature/pattern. No Rimgate Harmony startup error appears in the clean log.
- GABS **1.1.2** connects to bridge **2.5.0.0**, SDK **2.0.0.0**, with **130 tools**.
  All ten configured mods are loaded from the private view, with zero version,
  dependency, ordering or session-mismatch issues. DBH is absent at runtime.
  Context reporting is still `unknown`; OS arguments plus live config/mod paths
  independently verify the selected profile. After hot configuration reload,
  single-tool detail lookup inconsistently reports an unknown game; filtered
  `games_tools` exposes schemas and fully qualified `games_call_tool` names
  without an extra gameId successfully invoke them. No upstream fix is claimed.
- The Lua file compiles and starts a fresh colony, spawning a real Dwarfgate and
  DHD. It then fails at synthetic selection; direct selection and screenshot
  attempts also time out. That initial run did not reach save/load, tick,
  rendered asset, transit or optional-integration acceptance. The issue was
  subsequently resolved for hidden-window automation as recorded below.

Computer Use was stopped with the physical Escape key before the native-window
activation control. All UI automation stopped and the private game was gracefully
terminated. Setup's player-config verification passed, and a separate byte-backed
comparison confirmed all **125** normal config files unchanged. The runtime's
shared application log is preserved privately at
`%LOCALAPPDATA%/RimgateDev/evidence/1.7/startup-and-smoke.log`; the isolated cache
and saves use Profile-1.7, but Waylith's Player.log uses the normal application
`Logs` folder. Native Vulkan layer warnings were observed without startup errors.

The input/capture follow-up below reran the unchanged smoke assertions. Continue
with the complete gate-transit/asset/save matrix after that bounded acceptance. The test
package, active GABS configuration and experimental CI compile are preparation;
they do not freeze 1.6 or satisfy the public 1.7 support gates.

Local validation passed for the public-reference 1.7 build, supported 1.6
library/DocFX build and generated-site checks, with zero build warnings/errors.
PowerShell syntax/help, unknown-target and corrupt-cache rejection, pre-write
game/reference-pin guards, skill validation and 43 local links/anchors passed.
The Git index and HEAD are unchanged; 1.6 source/content, prior dependency pins,
normal configuration and all 8,678 insurance-snapshot files remain intact.
The added CI commands were exercised locally; no workflow was pushed or run remotely.


## November 24 background input and capture acceptance

Based on committed preparation `918d34c`, with no desktop automation or native
window activation. [RG-Issue-002](issue-tracker.md#rg-issue-002) is closed as a
**verified launch-workflow fix**, not an engine or Rimgate gameplay patch.

The original operation `op_a6aff743a4a0490d82c2877cb1b81156` spawned a gate at
`(110,130)` and DHD at `(110,134)`, then failed selection after 2,000 ms. The
selection stayed empty despite read-only inspection confirming the gate and no
blocking window. Screenshot capture separately timed out after about ten seconds
without a PNG. The bridge reported `visualReady` and its input patches applied;
those flags did not establish GUI/render progress. A foreground control was
cancelled before it ran, so the exact original internal pause cause is unproven.

Upstream's [Waylith launch inventory](https://github.com/pardeike/RimBridgeServer/blob/60c951ce494b47db69387b2b3347756aede45264/skills/rimworld-17/references/launch-arguments.md)
and [autonomous development evidence](https://github.com/pardeike/RimBridgeServer/blob/60c951ce494b47db69387b2b3347756aede45264/skills/rimworld-17/references/autonomous-development.md)
distinguish graphics-enabled `-NoWindow` from graphics-free `-Headless`, and
identify focus-related pause handling. Their earlier macOS results were a lead;
the acceptance here is Windows **1.7.5014 rev1195**, Steam build **26793372**,
.NET **10.0.15-waylithclr.dev**, GABS **1.1.2**, released bridge **2.5.0.0**
(commit `b861da3219d1c0b2dd3b5e9d5052aeaf95c697b7`) and SDK **2.0.0.0**.
No newer bridge, optional companion or SDK was installed for this fix.

Experimental setup now generates this argv array:

```json
[
  "-savedatafolder=C:\\Users\\david\\AppData\\Local\\RimgateDev\\Profile-1.7",
  "-NoWindow",
  "-WindowSize=1280,720"
]
```

The fixed size makes captures comparable; hidden graphics mode avoids occupying
the desktop. The launched process argv independently matched these flags, and
its `MainWindowHandle` was zero. Profile/config and Rimgate deployment paths were
verified by the unchanged guarded Lua case. Ten loaded entries remained Harmony,
Core, five DLC, installed VEF, Rimgate and RimBridge, with zero configuration
issues. The installed/public VEF distinction recorded above still applies;
DBH remained absent at runtime.

Acceptance operation **`op_c5f325f718794996b864aeaa46e9b9fc`** completed in
23,473 ms: script success true, 22 capability calls passed and zero failed steps.
The gate was `Thing_Rimgate_Dwarfgate45149`, DHD
`Thing_Rimgate_DialHomeDevice45150`, gate address **P9F-659**. Selection succeeded
in 56 ms and after reload in 63 ms. The real iris gizmo added the iris, a save
persisted it, removal changed inspect state, and reload restored identical gate
identity and iris/address text. Exact stepping advanced **120 ticks across 120
frames**; this asserts the delta, not a fixed initial tick after loading.
Screenshot capture took 134 ms and produced a decoded **1280x720**, **2,015,381
byte** PNG. The bridge error journal was empty and the native log had zero ERROR
headers. A native ruin-generation warning and existing Vulkan-layer warnings
were observed; no blanket warning suppression was added.

The screenshot was visually inspected: the gate and its saved iris/address UI
render, but the iris/force-close command icons show red X placeholders. DHD
placement is proven by its spawn payload; this capture does not establish its
texture correctness. These observations remain inputs to the existing complete
asset acceptance gate; capture success is not full texture/bundle compatibility.
Gate transit, optional DBH presence, the public dependency runtime set and old
1.6 saves were not tested by this case.

Private evidence is under `%LOCALAPPDATA%/RimgateDev/evidence/rg-issue-002/`:
`hidden-window-smoke.json`, `hidden-window-smoke.log`, the screenshot, and a
byte-backed snapshot/manifest of normal config before launch. The generated
save is `Profile-1.7/Saves/Rimgate-bridge-smoke.rws`. These paths are supplementary;
the checked-in script, argv, input versions, results and limitations above retain
the reproduction contract if private evidence is later removed.

The private game was stopped and termination verified. Setup's config verifier
passed; a separate recursive comparison found all **125** normal config files
unchanged with no additions. The 1.6 source/content and local insurance snapshot
were not modified. Reopen RG-Issue-002 if input/capture fails under this hidden
graphics profile, retaining actual argv, build/dependencies, payloads and
frame/render evidence. Do not weaken smoke assertions to hide a recurrence.

Local verification also passed the real experimental setup/build (zero warnings
or errors), GABS configuration validation, Windows PowerShell 5.1 parsing and
parameter help, both skill validators, 44 local Markdown links/anchors, and the
supported 1.6 library/DocFX/site checks (zero warnings or errors). The generated
GABS argv was checked against the tested hidden graphics profile. Git diff checks
passed; HEAD remained `918d34c`, with no staged changes or tracked source/content/
dependency-pin changes. All follow-up edits remain unstaged/uncommitted.


## November 24 asset loading and iris rendering pass

Based on `f70a1b6`, using the same Windows 1.7.5014 rev1195 installation,
10.0.15-waylithclr.dev runtime, GABS 1.1.2 and released RimBridge 2.5.0.0 /
SDK 2.0.0.0. The ten-mod isolated dependency set is unchanged; the runtime VEF
versus public CI reference distinction still applies. No desktop automation,
Steam branch change, public support bump, gameplay source edit or bundle rebuild
was needed. Two subagents audited source/raw assets and an additional subagent
implemented the development probe; an independent read-only review checked its
ownership and non-vacuous command assertions.

### Red X diagnosis

`Building_Gate.GetGizmos` creates the dev-only Add/remove iris and Force close
actions without assigning `icon`; the owning 1.6 and 1.7 files are identical.
Inspection of actual installed 1.7 `Verse.Command.DrawIcon` IL shows that a null
icon uses `BaseContent.BadTex`; the read-only 1.6 insurance assembly has the same
behavior. The earlier capture's red X observation was real, but its suggestion
of missing command assets was unproven. This pass establishes an inherited dev
UI default, not an engine migration defect. Those commands were preserved.
The player's `Comp_ToggleIris` command instead uses the existing
`UI/Button/RGIris_Close` and `UI/Button/RGIris_Open` textures.

### Inventory and development tooling

The retained manifest has **840 assets**, exactly matching **814 raw PNGs** and
**26 raw OGGs** by name. One YAML path wraps onto a continuation line; counting
only lines beginning with `- Assets/` would incorrectly report 839. Source/XML
path audits found no unresolved candidates, including checked vanilla/DLC paths.
There are no bundled custom shaders/materials: effects use game shader entries.
This inventory comparison is by names, not raw-source versus bundled-byte equality.

Setup now builds `.assets/tools/RimgateAssetProbe/RimgateAssetProbe.csproj`
against the actual 1.7 game and SDK 2.0.0, deploying only the development companion
and generated raw-path manifest to `Game-1.7/BridgeTools/Rimgate/`. It adds one
read-only capability, `rimgate/asset_report` (**131** total live tools), outside
the Rimgate player package. Each named object loads directly from Rimgate's own
bundle, with normal `ContentFinder` lookup checked for the same object. Positive
texture dimensions, finite positive audio duration, sample count, channels and
frequency are required. The report also checks static textures/materials/shaders
and selected gate command/iris texture semantics. A main-menu inventory report
is supported; selected-command acceptance explicitly requires one equipped gate.

A deliberately nonexistent manifest texture `UI/AssetProbe-DeliberatelyMissing`
produced `success:false`, with both missing bundle entry and missing texture
reported, operation `op_d296ce832a5a4428ab9c00e82666370d`. The operation handler
itself completed successfully, demonstrating why nested results matter. Restoring
the exact external manifest bytes returned `success:true` in
`op_b6dce2509dd94fcca4eb4bb71fc1e666`. No baseline bundle was modified by this control.
During probe development, a main-menu selector cast failure was corrected with
an explicit Playing/current-map guard; final acceptance used a clean rebuilt,
deployed and restarted companion.

Independent review also moved the loaded-deployment root guard ahead of the
first save load. A wrong-root control, `op_53aa4aafb70f4bae999f266aab9ae307`,
failed at that guard after only the configuration query; state remained Entry
with `hasCurrentGame:false`. The final corrected Lua compiled into 139 top-level
statements and its full positive scenario passed below.

### Runtime and rendered acceptance

The unchanged base smoke passed in **18,848 ms**, operation
`op_b1caaa5dba6e4d6d8186fa32c5da4c3a`, including iris mutation/save/reload,
120 exact ticks and capture, with no failed script steps. It produced owned gate
`Thing_Rimgate_Dwarfgate38637`, DHD `Thing_Rimgate_DialHomeDevice38638`, address
**P3F-539** and the `Rimgate-bridge-smoke` save.

The version-owned `.assets/scripts/rimbridge-assets-1.7.lua` then loads that save,
verifies the private config/deployment and selected equipped gate, clears fog,
places/claims a Vanometric power cell at `(105,130)` outside the gate footprint,
and allows 750 ticks for the normal power net. It requests Close iris through
the real gizmo, orders a colonist through the real context-menu toggle job and
steps 600 ticks. It captures the closed overlay and repeats the real job to
reopen, with another 600 ticks. This is fixture power, not an unlimited-power
cheat or direct iris-state assignment. Reruns reload the owned save first; the
case does not overwrite it.

Final clean-run operation **`op_cab033aec99f4297ac85b94d75a2f607`** passed in
**42,801 ms**, with **31 successful capability calls** and zero failed steps.
Both **1280x720** screenshots were inspected: gate, revealed DHD, closed iris
overlay, reopened gate, corresponding Open iris/Close iris textures and unchanged
dev glyphs render. The final report `op_4032f3909b324be2abeaf0cbb135c538` resolved
all **814 textures / 26 clips**, with no missing bundle entries, invalid metadata,
shadowed content, bad static material/texture or invalid selected command.
Both static force-field materials use valid **MoteGlow** shader/texture pairs;
this validates resolution, not a rendered shield-effect test.

The final bridge error journal was empty and native log had zero ERROR headers.
The private game was stopped and termination verified. Setup's verifier passed;
a recursive pre/post comparison found all **125** normal player-config files
unchanged, with no additions. Tracked 1.6 source/content, raw inputs, builder,
dependency pins, four bundle bytes and the local insurance snapshot were preserved.

Private evidence is under
`%LOCALAPPDATA%/RimgateDev/evidence/asset-pass-20261124-115053/`: baseline-smoke,
asset-scenario, guarded-asset-scenario, asset-report, menu-asset-report,
missing-texture-control, restored-manifest-control and wrong-deployment-control
JSON; final-asset-pass.log; both PNGs; and recoverable
normal config bytes/manifest. The checked-in scenario, probe source, setup,
input identities and results above retain the reproduction contract if temporary
evidence is removed. The owned save is under `Profile-1.7/Saves/`.

### Disposition and remaining gates

The existing legacy bundles load correctly for all enumerated objects on this
build. There is no evidence requiring an asset-pipeline or importer change.
Retain the existing bundles and builder; version new inputs/tooling only when an
actual 1.7 requirement diverges. No issue ID was assigned to the inherited dev
icon defaults, since this investigation did not establish an asset defect.

This pass does not establish visual fidelity of every rotation/effect, active
DHD/chevron/event-horizon rendering, or audible playback (the isolated profile is
muted). Carry those behavior-dependent checks into the complete dialing/transit
scenario. Public dependency runtime parity, optional DBH, other stateful systems,
old 1.6 saves and public support/freeze remain separate gates. A manifest match
and positive clip metadata must not be reported as audible or full gameplay acceptance.


Windows PowerShell 5.1 setup/build/deployment and GABS configuration validation
passed. Both the experimental mod and companion built with zero warnings/errors.
The supported 1.6 library/DocFX build and generated-site checks also passed with
zero warnings/errors. PowerShell parsing/help, the Rimgate skill validator,
37 local Markdown links/anchors and Git diff checks passed. HEAD remains
`f70a1b6`; the index is unchanged and all follow-up edits are unstaged/uncommitted.

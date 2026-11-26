# RimWorld 1.7 Migration Plan

- **Status:** In progress; **phases 0, 1 and 2 complete. Phase 3 is active.**
  The background core matrix now accepts native assets, planetary/orbital travel,
  living return, current-save ownership, medical/cloning, conditions/incidents,
  native manifests/DHD cancellation, cart recovery/fuel/refrigeration, unloaded
  cargo/loose corpse continuation and ZPM facilities/scheduled raids. Optional
  present-dependency clinical/facility checks have finished their bounded pass;
  23-mod startup, content/hook contracts and representative actual operations
  pass, with qualified branches retained in the [optional acceptance record](#final-optional-integration-acceptance).
  [RG-Issue-028](issue-tracker.md#rg-issue-028) remains a separate nested-corpse
  candidate needing reproduction. Continuous-motion, representative
  native appearance and actual device/listening acceptance remain coordinated
  manual gates. See [remaining gates](#remaining-phase-3-gates) and the dated
  records below for exact scopes and rejected fixtures. Only **1.6** is publicly
  supported; **phase 4** owns acceptance, default/support changes and its freeze.
- **Created:** November 23, 2026. **Scope reconciled:** November 25, 2026.
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

| Phase | Current disposition | Owning evidence / next gate |
| --- | --- | --- |
| 0 - Runtime and protected baselines | Complete | Exact licensed runtime, isolated profile and byte-backed preservation checks established. |
| 1 - Versioned experimental projects | Complete | Independent 1.6/1.7 source, explicit experimental output, pinned references and CI compilation. |
| 2 - One complete production gate path | Complete | [Phase-2 closeout](#phase-2-completion): startup/patches, native assets, planetary discovery, dialing, pawn/inventory transit, save checkpoints, closure and ordinary/conditional iris presentation. |
| 3 - Wider feature/save/dependency matrix | In progress | [Remaining gates](#remaining-phase-3-gates): broader logistics, medical/resource lifecycles, orbital operations, optional integrations and coordinated manual quality checks. |
| 4 - Declare support and move defaults | Pending phase 3 | Public metadata, active 1.7 tooling/docs/CI and final 1.6 source/content freeze. |

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

### 2. Prove startup, assets, patching and one complete gate operation - complete

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
present. Verify a new colony and current 1.7 save/load continuation. The owner
waived historical player-save migration on November 25 after the retained-save
trial; preserve the originals and record that disposition explicitly.

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


## November 24 native Waylith pack comparison - scope and acceptance

The owner authorized comparing the native 1.7 pack path with the validated Unity
bundles before continuing transit work. The prior asset pass proved legacy
compatibility; it did not establish that Unity is the preferred 1.7 builder.
The installed 1.7.5014 baker accepts `-BakeModAssetPacks=<mod-root>` and writes
native `Assets.zip` plus stamp/role sidecars for applicable load folders.

This slice keeps `AssetsRaw`, the Unity builder, tracked 1.6 source/content and
all four bundle bytes intact. Build only an ignored experimental 1.7 candidate,
using the same private game/dependencies, owned smoke save and hidden renderer.
Stage raw `Textures`/`Sounds`, bake through the game's native executable, retain
its pack metadata and remove loose inputs plus Unity bundles from the native
test deployment. Verify actual native ownership rather than fallback lookup.
Public support metadata, 1.6 freeze and release packaging do not change here.

Compare complete inventory, texture dimensions/format/mips/filtering, audio
metadata, package bytes and the same rendered powered iris scenario. Native
baking does not consume Unity importer settings; record differences and inspect
alpha/edge rendering instead of treating successful name lookup as equivalence.
Use equal source identities and note existing bundle provenance limits. Do not
claim startup/memory improvements from one run or call inherited caches cold.
A passing native candidate informs the next 1.7 tooling decision; active effects,
audible playback, transit, dependencies and old-save gates remain open.

The comparison results and subsequent native-default decision are recorded below.
No extra feature plan or support declaration is implied.


### Native comparison implementation and results

Based on `cd02845`, Windows RimWorld **1.7.5014 rev1195**, runtime
**10.0.15-waylithclr.dev**, unchanged ten-mod profile/dependencies, bridge
**2.5.0.0** and SDK **2.0.0.0**. Both runs use the same owned
`Rimgate-bridge-smoke` save (gate 38637, DHD 38638, address P3F-539), fixed
1280x720 hidden rendering and unchanged 1.7 gameplay assembly/source. The current
installed/public VEF distinction remains; this is not dependency-parity acceptance.

The initial comparison added experimental `-AssetFormat UnityBundles|WaylithPack`
to setup and retained **UnityBundles as default** at that stage; the subsequent
owner decision below changes the 1.7 default. The native builder stages only ignored 1.7 inputs,
launches `-NoWindow -WindowSize=1280,720 -savedatafolder=<external-bake-profile>
-BakeModAssetPacks=<ignored-mod-root>` and checks child exit plus four nonempty
pack files. It removes prior known outputs before a fresh bake, then removes
only staged source folders/legacy bundles. No Unity editor was used, no tracked
asset was regenerated, and public support/release selection remains 1.6.
The initial Windows PowerShell 5.1 wrapper could not read ExitCode after its
wait despite a successful bake; retaining the process handle before waiting
fixed it. Actual 5.1 setup and standalone rebake subsequently passed.

Actual baker IL uses BC7 target/quality 1, Kaiser full mip generation, disabled
RDO and no Unity .meta reader; grayscale assets may choose BC4. XML
`borderMipMaps`/`uncompressed` flags are recognized, but neither restores the
old no-mip/bilinear/clamp import policy. A bounded inspection found no supported
per-texture native pack metadata for disabling mips or selecting filter/wrap;
matching that policy would require deliberate further runtime/tooling work.

The successful setup bake reported **840 entries, 0 reused, 48 deduplicated,
0 failed**, **8.3 seconds** for encoding/baking; total wrapper time was **14.51
seconds**. A standalone rebake passed in **10.49 seconds** total and produced
the same Assets.zip SHA-256 as the deployed/tested candidate. These are local
build observations, not a startup or general performance benchmark.

| Comparison | Retained Unity bundles | Native Waylith candidate |
| --- | --- | --- |
| Resolved source inventory | 814 textures, 26 clips | 814 textures, 26 clips; 840 owned current pack entries |
| Texture width/height | Existing loaded dimensions | All 814 match Unity and source dimensions |
| Formats | 708 DXT5, 99 DXT1, 7 RGBA32 | 752 BC7, 62 BC4 |
| Mip levels | 1 on all textures | Full chains: 6-11 levels |
| Sampling | Bilinear / Clamp on all 814 | Trilinear / Repeat on all 814 |
| Asset payload bytes | 10,016,071 rimgate_core | 19,332,224 Assets.zip |
| All four artifact bytes | 10,097,945 | 19,489,545 |
| Powered iris scenario | Passed, 42,041 ms | Passed, 42,037 ms |

The native pack is about **1.93x** the four-file legacy set. Its format/mipmap
policy differs, so the size result does not isolate container overhead or imply
a quality-equivalent performance regression. All PNG source dimensions are at
most 1024; 70 images are non-power-of-two. Runtime comparison found no dimension
change. Existing Unity bundle provenance establishes names and loaded objects,
not decoded byte parity with current raw inputs; no image-equality claim is made.

The two native screenshots show the same gate/DHD and correctly closed/reopened
iris with valid command icons and transparency in the exercised views. The gate
looks visibly softer at the tested zoom, consistent with the changed sampling
and mip policy; it is not being silently accepted as identical presentation.
Other UV/rotation/effect states remain unverified.

Audio channels/duration/sample metadata match for 24 clips. The two differences
are recorded rather than normalized away:

- `Ability/RG_Wraith_Cocoon_Cast`: Unity 48,000 Hz / 12,049 samples;
  native 96,000 Hz / 24,098 samples, same duration. Reading the raw Vorbis
  identification header and final Ogg granule confirms the native values match
  the source; the imported Unity clip uses half that rate/sample count.
- `Building/RG_Sarcophagus_Running`: both stereo 44,100 Hz; Unity 73,600 samples
  / 1.6689342 s, native 73,664 / 1.6703855 s. Raw Ogg metadata matches native.
  The 64-sample (~1.451 ms) difference does not establish its import mechanism
  or audible loop behavior. The profile remained muted; audible acceptance is open.

The probe now uses actual public `Waylith.Assets.ContentPack` mount/current-entry
APIs, exact pack path and Rimgate's own content holder. It rejects bundle/loose
fallback and compares normal content lookup with the owned object. The native
pack had 840 content hashes/current entries, zero loaded bundles and no loose
Textures/Sounds folders. Both full inventories, static materials/shaders and
real command/iris checks passed. Script operation IDs:

- Unity: **`op_b46526f1c67a4f6fa253ea5270047449`**.
- Native: **`op_5b0e7067aec641738e6b30990c6085e6`**.

Native negative controls detected a missing manifest entry
(`op_3c67aa867c524369ba984fb62c8734dc`) and an injected loose-file directory
(`op_ffb2d09619a649aeafd8b272d5aebc7c`). Restoring exact manifest bytes/removing
only the known loose control returned success in
`op_24f27fad74a24d7394b23032574ef52c`. No fallback was needed to pass. Both
scenarios ended with empty bridge error journals; native logs had zero ERROR
headers. The baker's stderr also contained a Vulkan/Fossilize layer diagnostic;
its successful output and exit were independently checked, not inferred from
silencing that diagnostic.

Native outputs retained together:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| Assets.zip | 19,332,224 | 0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797 |
| Assets.zip.stamps | 83,135 | 915d23b0ed98fc33af4896bb35b35fc166f6962948855cc3a2e6f2bcb3d3d274 |
| Assets.zip.roles | 74,016 | 26f1588ebd14bd3c8ff001f89b3943b04a9782c0da4242221f0fc119e78c38c2 |
| Assets.zip.fingerprint | 170 | 589f238100089732167a8b91078d0e20b443371d7c167648e6c3e86e61b79c1d |

Private evidence is `%LOCALAPPDATA%/RimgateDev/evidence/native-pack-20261124-123501/`:
per-format complete reports/scenario results, original/open/closed PNGs, native
bake/rebake logs and artifact identities, both negative controls plus restored
report, comparison JSON, normal config byte copies and hashes. No private logs,
endpoints, generated packs or source art are added to tracked player content.
The checked-in builders/probe/scenario and input/result details here retain
reproduction independently of those temporary artifacts.

**Initial comparison disposition (superseded for the default):** Native packaging
was viable but initially remained opt-in while presentation differences were
reviewed. The owner's subsequent selection below makes it the 1.7 development
default; the recorded size, sampling and audio observations remain valid.
The 1.6 freeze, public 1.7 support, optional integrations and old-save acceptance
remain open.


Final verification: actual Windows PowerShell 5.1 setup passed for both formats;
standalone native rebaking passed with byte-identical Assets.zip; parser/help and
unsupported-1.6 native rejection checks passed. Both mod/probe builds and the
supported 1.6 library/DocFX/site build passed with zero warnings/errors. The
Rimgate skill validator, 37 local Markdown links/anchors and Git diff checks
passed. Both runtime processes and the baker were stopped/finished. All **125**
normal config files matched their pre-comparison byte-backed hashes, with no
additions; the four tracked Unity baseline hashes and protected source/raw/builder/
reference inputs were unchanged. The local insurance snapshot was not modified.
HEAD/index remain `cd02845`/unchanged; all edits and the new builder are unstaged.


### November 24 owner decision - native default for 1.7

The owner selected Waylith packs as the 1.7 development default to remove Unity
from active 1.7 asset building, with continued checks that textures retain their
intended appearance. Setup now resolves an omitted AssetFormat by target:
**1.7 -> WaylithPack**, **1.6 -> UnityBundles**. Explicit UnityBundles remains
available for 1.7 comparison/reproduction; explicit native format for 1.6 is
still rejected. The script's default game target remains 1.6, and public support,
release packaging and DocFX target selection have not changed.

Use `setup-rimbridge-dev.ps1 -RimWorldVersion 1.7` for the native development
baseline and pass `assetFormat: WaylithPack` to the asset scenario. The recorded
pack-only inventory, dimensions, command icons and powered iris results support
this default. The earlier gate capture was visibly softer at the tested zoom;
that observation is retained rather than claiming identical pixels. Sampling
policy selection does not close broader texture appearance, rotations, active
DHD/event-horizon/chevron effects, audible playback or complete transit gates.
Investigate unexpected distortion, seams, missing detail or incorrect asset
selection in the owning 1.7 path before accepting the affected behavior.

The retained 1.6 source, content, Unity builder/bundles and local insurance
snapshot remain untouched. No additional rebuild or maintenance of 1.6 assets
is required to change the experimental 1.7 default.

Follow-up verification used the actual Windows PowerShell 5.1 parameter block:
omitted target/format resolves to 1.6/Unity, explicit 1.7 to native, and explicit
1.7/Unity remains honored. Running setup with only `-RimWorldVersion 1.7`
successfully baked/deployed native assets in 15.13 seconds. The deployed pack
matches the previously runtime-tested SHA-256 above, with no loose/bundle fallback
folders. Gameplay was not rerun for this selection-only change; the prior native
scenario remains the runtime evidence. All 125 normal config files and four 1.6
bundle hashes were preserved. The mod/probe and supported 1.6 DocFX/site builds
passed with zero warnings/errors; skill/diff checks and independent review passed.
Private byte backups and verification JSON are under
`%LOCALAPPDATA%/RimgateDev/evidence/native-default-20261124-132336/`.
Prior comparison edits and these follow-up changes remain unstaged/uncommitted.


## November 24 complete gate-operation acceptance - scope

The owner authorized the next phase-2 slice after committing native asset tooling
as `068b95e`: known-destination registration/dialing, normal pawn/item transfer,
arrival/cleanup and save/load continuation on the native 1.7 baseline. Retain
1.6 and player data; do not declare public support or expand to all phase-3 systems.

Use the owned smoke save and a controlled lazy destination site. Fixture
helpers may register that site, stage actors and a uniquely identified inventory item;
they must not open the gates or directly relocate a traveler across maps to
obtain a passing result. Dial, enter and close through the real DHD/context menus
and mod jobs. Distinguish fixture address registration from glyph/quest progression.
Check reciprocal gate/map references, pending/active state, exact pawn/item
identity and ownership, buffers, post-arrival state and clean closure.

Save/reload checks must include queued dialing, an active connection and an
in-flight checkpoint where practical, then continue the actual operation.
Read-only snapshots and saved XML should distinguish a real persistence defect
from a UI/fixture problem. Source candidates involving the world-component cache
and reference-only buffers require reproduction before selecting a runtime fix.
Keep results, limits and any confirmed issue links here rather than a parallel plan.

### Complete-operation result and persistence corrections

**Accepted slice:** The known-address production path passes on native packs.
This does not close all of phase 2 or declare public 1.7 support. Actual target:
Windows RimWorld **1.7.5014 rev1195**, Steam build **26793372**, custom runtime
**10.0.15-waylithclr.dev**; hidden graphics profile, Harmony **3.0.4.0**, Core and
all five DLC, VEF, Rimgate and RimBridge **2.5.0**. GABS is **1.1.2**, bridge SDK
**2.0.0.0**. Workshop VEF SHA-256 is
`8557af77a8e4cc2915c2de710f2f6a4d167f2989704014918a8369f133dbf13a`;
the public CI reference remains separately pinned at
`d3559813de4e3f91ad9862d756ca2f01548c7bf8e5f01fdc5afadd7fdf485fd9`.
These are distinct dependency contexts, not a parity claim. Bad Hygiene was absent.

The development-only companion now offers four fixture/observation tools beside
the asset report. It references the experimental assembly without copying it or
SDK/game libraries into the player mod. The repeatable scenario is
`.assets/scripts/rimbridge-transit-1.7.lua`; the contributor guide owns invocation.
Fixture source tile **6293** (`P3F-539`) and lazy destination **77722** (`P8N-331`)
use normal site registration. Real DHD dialing generates the second map; helpers
never activate gates or relocate travelers across maps. Healthy colonists and a
unique inventory pistol are staged on the source map. The gate's interaction
cell is walkable but not standable; it lies in the pass-through gate footprint.

Two defects were reproduced before correction:

- **RG-Issue-003:** The static `GateUtil.WorldComp` cached a previous world's
  address component. Load smoke, register the site, reload smoke and register
  again: UI/cache showed both tiles while the current component and queued-save
  XML contained only 6293. Status operation
  `op_ca3ca3c56d274a67b4d35420357e3b3c` established unequal component identity.
  The 1.7 property now resolves the current world's component on access.
- **RG-Issue-004:** Normal Enter Dwarfgate queues/despawns the traveler immediately
  when already at the interaction cell. Before ticking, save operation
  `op_d9af69983e3d4f9aabc9bf5cd2c28648` recorded only a reference to Human989:
  zero deep pawn or Gun_Autopistol42767 definitions, no ParentHolder and no world
  registration. Reload operation `op_0df5d6525e5d465f99c99de1e258ce83` found
  neither object. The gate now owns pending matter through a deep-saved
  `ThingOwner`, with the existing send/receive keys retaining ordering references.
  Entry, hauling, container and transporter admission transfer ownership only
  on success; handoff, arrival, iris destruction and closure release it correctly.
  Receive/redraft temporary lists survive the Scribe phases that populate them
  until post-load reconstruction. Failed placement retains owned contents and
  retries on rare ticks while inactive. No 1.6 source was changed, and matter
  already missing from an old save cannot be reconstructed by this correction.

Final cold-start acceptance used the current runtime/probe and reference-based
object counting (game Thing equality can equate separate objects by def/ID):

| Phase | Observed result | Bridge operation |
| --- | --- | --- |
| Prepare, same-process reload/register twice | Current component identity; both addresses registered once; destination still has no map | `op_efb2d70706e3409a8b5625b364068963` |
| Real DHD dial, queued save/reload | Lazy site remains ungenerated, opening timer resumes; saved address list contains both tiles | `op_b7f89fd111d94738856467c931fa0c33` |
| Connect, active save/reload | Lazy map generated; active reciprocal links and direction survive reload | `op_6d653452ce524d44a6677e68c4f6bb4a` |
| Pending cancellation | Existing developer Force close safely returns the queued pawn/inventory to source, with one owner and empty pending/send state | `op_5b2064610e0a497cb35bb754a9775c39` |
| Send save/reload, then 300 ticks | One owned Human989 and inventory item, destination redraft intent 989; exact IDs arrive once, drafted, buffers empty | `op_8293bfbe401e4e388243ee4d36ed5224` |
| Receive save/reload, then 300 ticks | Staggered real entry jobs expose receiver checkpoint; same identity/owner/inventory/redraft/arrival checks pass | `op_5f6566d5ec8745778cf6dd2cfe52650b` |
| Normal DHD close job, closed save/reload | Both links inactive/cleared, buffers/owners/transporters empty, no DHD close request, pawn/item retained | `op_497da82312b44999bdc75f1f2d03578d` |

Both in-flight saves contain exactly **one pawn definition and one nested item
definition under pendingContents**, retaining the same IDs. Source active and
destination-arrived screenshots show the blue event horizon and lit chevrons
without missing textures at the tested rotation/zoom. No claim of pixel identity
with Unity or complete animation acceptance follows from those captures. The
bridge error journal was empty; the preserved native log has no ERROR/exception
entries. It does contain Vulkan-layer warnings and shutdown persistent-allocation
warnings; their origin was not established as Rimgate behavior.

Preserved external evidence is under
`%LOCALAPPDATA%/RimgateDev/evidence/gate-transit-20261124-134658/`: before-fix
assemblies, actual failed saves, status/operation JSON, accepted checkpoint saves,
captures, logs and byte-backed player-config manifests. The tracked recipe and
observations above remain the durable reproduction if temporary files are removed.
The final native pack remains SHA-256
`0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`, with no
loose/bundle fallback. All **125** normal config files and four tracked 1.6 bundle
hashes were preserved; private game processes were stopped. Retained source/raw
assets/Unity inputs/reference pins and the local insurance snapshot were untouched.

**Remaining gates:** glyph/quest discovery, audible playback, other rotations and
active DHD/effect/animation coverage, loose-item hauling/transporter loading,
containers, iris-blocked arrivals and interrupted/fully blocked placement remain
unverified by this slice. Rare-tick recovery is reviewed/compiled but its fully
blocked placement case was not exercised live. Existing owned smoke saves without
the new pendingContents key load successfully; real pre-migration 1.6 save acceptance
and optional integrations remain separate work. Next take a bounded phase-2
discovery/audio/presentation pass before broadening into phase-3 systems or the
1.6 freeze/support milestone.

Verification closeout: installed-dependency mod/probe builds and native baking
passed; the separate public-reference 1.7 build and supported 1.6 library/DocFX
build passed with zero warnings/errors. Generated-site checks, **48** local
Markdown paths/anchors, Rimgate skill validation and Git diff checks passed.
Independent source/caller review found no blocking regression after the Scribe
phase correction. HEAD remains `068b95e`, the index is unchanged and all changes
are unstaged for owner review. No release/support metadata was changed.

## November 24 discovery and presentation pass - scope

Continue phase 2 from committed `6f324b3`. Exercise planet glyph decoding through
its real context-menu job, quest/site/address persistence and subsequent DHD
dialing. Fixture preparation may complete prerequisite research and provide a
healthy Intellectual-20 colonist and parchment; it must not generate the quest,
register its destination directly or invoke decoding completion. Capture the
native gate/DHD overlays at additional orientations and follow production
open/idle/iris/close audio where the installed engine exposes scoped observation.
Do not use desktop focus, microphone or system-wide audio capture. Distinguish
clip metadata, per-voice PCM output and subjective/device listening evidence.
Keep 1.6, normal player configuration and private baseline saves intact; helpers
and captures remain development-only. Results and any confirmed defects belong
here and in the existing tracker. Full optional integrations, containers and
pre-migration player-save acceptance remain phase-3 work.

### Discovery and presentation results

**Disposition:** Planetary discovery and scoped production-audio checks pass.
Additional orientation rendering remains blocked by reopened
[RG-Issue-002](issue-tracker.md#rg-issue-002); phase 2 is not complete. This pass
changes only development companions, scenarios and documentation. Production
source, XML/support metadata, raw assets and the retained 1.6 pipeline are unchanged.
Runtime/dependency context remains the 1.7.5014 rev1195 native profile recorded
above, with GABS 1.1.2 and bridge/SDK 2.5.0.0/2.0.0.0. The final companion adds
discovery, presentation, scoped audio and read-only capture diagnostics, bringing
this session to 143 tools including the 130 core tools.

The planet fixture prepares Intellectual 20, glyph research, a short clear
approach and two planetary parchments. It does not replace the decode outcome,
create a quest/site/address or bypass the real DHD/entry jobs. The skill removes
the production decode failure chance without an RNG patch. Accepted observations:

| Check | Result / operation |
| --- | --- |
| Prepare | Source tile 6293 only; no quest/site, parchment `Thing_Rimgate_GlyphParchment42766` stack 2; `op_cc704d3754b944549ae16dcc80915246` |
| Decode with working-job save/reload | Real **Decode symbols** job observed after 100 ticks, saved/reloaded, then completed. Stack 2 -> 1; one ongoing `Rimgate_GateQuestScript_Planet` quest ID 0 and one bound site ID 115 at surface tile 11898 (`P1T-351`), with no map yet. Decoded save/reload retains quest root/state, site binding, current-world ownership and one registered address; `op_98ce7edf981f4822b2f980b4e62e7cfb` |
| Dial discovered address | Real DHD job generates **Forbidden Dwarfgate Citadel**, parts `Rimgate_GateSiteMech` and `Rimgate_SoldierTauri`; generated receiving gate and reciprocal active links, quest-map count 0 -> 1; `op_14db5417ba7a410b9e97930ea734b5e8` |
| Visit and persist | Real Enter Dwarfgate action transfers the living Human989 to map 1; WasEverVisited becomes true. Saved/reloaded `Rimgate-discovery-visited` preserves pawn/site/quest binding and count 1; status `op_0c38d1f6c69a471e82b226d700542dbd`. The pawn's FleeAndCower job reflects the hostile site; combat was not suppressed. |

The durable recipe is `.assets/scripts/rimbridge-discovery-1.7.lua`. Prepare,
decode and connect passed as script phases. Visitation passed through direct
production bridge actions before the input recurrence; the subsequently added
visit phase compiled but its replay hit the synthetic-context-menu timeout
(`op_fd981cdb05214aca907debb9e5ef6f7f`). Do not report the full four-phase script
as passing. A deferred second map event initially stopped maturation; connect
now waits for only the specific busy response and resumes remaining ticks within
eight attempts. The revised connect passed; arbitrary failures are not ignored.

The accepted North-facing discovery capture shows the native blue event horizon,
lit chevrons and residual opening smoke (`rimgate-discovery-active.png`). It
extends the prior North-state evidence, not all-rotation/animation acceptance.
The presentation scenario and guarded inactive respawn/controller fixture compile;
its initial North ancient-DHD material/power/facility checks passed, then capture
timed out. The complete orientation scenario, other rotations, Tau'ri overlay
pixels and active-iris orientation captures remain unverified. Material names,
valid shaders and framebuffer dimensions alone do not establish visual fidelity.

#### Production-voice audio evidence

The development audio helper observes only explicitly named Rimgate production
SoundDefs through the installed `AudioSource.GetOutputData`/native voice tap.
The actual game jobs generate the sounds. No synthetic playback, OS/device mix,
microphone or other-application audio capture was used. Master output is muted
before effect gain is enabled; previous effect/master gains (0 / 0.8) are restored
afterward. The native backend is **miniaudio, 48000 Hz**, channel 0; 16384-sample
rolling windows may overlap and are not a continuous recording.

| Production sound | Nonzero / observed source windows | Peak | Maximum window RMS |
| --- | ---: | ---: | ---: |
| Gate open | 58 / 60 | 0.106662 | 0.042982 |
| Gate idle during reopening | 58 / 60 | 0.006619 | 0.001928 |
| Gate close | 84 / 85 | 0.110104 | 0.035784 |
| Iris close | 101 / 103 | 0.091403 | 0.035986 |
| Iris open | 106 / 107 | 0.087376 | 0.044129 |

All examined samples were finite. The real iris/close sequence passed in
`op_49b6e2ac39924a748386bbed5786e811` (731 reads, maximum gap 95 ms); normal DHD
reopening passed in `op_d23ab5e9d5cc4f75aa0c0efbbf425c8b` (325 reads, maximum gap
36 ms). Both report no missing definitions and restored gains. Other-map idle
voices remain silent as expected. The one-second inactive/paused timeout control
automatically restored gains with no explicit end call; it correctly reports a
missing idle definition and **success false** because no sound was generated.
This proves cleanup/negative detection, not positive playback acceptance.

The initial audio attempt across monolithic Lua/map-generation work observed
only two near-silent windows per voice. It was not accepted and the threshold
was not lowered. Installed IL confirms idle's muteWhenPaused behavior. Separate
bridge calls with short Normal-speed intervals produced the accepted results;
the guide owns that invocation contract. Per-voice PCM establishes engine output,
not final speaker/device routing or human listening quality. Those remain open,
along with teleport/failure/iris-hit and other systems' audio coverage.

#### Recurring hidden capture/input blocker

After the accepted discovery/audio/visit observations, full and rectangle
screenshots timed out repeatedly. A fresh process loading the owned smoke save
**before any audio probe** reproduced the failure. Actual argv remains
`-savedatafolder=<private Profile-1.7> -NoWindow -WindowSize=1280,720`; private Prefs
are byte-identical to the before-copy (SHA-256
`023238fbbb7fdf40092b37c16a4a87d9b4b9e1aadb63c7b1981ddba07dca9cd7`).
Brief Normal-speed running and explicit camera framing did not resolve it.
Volume setters do invoke broad Prefs.Apply, but this launch locks the dimensions,
and a prior capture succeeded after probing; no audio-causality claim follows.

The final read-only diagnostic control:

- Before request `op_e2e56c3b86f44bdb8cef553c7b470373`: native
  `RimWorldWaylith.Rendering.ScreenCaptureFeature`, CanCapture true, no queued
  files, frame 809, no pending request; Screen/render extent both 1280x720.
- Screenshot `op_4ee7b0b5a919495da50c9101c3026acd`: handler completes but gameplay
  success is false, ten-second file timeout.
- After `op_02cbdeded60e4650851105067f6faa3d`: frame 1420, accepted filename still
  queued, captureRequested true, no capture texture, same Screen/render extent.

This localizes the observed request before PNG encoding/writing; it does not
identify why native capture/readback did not run. Transient armed/readable null
values alone are not a diagnosis. Synthetic right-click also timed out on the
visit replay while state queries, camera operations and tick stepping worked.
No ERROR/exception log entries explain the final cold control. The original
RG-Issue-002 history remains valid, but its reopening condition is now met.
Stop this bounded investigation here; restore reliable hidden input/render/capture
before more pixel acceptance or phase-3 expansion. No speculative runtime patch
or uncoordinated foreground automation was added.

Evidence is retained outside Git under
`%LOCALAPPDATA%/RimgateDev/evidence/discovery-presentation-20261124-150200/`:
actual working/decoded/active/visited saves, accepted capture, positive and failed
operation JSON, native logs, runtime/probe binary hashes and byte-backed config
copies. Preserve these controls when investigating recurrence; the recipes and
observations above remain usable if temporary artifacts are later removed.

Closeout checks passed: the final companion compiled against the installed 1.7
assemblies with zero warnings/errors; supported 1.6 library/DocFX generation and
strict site checks passed with zero warnings/errors. The updated skill validates,
51 local Markdown paths/anchors and Git diff checks pass, and independent tooling
review found no blocking safety/production regression. All 125 normal config
files, private Prefs bytes and four 1.6 bundle hashes matched their byte-backed
baselines. The native pack hash is unchanged from the prior acceptance. Production
source/content/raw assets/reference pins and the local insurance snapshot were
not modified. All private game processes are stopped. HEAD remains `6f324b3`,
the index is unchanged and all new changes are unstaged/uncommitted for review.

### RG-Issue-002 automatic hidden redraw correction

The owner authorized a focused reliability correction from committed `a21e56e`,
preserving 1.6 and leaving edits unstaged. **Disposition: verified private-profile
workflow correction; RG-Issue-002 closed again.** This addresses a deterministic
occlusion/render-scheduling defect. The exact OS occlusion source during the
earlier natural failures was not recorded; do not attribute those occurrences
specifically to session locking, display power, shell cloaking or audio probing.

Runtime remains 1.7.5014 rev1195 with the same native pack, dependency profile,
GABS 1.1.2 and RimBridge/SDK 2.5.0.0/2.0.0.0. A fresh unmodified run initially
captured successfully in 178 ms, demonstrating the intermittent state. Extended
diagnostics showed a hidden, unfocused but **unoccluded** window with
ShouldPresent true, a nonzero surface and no session-lock/display-off flags.

Inspection of the installed binaries established the shared path:

```text
renderWanted = Window.ShouldPresent
    || (_redrawRequested && Window.Surface != null && !Window.SurfaceSize.IsZero)
```

Win32 ShouldPresent requires a surface, nonzero size and no reported occlusion.
When renderWanted is false, application updates and bridge frame/tick scheduling
continue, but CaptureFrame/render enqueue/end-of-frame capture processing do not.
Root/UIRoot OnGUI callbacks also do not run, so synthetic gestures cannot advance
their down/up/completion phases. Neither this build's native screenshot request
nor the bridge gesture queue requests a redraw. Hidden visibility alone is not
the gate. This explains why readiness/frame counters can progress while both UI
and capture requests wait indefinitely.

The correction stays in the development companion, outside the player mod:

- Register a public `EngineApplication.PrePollEvents` callback. In the bound
  private `Profile-1.7` developer session with HideWindow true and Headless false,
  request the public `IWindow.RequestRedraw()` when ShouldPresent is false and a
  nonzero surface exists. It schedules normal rendering without changing window
  visibility, focus, reported occlusion, OS session state or display power.
- Register during SDK **provider discovery**, before core-only UI tools. The
  installed loader uses Assembly.LoadFile and skips instance creation for
  all-static tool owners; a module initializer alone was not activated by the
  cold metadata-only control. RenderProbe now has an instance status tool and
  an idempotent public constructor, which the existing SDK discovery lifecycle
  explicitly creates. The startup log proves registration before any companion
  call. No warning suppression or player-mod startup patch is needed.
- Reference Waylith.Windowing with Private=false. Setup deploys only the probe
  DLL, its deps JSON and the asset manifest in BridgeTools; no engine/SDK DLL was
  added to the player mod or companion output.

The regression fixture reports occlusion only to the engine's owned window;
it does not lock, minimize, cloak, show, focus or power-cycle anything in Windows.
It can temporarily suppress the redraw callback for a negative control. Explicit
end or the bounded timer calls Win32 UpdateOcclusion to resynchronize current OS
conditions. Suppression resets in unconditional cleanup, including dispatcher
failure, so the test cannot permanently disable the redraw hook. Native refresh
failure is still reported as not restored. The caller ends the fixture in finally
outside the Lua invocation; the timer provides a maximum-60-second fallback.

Accepted checks include:

| Check | Result / evidence |
| --- | --- |
| Negative occlusion control | With redraw suppressed, renderWanted false and nonzero 1280x720 surface, the unchanged screenshot times out in 10 s and right-click in 2 s while engine frames advance and a filename remains pending. Exact expected failures asserted by `op_b1a34bef83e44fc590f218dea5cf4ea4`; success denotes the regression was reproduced. |
| Positive control after cold restart | Same reported occlusion, hidden/unfocused window and ShouldPresent false: five real left-click/selection/right-click/menu/capture cycles, iris mutation/save/reload, 120 exact ticks and final capture pass. Native requests keep renderWanted true and drain capture queues. |
| Final provider-startup regression | `op_642421b2dfcc46c9a23c7ea7a9f3d13d` passes in 7.753 s; redraw requests rise 5 -> 384 while the 60 s fixture remains active/occluded. Explicit end restores actual OS-derived state. |
| Core-only cold startup | Startup INFO identifies RenderProbe constructor / SDK TryCreateInstance before any companion method call. Unchanged `rimbridge-smoke.lua` passes from the main menu in `op_662fa4d8a5284577b8f9bfa32bd17ecf`: creation, selection, iris mutation/save/reload, 120 ticks and capture. The original owned smoke bytes were backed up and restored after this case. |
| Previously blocked discovery visit | The unchanged visit Lua phase now passes under occlusion in `op_4ffeb9fd99524a54876854d3eb67d881`: real context menu/entry, living arrival and visited-state save/reload. A subsequent capture also passes before fixture cleanup. |
| Previously blocked presentation preparation | North ancient-controller preparation, material/power/facility checks and inactive capture pass while occluded in `op_92e83deae7c3437a8075a089a36de292`. Other orientation/active presentation acceptance remains separate. |
| Timer fallback | A one-second suppressed fixture restores itself without an end call; status reports completed/restored, suppression false and normal presentation resumed. A new capture succeeds afterward. |
| Standard deployment smoke | `setup-rimbridge-dev.ps1 -RimWorldVersion 1.7` rebuilds/stages the companion and natively bakes successfully. After setup, startup registration appears before companion calls; core selection and capture pass (`op_12893b2ff7904b0eab114bca6f471b3d`, 136 ms). |

`.assets/scripts/rimbridge-background-1.7.lua` retains prepare/negative/positive
phases and assertions for the exact failure, nonzero surface, real ui_event
provider, expected DHD option, object identity/iris state, capture completion,
tick count and continued occlusion/hidden/unfocused state. The active-fixture
assertion prevents accepting a positive test that merely outlasted occlusion.
Negative captures can finish late after restoration; retain those files as late
artifacts, not successful negative operations. No queue clearing was used.

Evidence is under `%LOCALAPPDATA%/RimgateDev/evidence/rg-issue-002-20261124-160726/`:
before/final probe bytes, startup and native logs, raw operation/diagnostic JSON,
negative/positive captures, test saves, config/bundle manifests and the original
owned smoke save. Initial explicit end and timer restoration both passed. One
GABS stop reported the owned PID still alive; setup correctly refused to refresh
it. Targeted games_kill verified termination before redeployment. A later normal
stop after the deployment smoke verified exit. Do not treat a stop action's
reported success alone as process termination.

No physical OS lock/display-off or graphics-device-loss experiment was performed.
The observed supported case requires a nonzero graphics surface; this is not a
claim to recover a lost device or accept all asset orientations. No production
Rimgate source, support metadata, 1.6 inputs or Steam branch changed. Resume the
remaining native presentation acceptance next; do not skip it because capture
now works.

Closeout verification passed: companion and standard experimental setup builds,
native baking, supported 1.6 library/DocFX/site checks, Windows PowerShell 5.1
parser/help, skill validation, 53 local Markdown paths/anchors and Git diff checks.
Independent reviews identified the cleanup and startup-lifecycle gaps; both were
corrected before final acceptance. All 125 normal config files, private Prefs and
ModsConfig bytes, original owned smoke save and four 1.6 bundle hashes match
their before-copies/manifests. The native pack remains byte-identical to the
previous SHA-256. Production source/content/raw assets/reference pins and the
local insurance snapshot are untouched. The final private process exit is
verified. HEAD remains `a21e56e`; the index is unchanged and all edits are
unstaged/uncommitted for review.

## Native presentation acceptance and RG-Issue-005

Resumed from committed `0b6dea1` on November 24 with the remaining North/East/
South/West ancient-DHD and North Tau'ri presentation matrix. Runtime remains
**1.7.5014 rev1195**, Steam build 26793372, .NET 10.0.15-waylithclr.dev,
Windows, all five DLC, Harmony 3.0.4.0, VEF, Rimgate and RimBridge 2.5.0.0 /
SDK 2.0.0.0 with GABS 1.1.2. Bad Hygiene is absent at runtime. The installed VEF
hash remains `8557af77a8e4cc2915c2de710f2f6a4d167f2989704014918a8369f133dbf13a`;
it differs from the pinned CI reference, so runtime/CI dependency parity is not
claimed. The hidden private profile uses graphics-enabled `-NoWindow` and a
1280x720 framebuffer; no desktop input/focus or OS session changes were used.

### Reproduced rendering defect and correction

Material and gameplay assertions initially passed at minimum normal zoom while
screenshots lacked the horizon, iris and chevrons. The DHD overlay and opening
flecks still rendered. A retained South active/closed-iris save, paused at tick
1739 after reload, reproduced disappearance at root size 11 and reappearance at
24 without changing simulation state. The native report measured:

| Value | Minimum zoom 11 | Ordinary zoom 24 |
| --- | ---: | ---: |
| Camera Y | 15 | 28.265305 |
| Gate / Blueprint Y | 9.512196 | 9.512196 |
| Old additive overlay Y | 19.024391 | 19.024391 |
| Old overlay camera depth | -4.024391 | 9.240915 |
| Corrected overlay camera depth | 5.4878054 | 18.753113 |

Near/far clip planes are 0.5 / 65.5. `DrawAt` received an already raised draw
location, then added Blueprint altitude again, putting overlays behind the close
camera. Removing that addition restored the iris but exposed a second defect:
the base's `addTopAltitudeBias` tilts its mesh by 2 degrees, while the flat
overlays intersected it and lost the upper three chevrons. Installed drawing IL
and the two-visible-chevron intermediate capture established this distinction.

The **1.7-only** correction reuses `drawLoc + drawOffset`, copies the base's pitch
flag to horizon/iris/chevron GraphicData during spawn/load and places chevrons at
+0.03 above the iris's +0.02. Graphic_Multi retains the GraphicData reference and
DrawWorker reads the flag each draw; no material rebuild or engine patch is
needed. The retained failing save now displays the iris and all five chevrons at
close zoom. No raw art, defs, save keys, ownership or gameplay timing changed.
This closes **RG-Issue-005** for the observed ordinary overlay path.

### Final scenario and visual acceptance

`.assets/scripts/rimbridge-presentation-1.7.lua` runs prepare/open/iris separately.
Preparation reloads the owned smoke save and stages only inactive inputs. Real
DHD jobs queue and generate the destination; real toggle jobs close the iris.
The scenario polls approach in 20-tick increments, at most 400 ticks, and checks
depth/pitch, materials, power/facility linkage and gate state. It captures five
states per case: inactive, queued, opening at 20 ticks, active at 200 ticks and
active with closed iris at 400 ticks. All cases use normal minimum zoom 11.

| Controller / gate rotation | Open operation | Iris operation |
| --- | --- | --- |
| Ancient / North | `op_0001f2802050441786485305a09ad714` | `op_540bd9799eb440a59a89142306eb12fe` |
| Ancient / East | `op_9245dc23f54f451caf8ea8abb8039b0d` | `op_15c73939fe414eac90f8881384e31f28` |
| Ancient / South | `op_fb2621bd6dbe4d26b0ed62372d5e76d0` | `op_98e65d30dacf4cb0b1195ff264932b2d` |
| Ancient / West | `op_480ee1f229e7453b8aa4593cff6bb322` | `op_48699bd33dad40bca1407f61a37ea954` |
| Tau'ri / North | `op_7e4f723d9e1c406890f1adda9bdad5ea` | `op_b4cfd4402bda4d8c9602d7cfa8aab4dc` |

Independent visual inspection of all 25 final frames finds no remaining ordinary
gate-layer clipping or alignment defect: chevrons light before opening, the blue
horizon fills the authored aperture, and the segmented iris replaces it while
chevrons remain above it. East/West retain the authored narrow/slanted profile;
the flat lower edge is in the source art. Ancient DHD illumination and the Tau'ri
monitor brighten only when active and remain lit with a closed iris. Rotated
opening flecks appear in the expected direction. After a real iris reopening,
an additional Tau'ri capture at 1000 active ticks shows the unobscured blue horizon
and bright monitor; ordinary-zoom capture also succeeds without advancing ticks.

Fixture failures were retained, not accepted as rendering results. East's fixed
80-tick approach was too short; another 80 ticks completed the real job. An
unheld West dialer died before the opening screenshot. Subsequent checkpoints
showed ordinary wandering after dialing and during the queue; they do not
reconstruct the exact death. The revised case drafts the operator at the safe
controller after queueing, asserts alive/drafted/same cell through opening, then
undrafts **before** requesting the iris job. Undrafting after the request can
start the automatic toggle immediately, making the later forced-menu assertion
invalid. No damage suppression, relocation during opening or evacuation patch
was added. Tau'ri initially exhausted the fixture's 1000 W cell (800 W controller
+ 500 W iris); its private staging now adds a second cell, and power checks pass.

These are sampled ordinary states, not every animation frame, automatic
evacuation safety, special condition-driven iris glow or exact Unity/native pixel
parity. The manual iris-glow mesh bypasses Graphic.DrawWorker and needs its own
condition-triggered acceptance. Human/device audio, orbital discovery, other
phase-3 systems, optional integrations, old-save acceptance and public support /
1.6 freeze remain open. Next, finish the conditional iris/effect checks before
broadening phase 3; retain the separate audio listening and dependency/save gates.

Evidence is outside Git under
`%LOCALAPPDATA%/RimgateDev/evidence/native-presentation-20261124-165544/`: original
and corrected captures, the retained regression save, operation JSON, native
logs and byte-backed player/private config and smoke-save copies. Native pack
SHA-256 remains
`0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`;
all 814 textures and 26 clips resolve from pack-only deployment. Keep this exact
runtime/pack and same-save zoom comparison when reopening RG-Issue-005.

Closeout passes the experimental 1.7 assembly and final companion builds with
zero warnings/errors. The exact final companion was cold-launched and reran all
Tau'ri phases successfully: open `op_0af0f53a19c248b8b80dd422c25df1cf`, iris
`op_bed63f9d854e4ba08b04226bfcaf8ab5`. Both final game sessions report no bridge
error entries. Supported 1.6 library/DocFX generation and strict site checks pass
with zero warnings/errors; the skill validator, 48 changed-document local paths/
anchors and Git diff checks pass. Independent code, visual and documentation
review found no remaining blocking issue.

After verified process shutdown, all 125 normal config files, private Prefs and
ModsConfig, the original owned smoke save and four 1.6 bundle hashes match their
byte-backed baselines. The native pack is unchanged. Source/1.6, tracked Mod
content, AssetsRaw, Unity builder, reference pins, Steam branch and the local
insurance snapshot were not modified. Final binary identities and preservation
results are in the external evidence directory. HEAD remains `0b6dea1`, the
index is unchanged and all edits are unstaged/uncommitted for review. Only 1.6
remains declared supported; this acceptance does not freeze it or publish 1.7.

## Phase 2 completion

**Disposition: phase 2 complete on November 24, 2026, from committed `2468084`.**
This closes the representative engine-compatibility gate: startup/patches,
native asset loading, planetary discovery, real dialing/map generation,
pawn/inventory-item transit and saved-state continuation, closure and ordinary /
conditional iris presentation. Phase 3's wider feature, edge-case, optional-mod
and existing-save matrix is next. Phase 4 still owns public support and the 1.6
freeze; neither changed here.

The target remains 1.7.5014 rev1195 / Steam build 26793372, Windows,
10.0.15-waylithclr.dev, all five DLC and the same ten-mod private profile with
VEF present and **DBH absent**.
Native pack-only loading still resolves 814 textures and 26 clips with SHA-256
`0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`.
Runtime VEF still differs from pinned CI; no binary-parity claim is added.

### Conditional lifecycle defects and corrections

The new `.assets/scripts/rimbridge-effects-1.7.lua` loads the owned smoke save,
stages an inactive rotated gate and real power, closes its iris through the pawn
job, then executes the discovered **production** psychic-drone incident. The
incident registers GameCondition_GatePsychicDrone; its normal Init takes one
hold and opens a local receiver. The script never sets private outcome flags,
forces activation or force-closes a gate to fake successful cleanup.

**RG-Issue-006:** The original condition stored `_heldGate` only in memory while
the gate serialized `_externalHoldCount`. Before-fix condition operation
`op_d450129453df40a39017369d3062e9df` has one condition, the correct held gate,
hold 1 and active receiver. Reload `op_e583fcbd9dee497088d63711de96f804` fails:
the condition disappears, hold 1 remains and the receiver stays active. The
installed GameConditionManager load path does not call Init for saved conditions.

The 1.7 base condition now scribes its gate reference under the new `heldGate`
key. It never pushes another hold while loading. For missing-key saves only,
recovery may adopt exactly one existing local receiver hold when there is one
unambiguous condition, no remote gate/address and exactly one hold. Eligibility
is frozen at PostLoadInit, before any condition can end and change the count;
spawned-gate lookup waits until first use because maps spawn loaded things after
Scribe finalization. A shared one-shot helper runs before Tick, End and Saving,
so ending or resaving while paused cannot bypass recovery. Explicit null stays
authoritative. No existing save key was renamed.

| Ownership control | Observed result |
| --- | --- |
| Retained original missing-key checkpoint | Condition/reference survive; hold stays 1 without another push. |
| Explicit `<heldGate>null</heldGate>` | No adoption; condition ends and existing hold 1 is untouched. |
| Two missing-key conditions / one hold | Neither adopts, regardless of iteration shrinking the list; hold 1 is untouched. |
| One missing-key condition / two holds | No adoption; both existing holds remain untouched. |
| Real incident during reciprocal remote connection | It is a nonowner before saving. After reload it ends without closing either gate, adding a hold or releasing another owner's hold. |

The three policy counterfixtures derive from the retained pre-fix save: append an
explicit-null key; duplicate the condition with uniqueID 9999; or change the source
gate's `_externalHoldCount` from 1 to 2, respectively. No successful gameplay
outcome is inserted. Live paused bridge loads may already advance the first load
tick; the true pre-first-tick End/Saving hooks were independently reviewed rather
than claimed as a zero-tick UI experiment. Recovery cannot identify the owner of
an already-removed condition or ambiguous/corrupt holds; those require a retained
case and separate disposition. This is not blanket 1.6-save compatibility.

**RG-Issue-007:** With a real condition and closed iris, the original North
capture showed a red lower half with a hard horizontal edge across the dark iris.
The manual glow DrawMesh bypassed Graphic.DrawWorker, used an untilted quaternion
and shared the iris center Y. The 1.7 correction applies the base's same 2-degree
pitch and raises the glow 0.005 above the iris, below the chevrons. All four
directions now show the full authored red aperture with no half-disc seam. The
initial fault PNG was overwritten by the successful replay; the fault is retained
as an observation with its committed source revision and reproducible scenario,
not misrepresented as an available before-image.

### Accepted conditional and complete-operation checks

Each conditional case captures before, condition-active, reloaded and normally
ended states. All 16 final frames were independently inspected. The same condition
ID and gate reference survive; hold remains 1 across reload and becomes 0 after
the normal End path, with the local receiver closed and cached glow cleared.
Side-facing ellipses preserve authored perspective; chevrons remain above the glow.

| Rotation | Condition operation | Reload operation | End operation |
| --- | --- | --- | --- |
| North | `op_13201ad21b5a45da9675278f5ba5ed0d` | `op_bc187770bc984dfab8c739f3e20df7b3` | `op_42c6eaf222bc432fb6667e38f6149dc3` |
| East | `op_d6c46b2a73a44d6a9b9b4f13a07e7e9a` | `op_cdd57e17b1474eddbe2ebe227119e948` | `op_f30a5382d480452e9526e299ea8b4d20` |
| South | `op_396cca1bc2454939b6bc1de9e1e228ac` | `op_98b1b9f1605042588fd0786c7a6b2137` | `op_7a0e546f0ce043f09952a7ebc1a7c0f9` |
| West | `op_62436ac5c6c84649907e368ed0fc77c9` | `op_643a5cfd0af5471f948f64258d7201a8` | `op_21ccbc81b5804c9699d808b4686b80d7` |

The unchanged transit recipe also passes all six phases on the corrected build:
prepare, queued dialing/reload, lazy generation/active reload, send and receive
in-flight checkpoints with resumed living arrival, and normal DHD closure/reload.
Send `op_24f7c18cacb346ef804d4d9a5248a348` and receive
`op_eac1cf31e19a4411b58c66a73be1333b` preserve exactly one tracked pawn and its
unique inventory pistol, redrafted at the destination. Close
`op_1fd0031adcaf4a568144571c7f40e3d4` leaves both gates inactive, links/queues
cleared and the living pawn/cargo intact. Inventory cargo does not substitute
for phase 3's loose hauling/transporter/container coverage.

The new read-only `rimgate/patch_report` observes **46 actual patched targets**
owned by `com.Rimgate.patches`, including the real
`Verse.PawnInventoryOption+<GenerateThings>d__6.MoveNext` transpiler. The earlier
exact game-IL call-pattern audit and clean startup log remain applicable. This
proves patch registration, not correctness of every patched feature. Startup
clarifies the version labels: **Harmony Mod v3.0.4.0**, library assembly/file
**3.0.0.0**, product **3.0.0.0-rc.7+b18fbc7562836f38acb4fd4563456cde2c130414**.
The DLL SHA-256 is unchanged at
`7252fbe77bc96fcfb5e6aa446a4957f4547de312d889046fe3ec7d15f7f0fcf5`;
the same labels occur in the prior presentation log. No deployment-change or
new Harmony dependency claim follows from the more precise report.

### Phase boundary and remaining acceptance

Phase 2 closes a representative production gate path, with engine-side audio
evidence from the prior five real-job SoundDefs. It does not claim human/device
listening, every animation frame, every random sound variant or Unity/native
pixel parity. Static horizon/iris graphics and sampled native opening flecks plus
the condition branch are accepted for this milestone. The remaining manual
motion/listening quality pass stays explicit in phase 3's wider acceptance.

Start phase 3 with gate edge cases and a preserved copy of an existing 1.6 save,
then cover the contributor matrix: loose cargo/containers, blocked arrivals and
placement, interruption/cleanup, cloning and sarcophagus, mobile containers,
ZPM/housing, orbital discovery, other quests/incidents, UI and optional mods
absent/present. Keep focused evidence in this plan or the relevant issue; do not
restart the completed engine bring-up or silently publish support.

Evidence is private under
`%LOCALAPPDATA%/RimgateDev/evidence/phase2-closeout-20261124-232136/`: pre-fix
condition save and failed operations, policy fixture provenance, 16 final frames,
six-phase transit results, actual patch report, final native log and byte-backed
player/private config plus original owned test saves. The final live session has
no bridge error entries and verified process termination. RG-Issue-006 and
RG-Issue-007 are closed for their reproduced paths; source/content for 1.6 remains
the retained baseline, with no public 1.7 metadata bump.

Final checks pass: experimental 1.7 and companion builds with zero warnings/
errors, supported 1.6 library/strict DocFX/site checks, skill validation, 62 local
Markdown paths/anchors, independent source/visual/documentation review and Git
diff checks. All 125 normal config files, private Prefs/ModsConfig, original owned
smoke save, four 1.6 bundle hashes and the native pack match their byte-backed
baselines. Source/1.6, tracked Mod content, raw assets, Unity builder, dependency
pins, Steam branch and local insurance snapshot are unchanged. Owned test output
saves have recoverable before-copies outside Git. The final private process exit
is verified. HEAD remains `2468084`; the index is unchanged and all new changes
are unstaged/uncommitted for review.

## Phase 3: gameplay and current-save acceptance

**In progress, November 25, 2026; starting revision `1f9e83f`.** Keep phase 4
separate: only 1.6 is publicly supported. Its C# and unrelated content are
preserved; the owner approved the shared layout XML corrections described below.
The target remains 1.7.5014 rev1195, Steam build 26793372, Windows, all five DLC,
the recorded Harmony/VEF runtime and native pack-only deployment. The ten-mod
baseline was tested without DBH/VTE; the twelve-mod optional profile adds
Dubs Bad Hygiene 3.2.3260 and Vanilla Traits Expanded, before Rimgate.
Installed metadata is not acceptance of every optional integration.

### Historical save disposition

A private copy of the retained 1.6.4871 smoke save loaded, advanced 120 ticks,
saved as a new 1.7 file and reloaded with the same gate/iris state. This is one
specific import, not general save compatibility. A copy of the January player
save (1.6.4633) failed with missing historical definitions and related apparel
errors; its original bytes were preserved. Camera+ and Visual Exceptions were
temporarily enabled only in the private profile to satisfy its recorded list,
then removed from the active profile. No upgraded player save was produced.

The owner explicitly waived old-save migration: “i don't care about old saves.”
The subsequent clarification identifies `Rimgate_TacticalVestSteelPlateInsert`
as the former name of `Rimgate_SteelPlateInsert`. Do not add historical aliases,
reconstruct removed items or make archival saves a release gate. Current 1.7
operation/checkpoint/reload/continuation remains required.

### Reproduced defects and fixture controls

| Issue | Before-fix evidence | Correction boundary |
| --- | --- | --- |
| RG-Issue-008: loose cargo targeting silently does nothing | Actual Bring selected targeting left a unique autopistol on the ground and the pawn waiting. | The 1.7 callback applies Claimable only to mobile containers, rejects ordinary buildings/plants/fires, and permits normal haulable items/corpses/incapacitated pawns. No admission shortcut. |
| RG-Issue-009: treatment queues disappear on reload | Valid 24 kW fixture: HealingStarted, remaining 258, patient-owned Hediff_207 queued. After reload the patient remained owned/powered but the queue was empty. Retained `medical-valid-power-before.rws`. | Save ordered hediff references and trait definition/degree keys through Scribe phases; rebind actual patient-owned trait instances after load. No re-diagnosis, timer/resource reset or power grace. |
| RG-Issue-010: moving cart checkpoint loses its original target | A real loaded push saved its carried proxy/cargo but referenced the despawned original cart in job target A and reservations. Reload failed to resume the pusher. Retained `moving-cart-before.rws` and native log. | Release the original reservation before despawn, retarget the job to its deep-owned carried proxy, and avoid spawned-map access while rebuilding loaded toils. |
| RG-Issue-011: live layout cells still name removed or malformed symbols | Read-only native KCSG report: 44 layouts, 34 invalid cells, 12 token forms. Includes old plate names, whitespace/dot errors, recharger/wall-lamp spelling and Smokeleaf. | Correct the authored Goa'uld, Mech, Misc and Tau'ri layout XML directly: 27 rows / 34 cells with unchanged row widths. The owner approved these shared 1.6/private-1.7 content fixes in place of the initial experimental patch. `SmokeleafJoint` is inferred stored-drug intent beside Beer, not a recovered original asset. |
| RG-Issue-012: DBH with thirst disabled throws every healing tick | Real patient needs had Bladder/Hygiene but no Thirst. Native log identifies SetThirstNeedCurLevelPercentage from Sarcophagus.Tick. | Null-check the optional thirst need, matching the existing hygiene/bladder setters. 1.7 only. |

The new scenarios prepare inputs and observe production results. They do not
insert successful transfers, healing, clones or discovered quests. Two invalid
fixtures were corrected before drawing production conclusions: duplicate power
transmitters split the medical net; a conduit over the housing minified it.
Preparation now skips transmitting footprints and asserts spawned objects plus
normal network initialization. These are test defects, not fixes to game power.

Dormant ancient mechs in the owned smoke map caused later host jobs to flee.
Native Thing.HostileTo explicitly excludes dormant pawns; preparation now uses
map thing listings and faction hostility before unfogging, excludes all player,
colonist, prisoner and slave pawns, and records removed IDs. The four removed
guards are Militor4472, Tesseron4474, Scorcher4476 and Tesseron4478.
This isolates medical/utility/orbital inputs; it does not establish combat safety.

The installed Lua compiler, rather than an offline general Lua parse, is the
authoring gate. Roundtrip assertions use measured game-tick deltas instead of
assuming visual readiness loads are zero-tick. Identity, queue order and fuel
checks remain exact. Script/companion details belong in the contributor guide.

### Accepted first-batch checks

| Case | Observed result and operation |
| --- | --- |
| Loose cargo | Autopistol53700 retains one object/owner through carry reload (`op_a4db111040864278a0474484f68ee54c`), destination arrival (`op_b7c41c5874d144ae8ed1f13e6b1c9b73`) and draft interruption back on the source map (`op_623bd3034e624f5aa50d2bd7511ff9f8`). This is one item, not a partial-stack/corpse/pawn test. |
| Sarcophagus, DBH/VTE absent | Fresh healing checkpoint retains Hediff_207; elapsed ticks explain timer progress without resetting it. Normal treatment ejects the same patient/inventory and removes the original injury (`op_0c0a712a9ed14b5382859afdf073cab0`). |
| Sarcophagus, DBH/VTE present | DBH has Bladder/Hygiene and no Thirst. Two ordered hediff targets plus the actual degree-0 VTE_Lush trait reference survive healing reload (`op_0376ba2eb781425c80a5e2a125c18894`). Real completion removes the targets/trait and discharges the same patient (`op_f5a34d05212b41caa2b21c6336c8a2c4`). |
| Genome cloning | Admitted host, unfinished calibration and pending-clone checkpoints survive reload. Final calibration replay `op_bcd8fdc9e425445c8f827b1d6e098e09` observes six elapsed ticks and bounded actor-stat work, not a zero-tick load. Pending clone Human51227 stays singly owned across reload (`op_166a3a236ef3463a933df38345a0f78c`) and real flick-off/on (`op_e6453789775c4806854613b9308138b6`, `op_1e7d3444c4d34238bca072046ad58c40`). Normal incubation completes with the same living, spawned generation-1 pawn, empty owners, Idle pod and fuel 200→100 once (`op_0fec809e8a7d4536a529ce629b03f4c3`). No developer finish command or duration/failure-setting override. Temporary Ultrafast boost was restored to false. |
| ZPM housing | Three real insertions produce stored energy 72000 and dark reserve 1500. Reload preserves both (`op_8c56b98326ac40a397d49026dc134b67`); ejection restores the same three modules, each 24000/500, and empty housing 0/0 (`op_69ef88e6acae44ec8ac77758121a38f3`). Spawned housing and connected/powered lamp prerequisites are asserted. Broadcasting/raid suppression/recharging remain separate. |
| Mobile cart | A normal loading manifest selects only one autopistol. Proxy46952/cargo42774/job/visual survive the moving checkpoint (`op_db61b684a30140fa8b319a46d9e110c5`). Resume places a steel, HP120 cart at the selected (150,120), preserves its cargo/use setting and clears slowdown/visual (`op_a863e8db2ae74f8ab675fc8196c37d5b`). Normal unload and draft interruption from the same checkpoint pass (`op_84f45eb5e34340eb96f9e7d1a54e7e4f`, `op_39d9048bee1f470abe4c0555a49cec8a`). Reconstructed cart ID changes by design; cargo identity is exact. |
| Orbital discovery/equipment | Real decoding consumes one of two parchments, survives an unfinished-job reload, creates ongoing Orbit quest 0 / site115 / tile214 / O1E-361 and preserves its lazy site/address (`op_296e8517fb2c4aacb99a0fb6f0e379f9`). The actual DHD option is disabled without equipment, enabled with normal powered linked equipment and remains eligible after reload (`op_e0b0d1e634914188bc4f52c7aafe275b`). No orbital map generation/entry/vacuum claim. |
| Layout symbols | Native report resolves all 44 loaded Rimgate layouts, 66460 nonempty cells, zero unresolved cells/layouts/tokens and zero ignored out-of-bounds cells. It does not generate or visually inspect every structure. |
| Final minimal-profile cold run | Final companion plus four production corrections pass a new 1.7 debug colony, gate/iris save/reload, 120 exact ticks and capture (`op_1f559dc8c53b44418ca2763ae11a020a`). Native asset scenario resolves 814 textures/26 clips and runs the normal powered iris jobs (`op_804456d007aa4061bea2c5d2e79b44ea`); both captures were inspected. Terrain/fog near the new fixture limits full-base visual coverage; the earlier controlled presentation matrix remains the presentation evidence. Final bridge error journal is empty. |

Bulk utility/cargo/orbital waits now run ordinary Ultrafast simulation and poll
their read-only companion's actual `result.tick`; they allow overshoot and
always pause on successful completion. The caller pauses in `finally` on
failure. This avoids coupling completion waits to hidden render-frame cadence
without claiming exact ticks or changing simulation jobs. One-tick in-flight
observations and the smoke's exact 120 ticks remain separate. Retained failures
include 119/250 frame-stepped ticks in 30 seconds and an orbital invocation's
60-second script budget expiry; neither was accepted as success. The bridge
status snapshot has no tick field; polling it was corrected to the owned
companion clock after a failed trial.

### Remaining phase-3 gates

Phase 3 is **still active**. The first and gate-edge batches close
RG-Issue-008–014 for the reproduced paths, not the entire feature matrix.
Blocked landing/retry/closure, receiver iris protection, loaded-cart gate
handoff/cancellation, native transporter loading and representative partial-stack/
corpse/downed hauling now have focused acceptance below. Current follow-ups are:

- [Local and fully blocked cart restoration](#non-wiping-cart-recovery-rg-issue-023),
  [unloaded-site cargo/loose corpse continuation and downed-map lifetime](#final-logistics-acceptance),
  and [solo/mixed native manifests plus walking-pawn DHD cancellation](#final-baseline-acceptance)
  now pass. A standalone native Cancel command was not exposed, and a fleeting
  in-frame comp-admission snapshot was not captured; do not infer a defect or
  claim fully blocked deep-comp cancellation from those observations. Arbitrary
  nested corpses in value-snapshotted containers remain outside the loose-corpse
  identity proof.
- Independent review recorded the pre-existing nested-corpse representation risk
  as [RG-Issue-028](issue-tracker.md#rg-issue-028). Its exact native manifestation
  still needs the bounded cart/load/unload/reopen reproduction in that entry;
  the loose-corpse correction does not close it or establish a new regression.
- [Full/enhanced/corpse cloning, resource/abort/needs, sarcophagus interruption
  and core medical incidents](#remaining-medical-acceptance) now pass their native
  cases. [Optional present-dependency health checks](#final-optional-integration-acceptance)
  now have representative actual acceptance and explicit unexercised branches.
  [Fuelled/refrigerated carts](#final-logistics-acceptance),
  [ZPM broadcasting, suppression and recharge](#remaining-zpm-acceptance) pass;
  the [actual scheduled consumer](#final-zpm-scheduled-consumer) also runs.
  [RG-Issue-027's current-threat correction](#current-threat-zpm-raid-scaling-rg-issue-027)
  passes its fresh-quest reload and naturally scheduled wave.
- [Orbital entry, native vacuum protection, visited reload, both abandonment
  branches and remaining world incident lifecycles](#remaining-world-and-orbital-acceptance)
  now have focused acceptance. The [short living orbital round trip and reload](#final-baseline-acceptance)
  passes on a later natural site. Previous bounded combat losses remain recorded
  separately. Meteorite guardian generation/hostile combat and a separately
  controlled real extraction/haul/reload pass; unmodified guarded-resource
  collection and orbital combat/loot victory are not claimed by those cases.
- The [bounded optional pass](#final-optional-integration-acceptance) now covers
  absent/present dependencies and representative actual health/facility paths.
  Isolated Android/Rimsec guards, the genuine Warcasket foundry consumer and
  Alpha's clinical suppression remain qualified or unproven. Quarry's installed
  package declares only through 1.6 and is excluded from the 1.7 profile; separate
  DBH variants are unaccepted. These are explicit coverage limits, not blanket
  compatibility claims.
- The coordinated manual motion/device/listening quality pass.

At the initial phase-3 batch closeout, verified shutdown and preservation checks
passed for all **125 normal config
files, 10 original player saves**, both private config files, the original owned
smoke input and four retained 1.6 bundles. The native pack hash remains
`0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`.
The fresh 1.7 colony has a separate evidence copy; the original smoke input was
restored. Normal player files were never restored over concurrent edits because
none changed. At the initial batch closeout, Source/1.6, Mod/1.6, raw assets,
Unity builder, dependency pins and Steam branch were unchanged; the local 1.6
insurance snapshot was untouched. The owner subsequently approved the narrow
layout-source exception below. The initial batch left the index unchanged and
all new edits unstaged/uncommitted.

### Evidence location

Evidence is private under
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-20261125-002512/`: byte-backed normal
config, original-save hashes, private config/smoke backups, retained failures,
native logs and operation JSON. Recovery inputs stay outside Git. Keep successful
replays distinct from failed or invalid fixtures. No public support/default
change follows from this record.

Final validation passes the experimental 1.7 assembly and final companion with
zero warnings/errors, supported 1.6 library plus strict DocFX/site checks,
PowerShell parsing, skill validation, Markdown path/anchor checks and Git diff
checks. Independent review found no blocking issue in the other production
paths, new scenario oracles or initial layout patch; sarcophagus serialization was
self-reviewed by its implementing agent. Record the manual and wider gameplay
limits above when assessing this first batch.

### Direct layout source corrections

On November 25 the owner approved correcting the shared authored XML instead of
retaining the experimental layout patch. This is a narrow exception to preserving
1.6 content: four files under `Mod/1.6/Defs/MapSites/Structures/` (Goa'uld, Mech,
Misc and Tau'ri), 27 rows and 34 cells. The direct edits produce exactly the same
parsed XML as the former patch, with unchanged row widths and unrelated bytes.
The patch and its extra setup copy step were removed; private 1.7 setup now
copies the corrected XML through the existing path. No def aliases, support
metadata, 1.6 C#, asset inputs or bundles changed.

Full setup and a cold native load without the patch pass: all four deployed
files match the authored source, and the report still resolves all 44 layouts /
66460 nonempty cells with zero unresolved or ignored cells
(`op_c376189bac694557852fbc6eacad5295`). In a new private debug colony, KCSG's
actual Quickspawn structure generator successfully creates
`Rimgate_StructureGoauldNaquadahStorehouse`, `Rimgate_StructureMechHiveBravo`,
`Rimgate_StructureJadeHengeBravo` and `Rimgate_StructureSGC`, one affected layout
from each edited file. The bridge error journal is empty. This verifies selected
layout generation, not every site, quest, rendered structure or live 1.6 behavior.

Evidence and the generated colony are retained under
`%LOCALAPPDATA%/RimgateDev/evidence/layout-source-20261125-111405/`, including the
old patch, original source bytes, exact transfer report and native operations.
The owner's staged snapshot is preserved; these follow-up edits remain unstaged.
Validation also passes all 153 definition XML files, the setup PowerShell parser,
skill validation, supported 1.6 library/strict DocFX/site checks and Git diff
checks. The game is stopped; all 125 normal config files, 10 original player saves,
both private config files, four 1.6 bundles and the native pack match their
baselines. Live 1.6 was not rerun; its source/layout checks do not replace that
runtime coverage.

### Gate-edge and loaded-cart acceptance

**November 25, 2026; starting revision `5ec4ff3`.** This second phase-3 batch
uses the same licensed 1.7.5014 rev1195 / Steam build 26793372, Windows and five
DLC, with native pack-only loading. Load order is Harmony 3.0.4.0, Core/Royalty/
Ideology/Biotech/Anomaly/Odyssey, VEF, Rimgate and RimBridgeServer 2.5.0; SDK 2.0.0 and
GABS 1.1.2 provide development tooling only. DBH/VTE are absent. The installed
VEF context remains the earlier recorded runtime, distinct from the CI reference
pin; these results do not validate every optional integration. Testing uses the
owned mature `Rimgate-transit-active` checkpoint and real jobs/menus in the hidden
graphics-enabled private profile, with no desktop focus or computer-use actions.

Two verified defects were discovered and corrected only in 1.7:

- **RG-Issue-013:** Issue a normal loaded-cart gate order, then let the gate close
  during the approach. Initialization replaced an invalid gate destination with
  the pawn's current cell. Replaying the carried proxy checkpoint then converted
  and spawned its cart at the original gate cell even though that gate was
  inactive. `cart-carried-before.rws` and `cart-rejection-after.rws` retain the
  original failure: cargo survives, but Gate38637 itself disappears from the save.
  Reject invalid destinations before conversion and unavailable gates before
  releasing the proxy. Secure gate ownership of the reconstructed empty cart
  before transferring cargo; failed admission retains the carried owner for
  normal interruption cleanup. General fully blocked restoration is still a
  separate acceptance case, not a claim of this correction.
- **RG-Issue-014:** Save a real assigned DHD close job and designation while the
  pusher is still approaching its cart. On reload, the designation/job survive,
  but the unsaved `_wantGateClosed` flag is false; the close job completes without
  closing and the cart proceeds through the still-open gate. The retained
  `dhd-close-before-pickup-before-fix.rws` contains both jobs and the designation.
  Remove that duplicate flag and derive close intent from the persisted native
  designation already used by the work giver/job. Toggle cancellation and normal
  completion remove the same designation; no new save key or migration hook.

| Case | Verified result and representative operation |
| --- | --- |
| Genuine blocked landing | A rejected 525-wall attempt had holes and delivered cargo; it was not accepted as blockage. The new Steel1 fixture creates 691 ordinary loose inputs while preserving original objects. The actual Near search checks 517 candidates, 438 native-spawnable cells all vetoed by the production no-merge policy, zero positive-quality cells and `nativeCanPlace: false`. Normal hauling admits Steel53699 to the receiver. The reusable `blocked` phase observes a further 81 real ticks, beyond the maximum normal receive delay, and retains that same object, one deep owner and one FIFO alias (`op_09fd0869eaf14ae0b7d191c09ebc77d2`). This is Steel-specific, not a universal physical obstruction test. |
| Reload, normal closure and inactive retry | Active blocked reload passes. The real Pawn989 enters behind Steel in the FIFO; normal DHD closure releases the pawn while Steel stays blocked, naturally retaining the map. Closed-state reload preserves the exact Steel/queue. Releasing only tracked input Steel53702 exposes one positive-quality cell; normal inactive hash retry places original Steel53699 on map 1 once, clears ownership/queue references and leaves both gates inactive (`op_58d9a9e2197b4159a9365ea9910aff64`). The full reusable sequence passes before and after the DHD correction. |
| No-pawn destination unload | A separate ordinary close with no receiving pawn unloads the map. The existing transit-site value cache retains gate→Steel/count 1 (`op_54b0760f4e684cdb9edd8aff4a0f3027`). These records deliberately lack original Thing IDs/queue identities. This is not lost matter, nor proof of exact-ID continuation across site regeneration; no world-site behavior was changed. |
| Cart retained failure | Replaying the original inactive-gate carried checkpoint now restores its loaded cart near the operator, preserves Gate38637, Steel stuff/HP107/use=false and exact cargo Gun53700, and clears proxy/visual/slowdown (`op_07088124ce9846c5aeeab3abe089d43b`). |
| Fresh cart handoff and cancellation | The real manifest selects only Gun53700; ordinary hauling loads it. Carried proxy53706, cargo and pushing job survive save/reload. Normal handoff delivers reconstructed cart plus the same living operator once; a later replay after requesting/cancelling DHD closure also arrives (`op_c8f4dc05241548cdb8205bb25f3d3a49`). Real separate DHD-close and iris-close jobs each leave the loaded cart/operator on the source side with Gate38637 intact and no proxy/visual/slowdown (`op_1918d94cbac34693a2ba3c13051cfff6`, `op_b84f2ae4ad2c4470a0ba2767f010457a`). Cart IDs change on conversion by design; cargo identity and stuff/HP/use are exact. Construction is disabled only as a fixture input so autonomous repairs cannot invalidate HP107. |
| Pending DHD close reload | The retained pre-pickup checkpoint now closes normally after reload; the original, unconverted cart58305/cargo/properties stay intact (`op_34f6959b69484dceaef3782e09e3d2c2`). Reloading its completed save keeps closure consumed. A reusable `close-checkpoint` phase also saves/reloads the real assigned closer job while the cart is carried, completes closure (`op_8a5d339374ae427690795da9fe501966`) and passes normal rejection cleanup (`op_ef4ce3d788ab4a6d95b737c46345c29f`). Real toggle on/off reports intent true/false and leaves the link available for the successful handoff above. Load readiness may advance normal ticks, so a request consumed during loading is accepted only with actual gate closure. |
| Receiver iris protection | The real traveler enters, ordinary generator/iris installation inputs provide a powered receiver, then actual close/open iris jobs execute. Closed iris destroys the exact watched Gun53713 object (`Destroyed: true`, count 0, no owner/loaded registration; `op_9a9057a3cd804c8291ed9a85a2715114`). Open iris delivers watched Gun53720 intact to map 1 with one object/owner (`op_565fceddcc6d4a13b77d779c8768b9b4`). The explicit non-owning watch is Game-bound and cleared after the case; it does not claim reference identity across reload. The full six-phase reusable scenario passes in the final cold session. |
| Source closed-iris menu | A real close-iris job leaves only the disabled “Cannot enter Dwarfgate: iris closed.” option, with no enabled Enter/Bring action. This does not establish every already-issued ordinary pawn/cargo race. Destination actors can flee from native dangers after arrival; transport survival is not combat safety. |

The contributor guide owns the three new bounded Lua protocols and guarded
companion inputs/observations. They never insert successful admission, placement,
closure or iris-destruction outcomes. One rejected congestion fixture exposed
native item relocation at full cells; preflight now skips cells already rejected
by native placement and verifies every original object's position/state/HP/count.
The rejected fixture and timeout trials remain separate from successful runs.
Outcome polling replaces an arbitrary 1500-tick cart wait that expired despite a
successful arrival. Every caller pauses on failure; iris cleanup also releases
its single observation. These are tooling corrections, not game-speed changes.

Evidence is retained outside Git at
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-gate-edges-20261125-112923/`: byte-backed
inputs, before/after failures, accepted checkpoints, native logs, runtime context,
binary hashes and complete operation JSON. `accepted-and-trial-operations.json`
includes failed trials explicitly; `final-native-operations.json` holds final
replays. The final bridge error journal is empty and process termination is
verified. Private Prefs/ModsConfig and all four original owned input saves were
restored after retaining the accepted outputs. All 125 normal config files,
10 original player saves and four retained 1.6 bundles match their before hashes
and file inventories. The native pack hash remains
`0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`.

Experimental assembly and companion builds pass with zero warnings/errors; all
three new Lua files compile through the installed SDK. Supported 1.6 library,
strict DocFX/site checks and skill validation pass. Independent review covers
both production corrections and the new scenario/probe contracts; its retry
measurement qualification was resolved by the explicit 81-tick observation.
Source/1.6, tracked Mod content, raw assets, Unity builder, dependency pins,
Steam branch and insurance snapshot remain unchanged. Public support still
declares only 1.6. The index is unchanged; all changes remain unstaged/uncommitted.
The remaining gates above, including manual quality checks, keep phase 3 active.

### Native transporter and wider-cargo acceptance

**November 25, 2026; starting revision `8df57cc`.** This third phase-3 batch uses
the same licensed 1.7.5014 rev1195 / Steam build 26793372, Windows, five DLC and
ten-mod native-pack profile as the gate-edge batch. Harmony 3.0.4.0, the recorded
installed VEF context, RimBridgeServer 2.5.0 / SDK 2.0.0 and GABS 1.1.2 are
unchanged; DBH/VTE remain absent. Loaded configuration has no issues. The private
graphics-enabled `-NoWindow` profile needs no desktop focus or computer use.

Two additional reproduced defects have narrow, version-scoped corrections:

- **RG-Issue-015 — native loading bypasses its transporter owner.** On the owned
  mature outgoing link, use the actual Load manifest to select unique Gun53700,
  then order **Prioritize loading Dwarfgate**. After 300 normal ticks, the item
  remains in source Gate38637's deep `pendingContents`, with no send/receive
  alias, empty native `innerContainer`, outstanding native manifest count1 and
  `notifiedCantLoadMore: true`. The receiver gets nothing. The retained
  `Rimgate-transporter-stranded-before.rws` preserves that state.
  Exact native IL shows `TryGetInnerInteractableThingOwner` chooses a building's
  own `IThingHolder` before its comps; native hauling therefore bypasses
  `CompTransporter.Notify_ThingAdded` and manifest subtraction. A gate-only
  postfix routes native interactable-owner lookup to its existing transporter
  container. Other holders, pending ownership, saved keys, traversal, direct
  Bring/Enter and receiving-buffer handling remain unchanged. Do not rescue
  arbitrary orphaned pending matter as a substitute for the owner correction.
- **RG-Issue-016 — stackable chunks lose quantity in map compression.** A legal
  ReplicatorLink99 input splits through actual hauling into carried75 and
  source24. The original carry-boundary assertions pass, but reload retains
  only the explicit carried75 and reconstructs a new source-cell link/count1:
  total76, losing23 units. Native compression stores a def hash per cell and
  does not restore stack count; the authored stackable chunk inherits
  `saveCompressible: true`. The 1.7-only XML patch sets it false. Setup overlays
  `Mod/1.7/Patches/` after copying the retained XML into the private package;
  the 1.6 definition and all shared content remain unchanged in this batch.
  This is a defect verified in 1.7, not evidence that it is unique to Waylith.
  The initial patch ownership is superseded by the owner-approved
  [direct-source correction](#direct-replicator-link-source-correction) below.

**Old compressed records are not repaired.** Their original counts/IDs were
never saved, and the native loader skips those records once the def becomes
noncompressible. The owner waived historical save migration. Retain the broken
checkpoint as failure evidence and use fresh inputs from an unaffected checkpoint
for acceptance; do not label a replay of already-lost quantity as repaired.

| Case | Verified result and representative operation |
| --- | --- |
| Real native manifest | The helper stages only two different loose guns, healthy approaches and two explicit Home cells. Native `AllSendableItems` includes both; ordinary Items-tab quantities1/1 and Accept create the native manifest/group with remaining2 (`op_9dbab111827c460098cb44af31a52353`). No DEV loading or manual manifest acknowledgment. The gate streams loaded contents automatically; no separate launch step exists in this path. |
| Native haul and both reload boundaries | Real `HaulToTransporter` carries Gun53700 with native remaining2; carried checkpoint preserves the exact job/targets/group and deep owner (`op_308c88ba4c664fea818a3cc5ef475956`). First delivery leaves remaining1/GateIsLoading, then a partial-manifest save/reload preserves the first arrival and outstanding second gun (`op_b43b119ecfbb44a381135217f2e08bf9`). Actual owner lookup reports the transporter, not pending transit storage. |
| Complete manifest | Both exact guns arrive once with one object/deep owner each. Native remaining becomes0, group becomes-1, loading ends and transporter/pending/queue owners drain (`op_0f2f61fd91174ff4bfe2d3f8b02bdcfa`). This is a two-item nonstackable manifest, not transporter pawn boarding or every loading combination. |
| Close during second carry | From the actual partial checkpoint, the existing native job picks up rifle53701 (`op_61f54a10c24b4983af4a961f8335e681`). A separate colonist's real DHD close job cancels loading, releases the rifle onto the source map once, ends hauling/carrying and clears group/manifest/container/pending/queue state (`op_296a7c922686490b80326ec13b74899c`). No developer force-close. The empty receiver unloads normally, so the earlier delivered gun thereafter uses the existing site's value-record contract rather than an original-ID claim. |
| Manual cancel disposition | The attempted Cancel branch found no such gizmo at this carried/partial boundary. Native gizmo enumeration requires `innerContainer.Any`; the gate has streamed the first gun out while the second is carried, leaving that container empty. This is expected UI behavior at this boundary, not a missing-control defect. The unused branch was removed; no player UI was added. Other nonempty-container states can offer native Cancel/Unload and remain separate. |
| Fresh full-stack save | Loaded definition reports `saveCompressible: false`. A fresh loose99 saves/reloads with its same original ID, count99 and one owner (`op_49cd0df5327740a1a70a98f806a6241a`). No stat, capacity, quest-tag or compression-reference workaround. |
| Native partial stack | Native capacity75 splits original Link53699 into source24 plus carried Link53701/count75. Carried reload retains both IDs/counts and the real job target (`op_3f7cf9d4fff849c3bdbe6ca7d4f6a82d`). Arrival places the same75 on map1 while24 remains on source (`op_4194afe6d92545958483415b6f3925d4`); draft interruption restores the carried stack source-side (`op_fb9cb5725e794762a75b6790829922d7`). Every branch totals exactly99 across only the recorded IDs, with empty gate queues. Native remerge into the original remainder is allowed by the oracle but was not observed in this run. |
| Corpse cargo | A newly generated fixture pawn is killed only as an explicit input; a unique inventory marker is seeded after native death drops. Actual Bring carries corpse53702/inner Pawn53699/marker53703 through reload, arrival and draft interruption with exact nested identities and one owner each (`op_d9aebccc2d0047c8830ddb96896e84cf`, `op_09870ca7c8454ebf893d7e87bf0d2f87`, `op_89dbd6ec46604fbfa2ea0dd841151f06`). No existing colonist is killed or repaired to complete the case. |
| Downed cargo | A fresh fixture pawn is natively downed by default Anesthetic input. Actual Bring retains the same living downed pawn, Hediff_252 and unique inventory marker across carried reload, destination arrival and draft interruption (`op_9feaa6a9a0fb4e5ba8315fc838ef51a9`, `op_992456bd5f3448289f0f45b9de20870f`, `op_23fe58c67a254040b9daec283785bfc3`). These are hauling/ownership checks, not treatment, combat or recovery acceptance. |

The contributor guide owns the two new bounded Lua protocols and companion
schemas. Inputs/observers never carry, admit, transfer, finish or cancel outcomes
directly. Rejected tooling trials are retained separately: passing an entire Lua
parameter object produced literal unresolved RPC arguments; omitted empty fixture
fields needed mode-conditional bindings; distant transporter inputs initially
failed native Home/storage eligibility. These observations do not establish game
defects. Strict original-ID and 99 checks caught actual compression loss; diagnostic
source-cell replacements never contribute to passing quantities.

Full experimental setup passes after the XML overlay change: assembly and final
companion build with zero warnings/errors, authored patch bytes are deployed, and
native baking reports 840 entries/48 deduplicated/zero failures. The native pack
retains hash `0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`;
live inventory still resolves 814 textures/26 sounds. This rerun establishes asset
resolution, not a new visual or audible-quality acceptance pass. Native SDK
compilation passes both new scripts, including the final second-carry/close path.
The loaded Harmony report has 47 targets including the exact owner-routing postfix;
registration is separate from the real loading evidence above.

Evidence is private under
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-cargo-20261125-123653/`: original
byte-backed inputs, stranded save, compression failure/current-cell report,
accepted checkpoints, exact native holder/compressor inspection, final binaries'
hashes and complete operation JSON. Failed/prototype runs remain distinct from
successful acceptance. The final bridge error journal is empty. Native fixture
warnings about verb references to removed ancient guards are retained in the
log; no pristine-warning or broader dependency-compatibility claim follows.

The game is stopped with verified termination. Private Prefs/ModsConfig and three
original owned input saves are restored. All 125 normal config files, 10 original
player saves and four retained 1.6 bundles match their before hashes and inventory;
the setup helper's player-config verification also passes. Source/1.6, Mod/1.6,
raw assets, Unity builder, dependency pins, Steam branch and local insurance
snapshot are unchanged. Public support still declares only 1.6; the authored
1.7 patch is selected only by private setup. Supported 1.6 library/strict DocFX/
site checks and skill validation pass. All new edits remain unstaged/uncommitted
with the original index intact. The remaining phase-3 gates above stay active.
Independent review covers the owner-routing correction, XML/setup boundary and
both new probe/scenario contracts. The final close phase also requires an actual
second-item carry at entry, then passes native compilation and a fresh guarded
replay. PowerShell parsing, all 177 Mod XML files, 94 changed-document links/
anchors and Git diff checks pass. No wider/manual acceptance is inferred.

### Direct replicator-link source correction

On November 25, 2026, starting at `e3c4ec0`, the owner approved correcting the
original XML instead of retaining a 1.7 patch, matching the earlier layout
decision. `Mod/1.6/Defs/ThingDefs_Items/Resource_Chunk.xml` now explicitly sets
`saveCompressible=false` beside `stackLimit=99`. This is an authored persistence
correction shared by retained 1.6 content and its private 1.7 copy. The former
`Patches_ReplicatorLinkPersistence.xml` and the setup-only patch overlay step are
removed. No version-specific patch pipeline is retained without a consumer.
Unrelated XML, C#, def/save identities, public support metadata and assets are
unchanged. This is a narrowly approved shared-content exception to the freeze.

Full experimental setup passes without the patch: 1.7 assembly and companion
build with zero warnings/errors, native baking succeeds, and deployed source XML
matches the authored file byte-for-byte with no stale persistence patch. In the
same isolated 1.7.5014 rev1195 ten-mod profile, the loaded flag is false. A fresh
loose99 survives save/reload with the same ID/count/one owner
(`op_5aa09d86c0c54207a302c5752d02fec0`). Real hauling splits it into carried75
and source24; both original IDs and all 99 survive the carried checkpoint
(`op_2badb6f99e0f424f95b4c2888b6ab316`), actual destination arrival
(`op_481bec54eab34a07a8a883cf760956c9`) and normal draft interruption
(`op_f42b4bedef994207a2f94bc23bd2a725`). Gate owners/queues drain, and the final
bridge error journal is empty. These results replace patch-based deployment
acceptance, not the original failure evidence. Old compressed-record loss remains
unrecoverable and outside the owner-waived historical-save scope. Live 1.6 was
not rerun; supported 1.6 compilation and static XML checks do not establish that
runtime coverage.

Evidence and accepted checkpoints are retained outside Git under
`%LOCALAPPDATA%/RimgateDev/evidence/replicator-source-20261125-144340/`, including
the removed patch, original XML, exact operation JSON and native log. XML
comparison finds only the intended field addition; all 176 Mod XML files parse.
Setup PowerShell parsing, skill validation and supported 1.6 library/strict
DocFX/site checks pass. After verified shutdown, all 125 normal config files,
10 original player saves, four 1.6 bundles and the native pack match their
baselines. Private configs and original owned test inputs are restored. The
original index is unchanged and follow-up edits remain unstaged/uncommitted.

### Remaining world and orbital acceptance

On November 25, 2026, from `6c6b268`, these checks ran in the same isolated
RimWorld 1.7.5014 rev1195 ten-mod native-pack profile. This slice cold-deployed
only the development companion; it did not deploy production C#/XML, change the
player profile or declare public 1.7 support. The current private package also
contained the separately investigated ZPM housing rare-tick input. An initial
attempt to reuse an older twelve-mod orbital checkpoint without its recorded
DBH/VTE dependencies produced missing-need/research and native map-update errors.
That input was discarded, the process cold-reset, and every accepted result below
was regenerated from the healthy ten-mod `Rimgate-bridge-smoke` save. The failed
load is fixture incompatibility, with no historical-save compatibility claim.

`WorldEdgeProbe.cs` stages loose vanilla vacsuit/helmet inputs and ordinary source
waits; its remaining tools observe native state or select the real world site.
`rimbridge-world-edges-1.7.lua` uses actual Wear, DHD dialing and Enter Dwarfgate
jobs, current UI targets, ordinary simulation and native save/load. Two genuine
orbital parchments created independent lazy quest/site/address bindings. The
second produced **Silent Signal Dock**, site 116/quest 1, Orbit tile 450,
`Rimgate_OrbitalSatellite` plus `Rimgate_EnemyPresenceOrbit`. Real suit/helmet
ownership gave native VacuumResistance 1 and `HarmedByVacuum=false`; actual entry
recorded the expedition/visited state and an unpressurized room with Vacuum 1.
The requested 600-tick sample and visited reload passed while the traveler was
alive and conscious, with no VacuumExposure/VacuumBurn hediff (observed ticks
4913 through 5541, including load readiness;
`op_317956d0f85a4b90ad191c2c5766c6f9`). Real home-DHD closure and orbital DHD
return dialing passed (`op_52069c7e7462421ca084ccc3dc599d37`,
`op_87cad71ba2a74e358433ef96ebfec8c3`). **Actual return did not pass**: generated
hostiles downed the scout before the Enter job completed
(`op_1d1163b6346d43488a52b263ff72d612`). Earlier attempts in the first hostile
ark also lost the scout to ordinary combat, including one with a normally worn
shield belt. No generated hostile, health result, vacuum flag, quest completion
or travel outcome was removed or inserted to obtain acceptance. Combat safety,
a completed orbital round trip, indefinite exposure and loot pickup remain
pending; the observed generated crates/items establish generation only.

Both native **Abandon exploration** paths passed through the actual world UI and
reload. Lazy unvisited abandonment removed site/address, failed its quest and
advanced LastIgnoredTick (`op_db4d8ae9cb054575aeae3df81c21d980`). The visited
case followed the genuinely lost scout's ordinary BloodLoss death; once the map
had no blocking colonist, abandonment removed the site/address/map, restored the
active-site counter to zero, failed quest 1 and left LastIgnoredTick at -1
(`op_273f000438264a20a77b1dc5629879a2`). Its saved XML contains only home map 0.
The unrelated lazy quest 0 remains ongoing. This verifies cleanup after a lost
expedition, and does not substitute for a living return/abandon expedition.

`rimbridge-world-incidents-1.7.lua` exercised actual native incident workers via
their normal developer incident actions, default durations and ordinary ticks:

- Gate heat wave and toxic fallout each produced the positive authored ramp,
  one exact local receiver hold/reference through reload, then zero holds and
  closed reception after native End and condition-manager pruning. Representative
  reloads: `op_0cbb222b60f04bd299fb905ab39a199b` and
  `op_945c8c1e2bbe487c9571274d4421e061`; End checks also passed.
- The Naquadah meteorite landed 13 new mineable cell coordinates, increasing the
  native count from 124 to 137, with the same coordinates/count after reload
  (`op_14261a1efe6d49fd85395248579e7419`,
  `op_f59155a9464a43f5a13a6c85263cf896`). Native compressed mineable IDs regenerate
  on reload, so these checks compare actual counts/coordinates and retain both
  ID lists. They do not claim mined-item pickup or stable compressed-ore IDs.
- Marauders generated a real hostile pawn with
  `Rimgate.LordJob_MaraudColony`; the same pawn ID/custom lord continued through
  reload (`op_16af28597b864d2f83b6b69c19b3cbed`,
  `op_ceacc1eb558241289988135f42795ac1`). This establishes worker/lord persistence,
  without a combat-victory or exhaustive timed-quest-raid claim.

The ten-mod optional-absence report also passed all 16 resolved XML/actual hook
contracts with the expected optional package set empty
(`op_796d802bbe2e43a78384ecb0137af73e`); this is registration/content evidence,
not present-dependency gameplay acceptance. The companion build passed with zero
warnings/errors, both new world Lua files passed installed-SDK compilation, and
the game was stopped with native process termination verified before returning
the exclusive runtime lease. Root-level player/config restoration and final
cross-slice validation remain owned by the enclosing acceptance run.

Evidence is outside Git under
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-completion-20261125-145845/world/`:
`summary.json`, operation JSON, the protected visited save, blocked-return save,
visited-abandoned save and the earlier combat-downing input. Reopen these checks
if native protection/visit state fails to survive reload, abandonment retains a
map/site/address or corrupts counters/history, an incident loses its hold/lord or
generated mineables, or a living, normally protected orbital return demonstrates
a production failure independent of enemy combat. The combat/round-trip/loot
limits above remain explicit pending acceptance, not closed migration defects
or a phase-3 completion claim.

### Remaining medical acceptance

On **2026-11-25**, the isolated ten-mod 1.7.5014 profile completed the remaining
core cloning and sarcophagus cases through real UI/jobs, ordinary Ultrafast
progress and native save/load. Default settings remained unchanged. All three
recorded calibration outcomes had **every failure flag false**; their native
clean-outcome incubation multiplier was 0.75. This does not sample every random
failure outcome or complete optional present-dependency clinical acceptance.

| Mode | Calibration work | Biomass charged once | Actual default incubation | Clone | Completion / completed reload |
| --- | ---: | ---: | ---: | --- | --- |
| Full | 6,750 | 150 | 135,000 ticks | `Human47104` | `op_3ed889b304ae4dd6ab25397a7bb30d6f` / `op_f6e036305c75410d8cea2e83a079c709` |
| Enhanced | 9,000 | 200 | 180,000 ticks | `Human43021` | `op_821a5763a98b40edae273a57ce860704` / `op_a45c4b64e5a1408e808108da1e8554f4` |
| Reconstruct | 11,250 | 250 | 225,000 ticks | `Human43019` | `op_30acaf22ae1a49c6a5f30b241d1fe374` / `op_3c46ef2fcb3b49d085dc0af749e9f2d9` |

Each case retained its exact pending clone and deep owner through reload, then
completed with the same identity, empty pod owners and no second charge.
Reconstruct used a fresh dead donor, original corpse wrapper and nested marker;
the real corpse-hauling job also performs calibration. Pre-reload snapshots
verified independent host/clone traits. A read-only audit of those recorded
snapshots also passed all 12 skill/passion results per mode and the applicable
backstories: Full retains 80% of source levels, Reconstruct 50%, and Enhanced
has its authored combat skills and baseline traits.

- **RG-Issue-018:** actual Full calibration shared a host Trait instance and
  changed its owner to the pending clone before any reload. The retained
  `full-admitted-before.rws` reproduces this; the failing assertion is
  `op_390460d467de40438d946735845f7e72`. Fresh native Trait copies fixed Full
  (`op_d701e402690d498d99cbbbfb5653c698`) and Enhanced
  (`op_698944a3c56d4371993603ec54757ebe`) before reload. The final gene-reference
  mapping preserves endogene/xenogene collection identity; Reconstruct ran that
  final implementation, but a duplicate-def gene-reference edge was not sampled.
- **RG-Issue-019:** the completed Full clone was normally downed by
  `Rimgate_ClonePodSickness` and `Rimgate_SystemShock`, despite its clean outcome.
  Native AI exposed the doctor work-giver destination mismatch. The retained
  completed input now reports no job when its sarcophagus is assigned elsewhere
  (`op_cd542dc95a554ab48638a930c95e2bb8`). Actual Set owner UI assignment makes
  the rescue predicate/destination available (`op_b4b7f849bff24e44a4e1e84e6144e28d`);
  ordinary AI then places the same clone inside the diagnosing sarcophagus with
  one deep owner (`op_55fa40a155bc4b4ca495834da0f9c2c4`). This is not a separate
  bed-transfer or Warden branch acceptance claim.
- **RG-Issue-020:** the actual loaded degree selector threw for 21 of 32 bounded
  seeded calls (`op_1d45248119f84998ac308431be89a6cb`); native `RandomElement`
  selectors eliminate the inclusive-Count indexing error. The same 32 calls
  passed (`op_4ea89e4599bf46e69bfa45b65ff0b654`), restoring Rand state after
  each call. This pure-method check is separate from natural cloning outcomes.

Additional successful paths include paused host ejection with exact saved-food
restoration; resource-paused clone reload, actual biomass hauling/resumption,
abort without refund and empty-state reload; and both diagnosis/healing aborts
with exact restored needs and retained unfinished targets. The native hauler
delivered 60 biomass, then 40 through a second real order before the 100-cost
Genome cycle resumed; one haul was never assumed to fill the cycle requirement.
`Rimgate_KillAllClonesNow` killed the actual completed clone and preserved its
corpse ownership through reload. `Rimgate_SymbiotePlague` infected a fresh native
symbiote host, preserved the disease reference through reload and diagnosis,
and was removed by normal treatment with a living patient after reload.

The smoke map rejects the SolarFlare **incident** target, and an occupied
sarcophagus exposes no usable power designation. Neither restriction was
bypassed. Instead, the native **Solar flare / 1 day GameCondition** supplied the
environmental input: actual electricity-disabled/PowerOff state caused native
ejection and ownership reset (`op_e8db15d1367443fbb0ca756e56d83589`), survived
reload, and recovered after native condition End. Normal re-entry completed
the retained injury (`op_2181ad3352c241728bf4cbd92ad36186`). This proves condition
interruption/cleanup, not incident eligibility or natural-duration expiry.

Fixture limits are retained explicitly: bulk post-discharge sampling can observe
ordinary need decay; exact needs use paused eject/abort boundaries. Ordinary AI
later injured a released donor and dropped its exact marker onto the map; the
post-discharge oracle requires the original ID/count and one deep owner or map
registration, while held checkpoints still require inventory ownership. No
mid-cycle health/needs rescue occurred. Native combat temporarily slowed progress;
bounded clock waits timed out without implying failed cloning. Reconstruct UI
capitalization/rich-text labels and its automatic calibration were corrected in
the scenario after rejected fixture attempts.

Detailed operations, flags, timing, failed fixture boundaries and actual input
save bytes are retained outside Git under
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-completion-20261125-145845/medical/`;
`summary.json` and `clone-shape-recorded-audit.json` consolidate the results.
Production and companion builds passed with zero warnings/errors, the installed
SDK compiled `rimbridge-medical-edges-1.7.lua`
(`op_8373abbfc9fb4b0584d6016ec96fecd6`), and shutdown verified no RimWorld process.
Optional native race/xenotype/health-policy, EBF arm-injury and Alpha Genes input
tools are prepared; their present-dependency clinical behavior remains unrun here.

### Coordinated manual quality pass

On November 25 the owner requested finishing feasible background checks first,
then discussing the checks requiring human observation. This does **not** waive
motion, visible sampling or device/listening quality. Background framebuffer and
per-voice PCM evidence remain separate from these gates.

| User observation | Existing evidence / focused remaining check |
| --- | --- |
| Continuous gate/controller/iris motion | Sampled frames, native rendering and actual jobs already pass their recorded cases. Watch an entire opening/idle/closure and iris cycle at normal and close zoom; check ordering, clipping, flicker and motion continuity. Include the conditional powered glow and representative controller orientations. |
| Representative native texture appearance | Native assets resolve and sampled presentation passes. Look at representative gate, controller, apparel and building surfaces in motion/at close zoom for visible sampling defects; do not require identical sampler metadata to Unity. |
| Actual audio/device playback | Production voice probes establish finite nonzero engine PCM, with output muted. In a coordinated private visible session, select the intended speakers/headphones and listen to gate open/idle/close and iris cues, their timing, balance and transitions. Record the actual device and private game volumes; restore preference bytes afterward. |

Coordinate this session explicitly before taking foreground focus or changing
output volume. Use the private package/profile and compatible owned input, with
the installed game/reference pin still matching; keep normal player saves and
configuration untouched. Record each actual pass/failure in this owning plan,
including a useful frame/clip or exact symptom for a failure. Do not declare
phase 3 complete or move support/defaults to phase 4 while these checks or another
required gameplay gate remain unresolved.

### Remaining logistics acceptance

The continuing November 25 pass uses the same isolated ten-mod 1.7.5014 profile
and baseline `6c6b268`. Private `phase3-completion-20261125-145845/`
retains complete operations in `root-logistics-wave1.json`, fixture IDs, the
original failing checkpoints and subsequent candidate saves. This subsection
distinguishes the original failures from the final acceptance below.

| Defect | Reproduced failure and focused correction |
| --- | --- |
| RG-Issue-022: parked refrigerator consumes no fuel | A real native refuelling job fills refrigerated tracked cart42790 to25. Ticks890–1940 leave fuel25 despite configured5/day (`op_cded589d13da40a7bf56a2deca4fc625`). Its Normal ticker calls `base.TickLong`, skipping native `CompRefuelable.CompTick`. The 1.7 correction calls `base.Tick`; the retained input then consumes24.999916→24.912384 over1043 actual ticks (`op_7c4c30e8c2af455587e48198b93c0d1a`), matching native repeated single-precision subtraction. Inner-container ticking is still explicit. |
| RG-Issue-023: interruption wipes an obstruction | With actual proxy53706 carried at137,119, thirteen native-invalid local cells include Sandbags58305 at the pusher. Real draft interruption removes that sandbag at1677 (`op_fc301d56ead146ac976067043d66b77e`). Native Near can itself choose destructive low-quality placements, and native job cleanup can drop a remaining carried proxy. The correction must secure non-wiping placement or durable saved ownership before releasing the original cargo. Final local/full-search recovery results belong below. |
| RG-Issue-024: unloaded site loses loose items and corpses | Real native split/haul delivers75 of99 Replicator Links. Closure unloads the receiving map without scavenging, but records only gate/DHD (`op_49c393b425b14c50a50a4ec68bca8b33`). Separate arrival of Corpse53703/Pawn53700/inventory53704 loses their site ownership too (`op_1fe3170baa6b4d0c8e09ca207cff8974`). Accept factionless ordinary cargo into existing value records; retain loose corpses in a separate deep owner, preserving their graph and existing normal-item scavenging. Capture follows the incoming-transporter veto. |
| RG-Issue-025: ordinary wheelbarrow freezes food | Actual loading stores MealSimple42792 with rot disabled; ticks906–1694 leave progress110.73198 (`op_ad31d6b1062145b3a00a27d7617a1add`). Spawn setup inverted the refrigeration extension. The corrected owner derives sealing from `sealPreventsContentTick && FuelOK`, reconciles ticking/rot on spawn and rare checks, and releases rot on removal. The same retained input now has rot enabled and advances110.73198→1077.2817 over1048 ticks (`op_5531b41d187841ea96521bfb3c39dc96`). |

**Rejected intermediate site replay:** closed-map corpse ownership and reload
preserved the exact corpse/pawn/inventory, and the ordinary value record retained
75 links, but both were absent after redial/reopening
(`op_0580d4bf08f0462f9489f16acd7efb58`,
`op_de836efb6d294dcd937a35a49be89682`). Original cargo cell55,27 and the new
corpse restoration anchor overlap the actual North gate's opening-vortex cells.
Final restoration must exclude the actual rotated vortex footprint and retain
failed placements; successful closed-state serialization alone is insufficient.
The first broad factionless-item candidate also retained natural stone chunks;
exclude those without excluding Replicator Links, which deliberately share the
broader Chunks category. These intermediate operations are not acceptance passes.

**Native People manifests:** real People/Items controls produce a mixed
pawn/one-rifle manifest, and native loading delivers both originals with empty
queues/group cleanup (`op_e477a7ed72e34ae08a871a65e4630bd6`). A solo native
`EnterTransporter` walking job and manifest survive reload
(`op_e2d3353fdafa42c895411c1bc5f3e520`), then deliver the same healthy pawn and
inventory marker with group-1/remaining0 (`op_e86fa5c919d0451284f0e08ea727ddb4`).
A first600-iteration diagnostic hit the SDK's10000-statement budget; bounded
scans now use200. One-tick snapshots still missed the fleeting comp-admission
boundary, so no deep-comp cancellation claim follows from successful arrival.
The observed native commands while loading expose no Cancel action. A subsequent
DHD-close branch encountered the smoke input's ancient guards/fire and ordinary
inventory dropping; it is retained as a failed fixture, not a cart/gate defect.
Fresh transporter preparation now removes those hostile non-colony prerequisites
as the other smoke fixtures do; its clean replay remains required.

Retained 1.6 C#/DLLs and asset bytes are unchanged. None of these checks accepts
every cargo type inside arbitrary nested value-snapshotted containers or repairs
state already absent from historical saves.

### Remaining ZPM acceptance

The private native 1.7 run verified the housing/tracker/shroud paths of
RG-Issue-017 and reproduced the separate raid-policy persistence defect,
RG-Issue-021. Evidence is retained under
`C:/Users/david/AppData/Local/RimgateDev/evidence/phase3-completion-20261125-145845/zpm/`
(`summary.json`, complete bridge results and save XML); the readable external
`zpm-acceptance-journal.md` also retains the original failure operations.

- **Housing and reload:** the original loaded housing def was `Never`, so its
  three modules powered the lamp without running `TickRare`. After the initial
  `Rare` ticker experiment, the original tracker rebuild still omitted
  housing: reload at ticks 2570→2596 changed tracker count 1 / quest 1 into count 0 /
  quest -1 and ended that quest while the same housing stayed broadcasting with
  three modules; 520 further ticks did not recover it. The 1.7 owner correction
  recognizes both standalone modules and loaded housing in source discovery,
  tracker reconstruction and protection targets. Fresh normal ticks 2008→2570
  then produced broadcasting/count 1 / quest 1; reload 2570→2600 preserved the same
  ongoing quest, tracker and exact three module IDs
  (`op_d6649789c0734233bbd1d63b21950e69`,
  `op_33ce1f6b72414e1f82194f5bf729813e`).
- **RG-Issue-021:** before its fix, a fresh protection quest retained its ID and
  ongoing state but reload discarded both `TargetDefs`, the broadcasting
  predicate and `RG_LetterRaidZpmProtectDesc`, reverting the letter key to the
  generic marauder default (`op_ce5f30453cc544d6908c2a5a0a56674e`). The focused
  1.7 fix saves the def list and letter key, replacing the unscribable delegate
  with the narrow persisted `RequireBroadcastingZpm` policy. The fresh quest
  above retained both ZPM/housing target defs, true policy and its protection
  letter key across reload. Existing callers retain their default policy; this
  does not reconstruct fields already lost from an older private quest save.
- **Suppression and recharge:** two ordinary adjacent emitters became linked,
  powered and suppressing through native initialization. A real flick job turned
  one off; the other kept map suppression active, including across reload
  (`op_c0d18e0001e2419998cdaf491302ae6b`). Turning the last emitter off through
  another real flick job resumed a new protection quest. A powered linked phase
  diverter increased housing dark reserve 500→562 over two ordinary rare ticks.
  After a real flick job disabled it, ticks 3980→4750 retained reserve 624 exactly
  with all three facilities off (`op_379a59049a9745909779d0f1488f8978`). No
  tracker, power, link, quest outcome or reserve setter produced these outcomes.

**Final ticker ownership:** preserve the exact retained 1.6 power definition.
Source plus inspection of its actual built DLL proves that enabling the housing
ticker activates broadcast→tracker→quest generation while its source discovery
still recognizes standalone ZPMs only; the housing-only quest returns before
populating its interval grammar. No live 1.6 replay is claimed. The correction
therefore belongs in `Mod/1.7/Patches/Patches_ZPMHousingTicker.xml`, consumed by
experimental setup's version-owned Defs/Patches staging. This is a necessary
assembly-version difference, unlike the shared layout/replicator XML corrections.
No retained 1.6 DLL change or implicit maintenance expansion is required.
When phase 4 establishes the full authored 1.7 definition tree, put `Rare` on
that housing definition and remove this override; do not add a second ticker or
change the frozen 1.6 definition.

**Scheduled consumer:** the retained continuation is private save
`Rimgate-zpm-scheduler-pending`, paused at tick 14686; its bytes are retained as
`zpm/scheduler-pending.rws`. Quest 2's initial delay is enableTick 3534 plus 19917,
ending at 23451. The later live run corrected the initial first-event estimate:
the native interval emits immediately when enabled, so the first actual raid
fires at 23452; 90000–120000 is the interval for later waves. It retains the same
quest/policy, selects housing42766, produces the protection-specific letter and
creates a real marauding lord. The final consumer record below owns the exact
operations and context; no combat-victory or balance-quality acceptance follows.
A 20000-tick wait hit its 20-second deadline at roughly 14626
(`op_57d21dd8ada341c9a93347dcb7081498`); the completed outer operation was
correctly rejected because the script/gameplay payload failed, then finally
paused. Eight native survival-meal stacks of 10 were staged on inspected empty
cells before the long wait. Random Storyteller activity was explicitly disabled
for that lab branch, while quest timers and the raid worker remained unchanged;
the debug setting was restored before shutdown. Cached-tracker behavior after
real cross-map minify/reinstall was not exercised in this lease.

Both selected 1.7 builds passed with zero warnings/errors. The utility, logistics,
cargo-site, transporter-pawn and optional-integration Lua scenarios compiled
through the installed SDK at their recorded revisions. The private game stopped
with process 2148 absent and no `RimWorld` process; player-config verification
matched its recoverable baseline. No retained 1.6 DLL or bundle was changed.

### Non-wiping cart recovery (RG-Issue-023)

On November 25, 2026, the real blocked-cart fixture reproduced destructive
interruption cleanup: a carried loaded wheelbarrow, thirteen radius-2 obstacles
including Sandbags on the operator's cell, then actual drafting plus one tick.
The old direct spawn destroyed Sandbags58305
(`op_fc301d56ead146ac976067043d66b77e`), while the native placement predicates had
already rejected the obstructed cells. This is a verified production defect,
not a timeout or compilation inference.

Experimental 1.7 now restores only the exact job-target proxy. It uses native
Near placement with an explicit whole-footprint non-wiping validator, securing
placement before releasing cargo. Native Near searches radius 12.9; its default
lowest-quality fallback can wipe another building, so merely calling Near is
insufficient. If every candidate is rejected, a narrow
`WorldComp_CartRecovery` deep-owns the actual restored cart, retaining its
map-parent reference and anchor, and retries every 250 ordinary ticks when that
map is loaded. Native world traversal includes this holder. Generic carry and
inventory cleanup cannot serve as the fallback because their native drops can
again select a destructive placement. No gate/address storage or 1.6 C# changes
are involved. The new English keyed notice is an otherwise unused shared
language addition; its authored/private bytes match and the actual native
message resolves correctly (`op_7e15c3432a0a40939619c0929a7658f0`).

Both branches pass on the cold-deployed combined 1.7 build in the same unchanged
ten-mod native-pack profile:

- Replaying `Rimgate-cart-blocked-before` naturally fails the saved blocked
  approach during load readiness and restores a Steel wheelbarrow at (135,118).
  All thirteen original obstacle IDs, positions and HP survive, including the
  previously destroyed Sandbags58305; the exact pistol53700 remains owned once.
  Cart HP107/use-contents=false, cleared proxy/visual/slowdown and the living
  operator are verified (`op_a1400da62ebc46d0b515f2ca241902c4`,
  `op_a77e637858ff4294a16ed14003028149`).
- A fresh real carried checkpoint is enclosed to radius 13 without deleting or
  moving existing objects. Already invalid original cells are preserved; 318
  ordinary obstruction inputs block the remaining cells, and the actual
  production validator reports zero legal native-Near candidates
  (`op_c3bb667e17f44d9abdd9bcd15b3737d1`). Actual drafting and one tick produce
  restored cart58623 deep-owned by the world recovery holder, with its original
  Steel/HP107/use=false properties and exact pistol53700 once
  (`op_e9ec4068f5e04b4d9f9e570f6005b291`). Proxy, attachment and slowdown clear.
- Saving/reloading retains that same cart ID, its cargo ID, map-parent 114 and
  anchor (138,119) (`op_29d224c63e1a4b34a9c4d8441b393fba`). All 318 obstruction
  IDs/positions/HP also remain exact after reload
  (`op_bd1ef64d2ac34c53aef01f5333524f96`). Saved XML contains exactly one deep
  cart and one deep pistol, with the destination stored as a reference.
- Removing only the recorded obstruction IDs and running an ordinary requested
  500 ticks allows the normal retry to restore the same cart58623 on map 0 at
  its anchor, preserving the same cargo once. The recovery owner/list drain and
  final XML still contains one cart and pistol
  (`op_3029efd5c76a4f34a7d0f5bf8d6b32d8`,
  `op_2006bf7f0ca14a1f9e80326ad68a7246`). Bulk simulation can overshoot; this
  does not claim an exact 500-tick placement latency.

Production/companion builds pass with zero warnings/errors. Installed-SDK
compilation also passes the updated logistics, cargo-site, medical and transporter
pawn scenarios. The game is stopped with process termination verified before
returning the exclusive lease. Original input, fully blocked input, pending
owner and recovered-final saves plus operation JSON and `summary.json` are
retained outside Git under
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-completion-20261125-145845/cart-recovery/`.

This proves the mapped Steel-wheelbarrow case, current-save ownership and natural
retry. It does not establish fuelled-cart fallback, another pusher's live
interruption, recovery after map unloading, or permanent destination destruction.
Clearing space near the recorded anchor is the normal recovery contract. If the
referenced map parent is permanently destroyed, the cart remains deep-owned but
there is no automatic alternate-map relocation or new recovery UI. Reopen RG-Issue-023
if interruption wipes an original object, cargo/metadata changes across the
pending checkpoint, ownership duplicates, or clearing space fails to drain the
retained owner through ordinary ticks.

### Final logistics acceptance

The November 25, 2026 native 1.7.5014 replay used the same isolated ten-mod
profile, graphics-enabled hidden window and Waylith pack. The full experimental
setup path passed after introducing the version-owned housing ticker patch;
the resolved housing ticker is `Rare`. The authored retained 1.6 power XML and
DLL are unchanged. Native `Assets.zip` remains
`0DEA55CEDEC8EB2F14BC1FED2AC78B9F6FE1E1B4E2E5EC03F02F3568E8BA6797`,
with all four pack files deployed together. Production and companion builds
passed with zero warnings/errors. The cargo-site, cargo-kind, logistics-edge,
utility and utility-edge scenarios compiled through the installed SDK.

Complete operation payloads, input/checkpoint saves and native XML observations
are retained under
`%LOCALAPPDATA%/RimgateDev/evidence/phase3-completion-20261125-145845/logistics-final/`.
The earlier rejected vortex-restoration operations remain failures; the final
cold-deployed results below replace them as acceptance evidence.

- **RG-Issue-024, corpse ownership:** normal DHD closure deep-retained original
  corpse 53703, inner pawn 53700 and inventory marker 53704 in the world site.
  Closed-state reload preserved all three identities
  (`op_a95d0bb6828248babd653b8760a62195`,
  `op_0beaf75363c0411d8f5ad5a0a4181724`). Real redial and matured reopening
  restored that same corpse at (55,30), outside every actual rotated gate-vortex
  cell (`op_5923785283a5447da21245f6aa377f61`,
  `op_7afb1a61bcef42a8bf294c472f400e6b`). A further reopened save/reload still
  contains the original body/pawn/inventory exactly once
  (`op_fba1abc754444181b915ca49529d1f35`,
  `op_80858c49b6d0408d9066dcf5c8192806`). The corpse is not a value record or
  a recreated pawn. It stays intact through the site's existing scavenging
  transition; no new corpse-loot mechanic is introduced.
- **RG-Issue-024, factionless quantities:** the real 99-link split retained
  source 53700 count 24 plus the destination value record count 75 through closure
  and reload (`op_183881b6b2aa43f7b87d508087cef734`,
  `op_ff8d66addb0243eeb9a3e5006c6746e4`). Redial/reopening recreated that
  record as 68789 count 75 at (55,30), outside the actual vortex; source count 24
  still exists, giving exactly 99
  (`op_98f35f3f6c694e1ca790841d83f322f3`,
  `op_79851ee896d9492286c77574b72919c4`).
  Coarse value storage deliberately changes the recreated item's ID. Neither
  case captured natural StoneChunks. Failed legal placements retain their
  record/deep owner for a later regeneration; a completely blocked regeneration
  was not exercised here. Restored normal records retain the existing scavenging
  policy. Historical cargo already absent from saves is not recovered.
- **Living downed subject:** the real transported Human 53700, anesthetic
  Hediff 252 and inventory marker 53703 survived normal closure and reload
  (`op_90185c344fa04364b37478a5759f1042`,
  `op_cdebf5517c654abfbd9dfa82a00572c4`,
  `op_36c501258386417c8d4a9eb8be90c1d4`). Native
  `AnyPawnBlockingMapRemoval` kept the destination map loaded. This proves a
  living pawn's native map-lifetime contract separately from unloaded corpse or
  item storage. One earlier script attempt lacked the fixture's anesthetic ID
  and was rejected; the corrected replay above owns the pass.
- **RG-Issue-022, native fuel tick:** the root replay already measured
  RefrigeratedTracked fuel 24.999916→24.912384 over ticks 891→1934 after
  `base.Tick()` replaced the incorrect `base.TickLong()`. The additional
  RefrigeratedHover run consumed24.956074→24.882908 over 1096 ordinary ticks,
  matching repeated native float32 subtraction of 4/60000, while its exact meal's
  rot progress223.82686 remained frozen. Reload retained that state
  (`op_d91075b93b3044b8bfdf6c6ea8f38eaf`,
  `op_3083366888b64b9bbc2804d6fb601b85`).
- **Actual moving fuel:** native jobs refuelled Tracked with 15 Chemfuel and Hover
  with 10 Naquadah, then loaded the original pistol 42774 through the actual Load
  dialog and pushed normally. Twenty individual ticks matched each proxy's
  reported per-tick float32 fuel subtraction; carried reload preserved the same
  proxy/cargo/fuel state
  (`op_a6a2a7a5d9c14ce99aa68506fe156256`,
  `op_c46108a4cf08405589123b0f8c234d9e`). Real arrival restored Tracked 46945
  with 14.66803 fuel and Hover 46944 with 9.733983, original cargo exactly once,
  input Steel/HP 180 or 160 and use-contents=false, with no remaining proxy,
  attachment or slowdown
  (`op_fe8b64f1dfd145b593b78a466f92722e`,
  `op_3539a02b9dd845ac83224e327353310b`). Ordinary repair jobs had legitimately
  repaired the input carts before pushing; HP claims use that actual pre-push
  baseline.
- **RG-Issue-025, seal lifecycle:** the root ordinary-cart replay changed meal
  rot 110.73198→1077.2817 over ticks 934→1982 with disabled=false/dontTick=false.
  Here the refrigerated meal stayed frozen while loaded, then real unloading
  cleared its rot-disabled flag and 1000 ordinary ticks advanced the same meal
  223.826859→791.332275
  (`op_d761b82b19304c3c9be6eb9c832f09f5`;
  `refrigerated-hover-unloaded-rot.json`). Together these distinguish native
  comp ticking, refrigeration and unsealed-food ticking.
- **RG-Issue-026, actual auto-refuel preference:** natural fuel exhaustion first
  exposed a previously false setting becoming true on cart reconstruction,
  starting an unwanted automatic refuel job. Retained input/restored saves and
  `026-before-field-proof.json` establish the actual field change. The narrow
  proxy field now copies, scribes and restores `allowAutoRefuel`. Fresh false
  survives conversion, carried reload and natural exhaustion: restored 46937
  has fuel 0/false, the same pistol once and no automatic refuel job
  (`op_95e87821e0b049db960606c9f4433750`,
  `op_5a4e0418e33f48ec93fc0a574f631ccd`;
  `026-false-restored.json`). A positive control uses the real fuel gizmo to
  enable auto-refuel, lets the native job fill 15 fuel, then preserves true
  through another push/reload/arrival
  (`op_7a1e68cd772040c092e976335a329a32`,
  `op_f318086c74754353b82384673e1bf5a2`). The missing-field default preserves
  previous proxy behavior; it cannot reconstruct an old lost false preference.

Two load warnings refer to obsolete cart IDs in historical message
`LookTargets`; no error-level entry accompanied these final checks, and exact
cargo/owner checks passed. These warnings are retained rather than reported as a
clean log. No arbitrary nested-container, permanent site destruction or fully
blocked corpse-regeneration acceptance follows from this batch.

### Final ZPM scheduled consumer

**RG-Issue-027 decision:** after the scheduled consumer exposed mixed threat
units, the owner selected **90–110% of current colony threat on every wave**.
Keep the custom part's current-threat multiplier behavior and correct the ZPM
caller to supply dimensionless factors, matching the gate-complex caller.
Do not introduce creation-time locking or change the stated factors. Verify a
fresh protection quest and its saved range before accepting a natural wave;
older experimental quests that already stored absolute points are not silently
rewritten by a caller-only correction.

The same ongoing quest 2 from the persisted policy checkpoint reached its first
real scheduled raid at tick 23452: enableTick 3534 plus initial delay 19917 ends at
23451, then the native interval emits immediately on enable. Its 90000–120000
range controls later waves, not the first event. No timer, signal, threat outcome
or lord was injected. Random Storyteller incidents were explicitly disabled for
this lab wait; quest timers and the real raid worker remained active.

`zpm-initial-delay-boundary.json` records the true broadcasting policy,
both ZPM/housing target defs and the protection-specific letter key. The actual
attack target is housing 42766. The real letter is "Raid: Syttkon" with the
ZPM-energy-signature text, and the actual `LordJob_MaraudColony` save stores
`_priorityTarget=Thing_Rimgate_ZPMHousing42766`. Its first observed member,
Human 67698, is a live spawned hostile on Map 0; the native lord owns 152 pawns.
The full snapshot, actual pawn payload and private
`Rimgate-zpm-first-scheduled-raid` save are retained
(`op_76fae2c6e97c420db1b29f4e1526d3fa`).
A long poll deadline preceded the direct consumer observation; that timed-out
wait is not itself an acceptance pass. This closes the targeted17/21
housing/policy/letter/lord consumer check, without claiming combat victory or
raid-balance quality.

Read-only follow-up identified a separate points-contract inconsistency rather
than inferring a defect from pawn count. At the same paused 23452, native
`Outputs\\Current Threat Points` logged 145.41
(`op_accb73d7f9d5490eaa7415e45140bc62`). Quest2 stores an absolute
pointsRange 123.919945–151.457718 and false `useCurrentThreatPoints`
(the native Scribe default for its omitted key). Installed native
`QuestPart_RandomRaid` uses that range directly in the false branch; Rimgate's
override multiplies it by current threat points. The ZPM caller supplies
absolute slate points times .9–1.1, while the other GateComplex caller supplies
a factor range .85–1.10. Both callers therefore require explicit reconciliation;
blindly changing the shared branch would alter GateComplex. Exact native IL and
read-only output are retained in `native-questpart-randomraid.il.txt` and
`raid-points-readonly-output.json`. No points/balance edit was made during this
lease. The focused current-threat decision and follow-up acceptance below own
its disposition.

Finally, the native Storyteller setting was restored to true
(`op_72a5b72c3ef4448094b3f282b36bdebb`). Shutdown verified process 15976 absent
and no RimWorld process; game status is stopped. Player-config verification
matched recoverable baseline `player-config-20261125-164850-670`. The runtime
lease was explicitly returned before later agents' checks. Retained 1.6 C#/DLLs,
bundle bytes, normal player configuration and player saves were not changed.

### Current-threat ZPM raid scaling (RG-Issue-027)

The user chose each protection wave at 90–110% of current threat points, matching
the other custom raid caller, before the existing faction minimum. The 1.7 ZPM
quest node now supplies the factor range 0.9–1.1 instead of multiplying it by
quest-creation slate points. The custom raid part retains its per-wave current
threat calculation and faction minimum; its comment makes the difference from
native absolute-range semantics explicit. No new policy/save fields or factor
abstraction were added. Read-only companion diagnostics expose current threat,
actual stored range/policy and selected faction/minimum without sampling RNG.

The cold-deployed native ten-mod replay loaded the old quest 2 unchanged with
range 123.919945–151.457718 and current threat 145.49414. Real flick jobs enabled
one linked shroud, ending that quest through suppression, then disabled it.
Normal broadcasting created quest 3 with range 0.9–1.1 and false
`useCurrentThreatPoints`. A real checkpoint preserved the same quest, range,
housing/target policy and letter key
(`op_b0ab0b5edfa74605b8aeaaadc70d113f`). Current threat at that checkpoint was
145.3216; no timer or quest signal was set by the probe.

Untouched initial delay 33737+26827 produced the first scheduled wave at 60565.
Current threat was 144.5641; the selected Wraith Empire's native combat minimum
was 100. The nominal factor band is therefore 130.11–159.02 before the existing
minimum. The actual worker created one spawned hostile WraithSoldier 72026 at
(130,0), owned by `LordJob_MaraudColony`; saved `_priorityTarget` is the actual
housing 42766. The protection-specific "Raid: Wraith Domain" letter also arrived.
The real raid save is `Rimgate-zpm-current-threat-first-raid`
(`op_5df3ab377c4b467c82823154c6921ba3`), with the spawned-pawn observation
`op_16c9df8a641a400da4500765ff3a3acb`. Exact local incident-points sampling was
not added; this verifies the chosen stored contract, current-threat calculation
and actual scheduled consumer, without claiming a particular RNG draw, combat
victory or balance quality.

Full JSON, before/after saves and native lord XML are retained outside Git under
`phase3-completion-20261125-145845/zpm27/`. An initial ad hoc wait passed a Lua
object binding where the installed SDK requires explicit argument fields; its
helper guard rejected the missing path. That attempt is not a production defect
or acceptance pass. After diagnosis/acknowledgement and pausing, explicit fields
produced successful bounded ordinary-tick waits. Random Storyteller incidents
were disabled only for the lab wait, then restored before shutdown. Process 18952
was verified absent, no RimWorld process remained, and player configuration
matched its recoverable baseline before the lease was returned.

Existing private experimental quests keep their stored absolute ranges; the fix
does not reinterpret old values or repair historical quest saves. End the old
quest through normal gameplay and create a fresh one. Production and companion
builds passed with zero warnings/errors. Retained 1.6 source/DLLs, assets and normal
player files were unchanged; all changes remain unstaged/uncommitted.

### Final baseline acceptance

The follow-up baseline lease used the healthy ten-mod private profile and the
existing deployed experimental build, without source, deployment, Steam or mod
profile changes. External evidence is under
`C:/Users/david/AppData/Local/RimgateDev/evidence/phase3-completion-20261125-145845/final-baseline/`:
`summary.json`, operation payloads and nine private input/result saves. The
deployed Rimgate DLL SHA-256 was
`DF12244C4DFB5BF263EB12CC90DBA3E651CDDDF0757CBFCDCB6E61741C0B2B1B`;
the companion was
`A8547EEA35C23C8BEC1A691D6DF33AC1852F8CB933F7A8AF76CFB926CC388892`.

**Native People manifests.** Independent fresh `Rimgate-transit-active` branches
passed solo boarding, mixed pawn/loose-rifle delivery and real DHD closure during
walking boarding. The solo branch saved/reloaded the native `EnterTransporter`
job before admission, then delivered pawn 53699 and inventory marker 53702 alive
and conscious with one exact object/deep owner each
(`op_49f4ed01cc19460d91d364420e14b4a0`). The mixed manifest delivered its original
pawn, marker and rifle 53703 once; group became -1, outstanding count zero,
loading false and gate pending/queues empty
(`op_82568718d4b946c8a29d1964b73a2990`). Two bounded 200-tick mixed boarding
scans stopped while the pawn was legitimately hauling its rifle; they do not
establish a mixed walking checkpoint. Natural progression subsequently passed
the full arrival assertions. The separate real `Prioritize closing` branch
retained its source boarder 53700/marker 53703 exactly once, ended boarding and
cleared manifest/group/loading with both ends closed
(`op_f38578ca4c0a45ebbdb18b49f40818ae`). The fixture removed only the four known
hostile non-colony smoke guards before assigning any manifest. No manual native
cancel-command surface or fleeting transporter-admission checkpoint is claimed.

**Living orbital round trip.** The bounded return-only ordering succeeded.
Loading `Rimgate-orbital-protected-source`, decoding the remaining actual glyph
created the natural Voidborne Signal Relay, site 116 / quest 1 / Orbit tile176,
with AbandonedPlatform and EnemyReplicator parts. The discovery wait initially
used an invalid array polling path and timed out; the host paused and directly
re-read the actual completed discovery before continuing. This diagnostic is
retained separately from the gameplay passes. Real DHD dial, entry, home DHD
closure, orbital DHD return dial and return entry brought pawn989 home alive,
conscious and drafted (`op_02b68970a25d4440ab357153b816e7f8`). The same ordinarily
worn vacsuit55200/helmet55201 retained full HP, native resistance1 and
`HarmedByVacuum=false`; only the original torso Stab6 remained. Saving/reloading
`Rimgate-orbital-short-returned` preserved that state and the visited site/quest
(`op_c18013adb7ca4d7b85aff51c3b04b95d`). The separate earlier 600-tick exposure
sample was not repeated, avoiding unnecessary combat delay. No stronger gear,
psylink, enemy deletion, health repair, vacuum override or forced transfer was
used in this orbital branch. This closes the living round-trip gap; it does not
establish combat victory or orbital loot pickup.

**Meteorite extraction and hauling.** The original guarded mining attempt is
retained as a combat failure: the real Mine designation/job was accepted, but
the miner died before damaging the rock. Connected source confirms
`IncidentWorker_Asteroid.TriggerRaid` deliberately creates a `StageThenAttack`
guardian targeting the meteorite; this was not unrelated Marauder residue.
A fresh controlled smoke branch removed its initial four non-colony guards,
disabled random Storyteller incidents, then ran the actual meteorite worker.
It generated thirteen new ore cells and an actual Wraith guardian42782, with
the generated guarded input retained before further preparation. Only that
exact generated guardian was removed as an explicit **starting input for the
extraction subcase**, before assigning any Mine order. This separates resource
extraction from defeating the intended guard and makes no guarded-collection
or player combat-victory claim.

The native Work tab enabled Mining, then real `Prioritize mining naquadah ore`
extracted the new 4000-HP rock at (103,23)
(`op_32b79a2bf8224e0c951be397221e3c17`). The result was Naquadah42977 x32,
consistent with the authored base yield40 and installed native MiningYield
factor0.8 at the miner's unchanged level2. A real stockpile designation and
`Prioritize hauling naquadah x32` produced `HaulToCell`, with all32 units in
the actual carry tracker. Carry checkpoint/reload retained the same item ID,
quantity and single deep owner (`op_06c9938b14144e2eb72baf54f0615c7f`). Ordinary
hauling deposited that item at stockpile (120,113); final save/reload retained
exactly one spawned item42977 x32 and a living conscious miner
(`op_355adea51d9c42bf9ac3953006feb337`). No ore, material yield, job completion
or inventory outcome was inserted. Native compressed ore IDs remain unsuitable
as reload identity assertions; the extracted ordinary item ID was retained.

The native Storyteller setting was restored to true before final shutdown.
Process19252 was verified absent and the lease returned with the game stopped.
Reopen these contracts for lost/duplicated pawn or material ownership, manifest
cleanup failures, an actual protected return failure outside combat, or native
mining/hauling regressions. This evidence leaves motion/audio listening,
orbital combat victory and actual orbital loot collection as distinct checks;
it does not declare public 1.7 support or phase3 completion by itself.

### Final optional integration acceptance

November 25, 2026: the private 23-mod profile loaded every planned package with
zero order/dependency/version errors and passed 16 resolved-content/Harmony
contracts. Fresh cases used the owned `Rimgate-optional-smoke` save after native
VEF faction-add dialogs, preserving the original ten-mod smoke. Exact loaded
assembly versions/paths, operation payloads, rejected fixtures and final builds
are retained in external `phase3-completion-20261125-145845/optional-clinical/summary.json`.
Folder names alone were not treated as compatibility evidence: EBF 6.0.12.0
actually selected its `v1.6/Assemblies/EliteBionicsFramework.dll`, while Android,
Altered Carbon, EPOE and VFEMedical also selected declared legacy-folder binaries.

| Case | Actual acceptance and limits |
| --- | --- |
| EBF/EPOE | Native BionicArm plus Bruise on the same Arm measured raw max30 versus integrated max36. Actual severity5.76 produced 289 healing ticks, versus vanilla counterfactual346, through ordered queue/reload/cure. The implant and max36 remained (`op_5ba7678a8c004b57b88952aee15bb514`, `op_9b61053e89074f3e869986205c84a8ef`, `op_29e47407e3454627b82da454e9dbd003`). |
| Saurid exclusions | All three excluded conditions retained their exact IDs through independent Bruise-only treatment, reload and completion; the Bruise healed. Completion operations: `op_433c581c31684704933701e4a1d75da3`, `op_b663415e03a146bd8fd00a3162f2675b`, `op_48b452c30b1a407ea4e6f41a95ddcc66`. |
| DBH | Actual admission held BadHygiene while hygiene/bladder filled normally. Native DBH cleaning removed that condition before diagnosis, so this is not isolated queue-exclusion causality. Separate dirty BadHygiene-only input correctly reported not injured (`op_969c6bf79f4b426eb8bae93bf710889d`); Bruise treatment/reload/cure passed (`op_33ee274526cf4a508e1d5065e2a003df`). Thirst was disabled; DBH variants remain unaccepted. |
| Positive policies | RadiationIncurable, Shellcasket, Lush and Stoner entered actual treatment queues, survived queue reload and were removed by normal completion; BionicArm remained. Completion operations: `op_ee9d5072a2734c8d8370d907f4f2ee2a`, `op_5ba609310d324415924c206c36606398`, `op_a227f040e66f4e73969e66c5afcbabcc`, `op_19015a5e275942d48d66fcd9e69d4ad5`. Naturally generated scar/addiction targets were retained in the observed queue rather than cleared for the test. |
| Admission controls | Injured unrestricted human actually entered and aborted normally. EmptySleeve plus independently treatable EBF Bruise reached the intended native blocking-hediff denial (`op_62cb7ded72e44856aed2ac1d6218328f`). |
| Android | Both actual excluded xenotypes with independent tendable Burn were rejected, but upstream medical-care policy preempted the xenotype-specific denial. Resolved exclusion plus observed rejection is accepted; isolated xenotype-guard execution is not (`op_7b35869e12da41a085ea467d318592f3`, `op_6776d28ad0ab42b68cb86d8609e34abe`). |
| Rimsec | Generic PawnGenerator input was invalid because it omitted native robot initialization. The genuine DefenderSpawner produced consciousness/battery-bearing robot47119 and ran 120 ticks without that error (`op_004660a7892b4558945e651cc4bba446`, `op_3d64db3915f5421ca764dd1eb4de052b`). NotInjured preempted specific race denial; the helper now rejects that exact factory-only generic input. Race-guard clinical acceptance remains qualified. |
| Warcasket | Raw trait insertion was rejected by upstream apparel-exclusive policy; loose apparel spawning also failed. Neither is an accepted clinical input. The genuine foundry consumer remains unrun; no guessed cause or production workaround was adopted. |
| VFEMedical | Native reciprocal table/light links and real pawn flick jobs measured facility offset .15→0→.15 and surgery factor .71400005→.63750005→.71400005 (`op_6a2b2b44d88e4002b6aff4f52687624c`, `op_c26cff05adff44d79bd3417efc4fb568`, `op_43022628435941e6b04c1fa4b81ebec8`). This proves link/power/stat behavior, not surgery outcomes. |
| Alpha Genes | The exact passive observer registered, and a real player-forced AttackStatic targeted the occupied sarcophagus. Normal treatment discharged the patient before destruction within the bounded 900-tick attempt; zero qualifying guard calls means suppression remains unproven. Actual End removed the observer (`op_d375227cc13c4956b5eec1af67b9dd48`, `op_09bc19f765424a0a9eeac7585e2ad1f1`, `op_86d77ca061ef47ee9f5325a06f667f1f`). Registration/consumer audit and watch cleanup are distinct evidence. |

The initial EBF observer itself violated upstream protocol by calling the patched
vanilla getter (attention12, stack naming `MedicalProbe.Tracked`). It now uses
the installed `EBF.VanillaExtender.GetRawMaxHealth` explicit vanilla-intent API
for its raw comparison. This was a measurement correction, with its failing
payload retained, rather than a Rimgate gameplay fix. Other rejected boundaries
included invalid generic Rimsec construction, invalid Warcasket inputs, a native
Flick instruction dialog that force-paused input, and selecting Fire at will
instead of the weapon-target command on the first Alpha setup. Those do not
establish mod or engine defects.

Actual authored NaniteRegeneration plus missing natural Finger exposed two
production defects, separately reproduced before correction:

- [RG-Issue-029](issue-tracker.md#rg-issue-029): paused save emitted the Thing
  `Scribe_Values` error for `_cachedMechWeapon`
  (`op_7337cf07a2b942c299533ce31aed89b8`, attention35). Same-key deep serialization
  now matches the field's removed-equipment ownership.
- [RG-Issue-030](issue-tracker.md#rg-issue-030): 120 ordinary native ticks emitted
  EBF's protocol error from actual `TryRegrowOneMissingPart` line115
  (`op_364a83734d3a45bbb7dac8daa47e9509`, journal36). All four regeneration health
  queries now use the existing EBF wrapper with their actual BodyPartRecord;
  whole-body chronic fallback remains1f.

Cold final-binary replay loaded the retained pre-regrowth save, saved before
processing without the Scribe error, then naturally restored the Finger and
created RegeneratingNanites246 in 120 ticks. Save/reload retained that ID and
severity5.0133333 plus original Nanite241 (`op_8e97e611dae44711900a6c9a8fd4e684`,
`op_0f30b4227a7c48c8a53a21d2d06eec6f`, `op_cf0eacc8bd1b40f6a390748b5c7c8403`).
Final-process error journal was empty (`op_25cc5cd1cb834cea85e34957ed004618`).
Nonnull cached mech-weapon/resurrection, colony-mech energy and chronic branches
were not live sampled; their shared ownership/API corrections passed independent
source review. This does not claim those distinct outcomes were exercised.

Production and companion builds passed with zero warnings/errors. Final deployed
SHA256: production `BA4C275985773B6609882ED1EC5CFFBD2B57EFB5B6D51543B6832F3670BF4CC3`;
companion `1930181DEAC95C8F3FEC00B3B69BE601A86B831147C91B09D09574123A25EE56`.
All nine changed/new scenarios compiled through the actual installed SDK; the
aggregate operation IDs are retained in the summary. Process22568 was verified
stopped before returning the lease. Root-owned absence/config preservation and
final build checks follow separately. Quarry is metadata1.6-only and excluded.
Phase3 remains active for coordinated manual acceptance, qualified branches and
the open [RG-Issue-028](issue-tracker.md#rg-issue-028) nested-corpse candidate;
these results do not declare public1.7 support or completion of every integration.

### Automated pass closeout and remaining acceptance

The final November 25 follow-up cold-started the restored **ten-mod** profile
with all optional integrations absent. A fresh owned medical input supplied a
missing Finger; the native Add Hediff action added authored NaniteRegeneration
to the whole pawn. Actual save/load, 120 explicitly stepped ticks and final
save/load preserved Nanite211 and RegeneratingNanites212, restored the Finger
and retained regrowth severity 5.0133333/max 8. Native load readiness also advanced
ordinary ticks; this is not a claim of exactly 120 total elapsed ticks. The fresh
error journal was empty (`op_e8f693c1e919426e8af95624f384f60c`), and all 16 absent
content/hook contracts passed (`op_7c3178164d2349ddbe625ce42c50a1bf`). This accepts
the wrapper's vanilla fallback and the null-weapon save case alongside the
present-EBF replay above. It does not extend the nonnull mech/chronic proof.
Exact payloads, both new input/result saves and the native log are retained as
`final-absence.json`, `Rimgate-nanite-absence-*.rws` and
`final-absence-Player.log` under the same private completion evidence directory.

Final validation passes the supported 1.6 library and strict DocFX/site checks,
portable pinned-reference 1.7 build, installed-runtime-reference 1.7 build and
development companion, with zero warnings/errors. The final runtime DLLs match
the production/companion hashes in the optional acceptance record. All nine
changed/new Lua scenarios compiled through the installed SDK. Independent source
review found no introduced blocker, including the last two regeneration fixes;
the nested-corpse candidate is tracked separately rather than treated as fixed.
All nine PowerShell scripts and 176 authored Mod XML files parse; 87 tracking/
AGENTS relative links and anchors, all 30 issue IDs and `git diff --check` pass.
Live 1.6 gameplay was not rerun.

Verified shutdown removed process 10200. The full private pre-optional config,
original task Prefs/ModsConfig and four original owned input saves were restored
and byte-checked; the generated private Rimatomics settings were removed. Full
inventories/hashes match for **125 normal config files, 10 original player saves
and four retained 1.6 bundles**. The setup helper's player-config verification
also passes. Source/1.6 and Mod/1.6 have no Git diff; the retained DLL hash remains
`804D03320F63466457EAD8B4B14445AAA5035CE045EB0740F1032ACAA573738A`.
The native pack remains
`0dea55cedec8eb2f14bc1fed2ac78b9f6fe1e1b4e2e5ec03f02f3568e8ba6797`.
Raw assets, Unity builder, public support metadata, dependency pins, Steam branch
and the local 1.6 insurance snapshot are unchanged. The index remains
`5b32eb2c0c2351376a9440b7f2d2748fcd189c2f`; all edits are unstaged/uncommitted.

The feasible background pass is finished. **Phase 3 remains active** for the
[coordinated manual quality pass](#coordinated-manual-quality-pass), the qualified
optional branches above and the bounded [RG-Issue-028](issue-tracker.md#rg-issue-028)
investigation/disposition. Next resolve that saved-state candidate, then review
motion, representative native texture appearance and actual device playback in
a coordinated visible private session. No foreground focus or output-volume
change was made during this pass. Phase 4/public support/defaults/final freeze
remain pending; background success does not waive those remaining gates.

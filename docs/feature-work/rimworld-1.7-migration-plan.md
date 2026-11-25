# RimWorld 1.7 Migration Plan

- **Status:** In progress; **phases 0, 1 and 2 complete. Phase 3 is next.**
  Versioned source split and experimental 1.7 compilation
  implemented; the existing local snapshot is temporary migration insurance.
  Live 1.7 startup/profile and hidden-window smoke checks passed earlier;
  [RG-Issue-002](issue-tracker.md#rg-issue-002) is closed after a verified
  private-profile occlusion/redraw correction and startup regression. Owned asset loading and
  powered iris close/open rendering pass for Unity and pack-only native assets.
  The native comparison is complete; the owner selected native as the 1.7
  development default, retaining explicit Unity comparison and the 1.6 pipeline.
  Known-address DHD dialing, lazy destination generation, pawn/inventory transit,
  queued/active and both in-flight save checkpoints, redraft and closure now pass.
  [RG-Issue-003](issue-tracker.md#rg-issue-003) and
  [RG-Issue-004](issue-tracker.md#rg-issue-004) are corrected in 1.7 only.
  Planetary glyph discovery, quest/site persistence and visitation now pass;
  selected production sounds also produce finite nonzero engine PCM.
  Native gate/controller presentation passes 25 sampled states after the 1.7-only
  [RG-Issue-005](issue-tracker.md#rg-issue-005) altitude/pitch correction.
  Conditional iris glow, condition ownership/reload/end and the final complete
  gate replay also pass; RG-Issue-006/007 are corrected in 1.7 only.
  Broader feature/edge-case, device/listening, orbital, optional-integration and
  old-save acceptance belong to phase 3. The 1.6 freeze/support milestone is phase 4.
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

| Phase | Current disposition | Owning evidence / next gate |
| --- | --- | --- |
| 0 - Runtime and protected baselines | Complete | Exact licensed runtime, isolated profile and byte-backed preservation checks established. |
| 1 - Versioned experimental projects | Complete | Independent 1.6/1.7 source, explicit experimental output, pinned references and CI compilation. |
| 2 - One complete production gate path | Complete | [Phase-2 closeout](#phase-2-completion): startup/patches, native assets, planetary discovery, dialing, pawn/inventory transit, save checkpoints, closure and ordinary/conditional iris presentation. |
| 3 - Wider feature/save/dependency matrix | Next | Gate edge cases and copied 1.6 saves, then other systems, orbital discovery, optional dependencies and remaining manual quality checks. |
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

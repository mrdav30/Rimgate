# Development Tooling Hardening

- **Status:** Completed implementation and recorded verification; the preferences
  anomaly remains a deferred investigation in [RG-Issue-001](../issue-tracker.md#rg-issue-001).
- **Recorded:** November 23, 2026; these are historical observations, not a new run
  or evidence of a published mod release.
- **Implementation:** [dfdb384](https://github.com/mrdav30/Rimgate/commit/dfdb3840dc11ad0a5b902982fd4ae2003f056259).
- **Evidence source:** Extracted from the contributor guide at
  [527257e](https://github.com/mrdav30/Rimgate/blob/527257ed7a2469dfb83eaabb213afd6948f7cc2d/docs/guides/developer-and-contributor-notes.md#build-tooling-verification-november-23-2026).
- **Scope:** Agent workflows, version-aware 1.6 builds, byte-preserving bundle
  relocation, explicit Unity version selection, isolated RimBridge validation
  and PowerShell/Lua script consolidation. RimWorld 1.7 migration was excluded.
- **Current instructions:** [Contributor guide](../../guides/developer-and-contributor-notes.md).

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

The pinned GABS **1.1.2** and RimBridgeServer **2.5.0** packages were installed and exercised
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
Lua source and [contributor workflow](../../guides/developer-and-contributor-notes.md#live-16-validation-with-rimbridge) reproduce the check without those captures. Keep
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


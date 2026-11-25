# Rimgate agent instructions

Rimgate is a Stargate-inspired RimWorld mod, maintained as a public GitHub source
repository and used locally. It has not been published as a mod release.

## Workflow and source of truth

Use the repository's [Rimgate specialization](.agents/skills/rimgate-development/SKILL.md),
which applies the shared [RimWorld modding workflow](.agents/skills/rimworld-modding/SKILL.md).
Keep this entry point concise; detailed instructions belong in the owning docs:

- [Developer and Contributor Notes](docs/guides/developer-and-contributor-notes.md):
  builds, version/source ownership, assets, validation and packaging.
- [Compatibility Matrix](docs/guides/compatibility-matrix.md): dependencies and patches.
- [Guides](docs/guides/index.md): feature guides; trace affected source before editing.
- [Feature work overview](docs/feature-work/feature-work-overview.md): active,
  deferred and completed work, with links to owning plans.
- [Issue tracker](docs/feature-work/issue-tracker.md): defects and investigations.
- [Performance signal backlog](docs/feature-work/performance-signal-backlog.md):
  measured performance and measurement-reliability concerns.

Documentation is authored in this repository; the GitHub Wiki submodule has been
removed. Leave changes unstaged/uncommitted unless explicitly instructed
otherwise. Do not commit, push or publish as an implied documentation step.

## Current contracts

- Only RimWorld **1.6** is declared supported. The build scripts default to it and accept
  `-RimWorldVersion 1.6`; an unconfigured target must fail. Version selection does
  not establish compatibility. The separate `Source/1.7/Rimgate/` project is an
  experimental .NET 10/C# 14 port. Only assembly builds accept `-Experimental`
  with `-RimWorldVersion 1.7`; outputs stay in ignored `.assets/experimental/`.
  Do not add public 1.7 support or package its outputs before runtime validation.
- `Source/1.6/Rimgate/` owns the retained net48 implementation; `Mod/1.6/Defs/` and `Patches/`
  own its XML. Preserve def names, save keys and pawn/container ownership.
- The private 1.7 package reuses authored XML from `Mod/1.6/`. Correct shared
  Rimgate-owned XML, localization and content directly when both versions can
  consume the correction with their existing assemblies. These maintenance fixes
  do not require DLL changes or a separate approval for each named definition.
  Preserve identities, save contracts and unrelated behavior; new features,
  balance changes and assembly/schema changes still follow the task's scope.
- Prefer direct source edits and existing defs/comps/hooks. Use XML patches when
  editing dependency-owned content or when a genuine version difference cannot
  be handled through authored version-owned content and load folders;
  use Harmony only when the required behavior cannot use an existing extension
  point. Do not duplicate a shared source correction in a version patch. Keep
  version-specific C# work in its owning project; leave the retained 1.6 DLL/C#
  unchanged unless explicitly scoped as maintenance.
- Once 1.7 is accepted, freeze the final 1.6 source/content in Git and move
  active builds, dependencies, runtime checks and documentation to 1.7. Later
  1.6 work requires explicitly scoped maintenance. The existing local snapshot
  is temporary migration insurance; keep it untouched and outside Git. It is
  not a maintained capture/restore workflow or an ongoing 1.6 environment contract.
- Raw art/audio belongs in `AssetsRaw/`. The retained 1.6 pipeline rebuilds through
  `.assets/scripts/build-assetbundle.ps1`. Experimental 1.7 setup accepts
  `-AssetFormat UnityBundles|WaylithPack` (native default for 1.7, Unity for 1.6);
  the native helper
  `.assets/scripts/build-waylith-assets.ps1` stages/bakes only under ignored
  `.assets/experimental/`. Keep the four native pack files together. Do not
  manually place raw assets in tracked `Mod/` or edit generated packs/bundles.
- Bundles go to **`Mod/<version>/AssetBundles/`** (`Mod/1.6/AssetBundles/` today).
  Keep the shared `Mod/AssetBundles/` location empty. Preserve baseline bytes
  during path-only changes; rebuilding assets is separate from correcting XML.
  Do not add/update generated artifacts in commits without explicit instruction.
- `UnityAssetBuilder/ProjectSettings/ProjectVersion.txt` owns the builder editor
  version. Read it before selecting an editor. Preserve the built-in render pipeline,
  `StandaloneWindows64` target and `ChunkBasedCompression` (LZ4); no scenes needed.
  Do not convert the builder to URP or HDRP.
- Check optional integrations with their dependencies absent and present as applicable.

## Work tracking

Keep status and detailed evidence in one owning plan or tracker entry. Feature
work gets descriptive plan names; preserve `RG-Issue-NNN` and `RG-Perf-NNN` IDs
through closure. Record the actual game/dependency context and distinguish
verified fixes from mitigations or investigations without a reproduced cause.
Archive completed plans in `docs/feature-work/done/`; keep guides evergreen.
Tracking records stay repository-only and are excluded from the DocFX site.
Small fixes and routine documentation edits do not need separate feature plans.

## Build and verification

Keep PowerShell and Lua tooling in `.assets/scripts/`. Default build logs stay
at the repository root and remain gitignored. From the repository root:

```powershell
.\.assets\scripts\build-assembly.ps1 -RimWorldVersion 1.6
.\.assets\scripts\build-assetbundle.ps1 -RimWorldVersion 1.6
.\.assets\scripts\build-release.ps1 -RimWorldVersion 1.6
```

The release script builds local outputs; it does not publish. Use the contributor
guide for dependency-path overrides and checks. Parse XML and build C# as needed,
then verify the affected gameplay and save/load path when runtime behavior changes.
Compilation alone does not prove gameplay compatibility. State unrun checks.

Build the library, Guides and API reference with `.assets/scripts/build-docs.ps1`.
It restores pinned public references into `.assets/references/<version>/`, builds the selected latest configured
target and runs DocFX with warnings as errors plus generated-site link checks.
Keep `docs/docfx.json`, navigation, guides and templates authored; do not commit
`docs/obj/` or `.assets/references/`. Keep code changes and their guide/API comments
in the same PR. The documentation workflow validates PRs and publishes `main`.

For live validation, use the contributor guide's isolated RimBridge setup and
scenario protocols. Refresh the selected deployment with all game instances
stopped; match the installed game/dependency set to its reference pins. The
experimental 1.7 profile uses `rimgate-dev` / `gameId: rimgate17`; bridge tooling
and companions stay outside the player package. Preserve player files and
recoverable config bytes before setup; verify them after shutdown.

Prepare fixture inputs and observe outcomes; real gameplay jobs must perform the
operation. Check both bridge status and gameplay payloads, exact quantities,
object identity, deep ownership and cleanup across interruption/save/load.
Compile changed Lua with the installed SDK; offline parsing alone is insufficient.
Use the guide to select relevant scenarios, phases and cleanup rather than
reproducing those procedures here. Pause/clean up bounded checks on failure and
state what remains unverified. Patch registration does not establish behavior.

Use graphics-enabled hidden execution for background input/capture and coordinate
any foreground computer use. Do not substitute graphics-free headless mode for
visual checks. The guide owns launch settings, redraw initialization and capture
diagnostics. Keep material resolution, rendered pixels, sampled audio and audible
playback as separate evidence. Current results and remaining gates belong in the
owning plan/tracker, not these instructions.

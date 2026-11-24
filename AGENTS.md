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
- Once 1.7 is accepted, freeze the final 1.6 source/content in Git and move
  active builds, dependencies, runtime checks and documentation to 1.7. Later
  1.6 work requires explicitly scoped maintenance. The existing local snapshot
  is temporary migration insurance; keep it untouched and outside Git. It is
  not a maintained capture/restore workflow or an ongoing 1.6 environment contract.
- Raw art/audio belongs in `AssetsRaw/`. Rebuild through
  `.assets/scripts/build-assetbundle.ps1` after asset edits. Do not manually place
  raw assets in `Mod/` or edit bundles.
- Bundles go to **`Mod/<version>/AssetBundles/`** (`Mod/1.6/AssetBundles/` today).
  Keep the shared `Mod/AssetBundles/` location empty. The four existing baseline
  files were relocated without changing their bytes. Do not regenerate baseline
  files for a path-only move or add/update generated artifacts in commits without
  explicit instruction.
- `UnityAssetBuilder/ProjectSettings/ProjectVersion.txt` owns the builder editor
  version (currently **6000.3.9f1**). Preserve the built-in render pipeline,
  `StandaloneWindows64` target and `ChunkBasedCompression` (LZ4); no scenes needed.
  Do not convert the builder to URP or HDRP.
- Prefer existing defs, comps and narrow hooks over broad Harmony patches. Check
  optional integrations with their dependencies absent and present as applicable.

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

For experimental live 1.7 checks, run `.assets/scripts/setup-rimbridge-dev.ps1
-RimWorldVersion 1.7` with all RimWorld instances stopped, then use the
`rimgate-dev` MCP server and `gameId: rimgate17`. The installed build must match
the experimental reference pin. Only the private test package declares 1.7;
the player package still declares 1.6. The CI workflow also compiles experimental
1.7 using pinned public dependencies; compilation is not runtime acceptance.
The contributor guide owns setup, tool discovery and the
`.assets/scripts/rimbridge-smoke.lua` acceptance case. After shutdown, run
`.assets/scripts/setup-rimbridge-dev.ps1 -VerifyPlayerConfig` against the
recoverable config backup. Keep normal player saves/mod configuration untouched
and validate both bridge operation status and gameplay payloads. RimBridge is
development tooling, not a dependency of Rimgate's player package.

The live 1.7 smoke currently stops at synthetic input/screenshot timeouts
([RG-Issue-002](docs/feature-work/issue-tracker.md#rg-issue-002)). Do not claim
rendered assets, iris save/load or gate transit acceptance from startup alone.

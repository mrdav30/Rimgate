---
name: rimgate-development
description: Develop Rimgate, the RimWorld mod in mrdav30/Rimgate. Applies to its gameplay, defs, compatibility patches, saved state, build and asset tooling, contributor docs and reviews. Specializes the shared rimworld-modding workflow.
---

# Rimgate Development

Use the sibling [rimworld-modding](../rimworld-modding/SKILL.md) workflow and the
repository's [AGENTS.md](../../../AGENTS.md). Resolve repository links from this
versioned skill directory. If loaded through a local junction, resolve its
physical target before following repository links. Rimgate is a public source
repository used locally;
it has not been published as a mod release. Do not infer Workshop distribution.

## Read the owner for the task

- Build, source layout, assets, checks and the 1.6 freeze strategy:
  [Developer and Contributor Notes](../../../docs/guides/developer-and-contributor-notes.md).
- Live runtime validation: the same guide's RimBridge workflow,
  [setup-rimbridge-dev.ps1](../../../.assets/scripts/setup-rimbridge-dev.ps1) and
  [rimbridge-smoke.lua](../../../.assets/scripts/rimbridge-smoke.lua). Use the isolated
  `rimgate16` profile; discover the installed bridge's schemas rather than
  assuming current upstream examples match its frozen 1.6 payload.
- Required dependencies and optional patches:
  [Compatibility Matrix](../../../docs/guides/compatibility-matrix.md), checked against
  Mod/About/About.xml and the affected patch/C# code.
- Gameplay: start with the matching page in [Guides](../../../docs/guides/index.md),
  then trace the implementation. Guides describe intended behavior; source and
  reproduction determine whether a defect exists.

Guides and the generated API site are owned by this repository under `docs/`;
the former wiki submodule has been removed. Build with
`.assets/scripts/build-docs.ps1`, which compiles Rimgate against pinned public
references and runs strict DocFX/site checks. Keep guide links relative with
their `.md` extensions or use API xrefs. Keep authored navigation/config/templates
in source control; `docs/obj/` and restored `.assets/references/` are generated.
The site follows `main` and the latest configured target, with no legacy wiki
mirrors. Preserve user changes and leave work unstaged/uncommitted for review.
Record concise evidence in the owning page or existing issue/plan; do not create
parallel feature reports.

## Respect the current boundaries

Only RimWorld 1.6 is currently configured. Source/Rimgate is the 1.6 source owner;
Mod/1.6 owns its defs, patches, assembly and AssetBundles. Keep generated bundles
in Mod/<version>/AssetBundles, with none in the shared Mod/AssetBundles location.
AssetsRaw owns raw
art/audio, and UnityAssetBuilder owns bundle generation. Preserve the built-in
render pipeline, Windows64 target and LZ4 compression. Do not manually copy raw
assets into Mod, manually edit bundles or add generated artifacts to commits.
The four existing 1.6 baseline files were relocated without changing their bytes.
Preserve their bytes during path-only moves; regeneration and publication of
replacement bundles require their own requested scope.

Use the build scripts in `.assets/scripts/` and select the target explicitly when
relevant. Keep PowerShell and Lua tooling there; default build logs stay at the
repository root.
Version selection does not authorize a 1.7 migration, metadata support bump or
replacement asset pipeline. Keep framework/dependency changes tied to evidence
from the target game. Do not move source folders merely to prepare empty targets.

## Follow the affected system

Runtime code lives under Source/Rimgate, with XML references centralized in
Rimgate_DefOf. Follow the feature's Things/Comps, AI/jobs, World, UI, utilities and
HarmonyPatches together as applicable. Representative stateful owners include
Building_Gate plus WorldComp_GateAddresses, Building_Sarcophagus,
Building_CloningPod, Building_MobileContainer, Building_ZPM and Building_ZPMHousing.

For these systems, check the actual operation, interruption/cleanup and save/load
continuation; use the contributor guide's task-specific checks. Preserve defName,
save keys, pawn/container ownership and cross-map/world references. Verify
research, power, fuel and access prerequisites before diagnosing a blocked action
as a bug. UI string changes also require the corresponding English keyed entries.

Optional XML integrations and C# build references are different contracts. In
particular, the 1.6 project needs Dubs Bad Hygiene reference binaries to compile
although it is not declared as a required runtime mod in About.xml. Check guarded
call paths instead of silently making it a mandatory player dependency.

---
name: rimworld-modding
description: Develop and maintain RimWorld mods, including XML defs and patches, C#/Harmony integration, saved state, assets, compatibility, build tooling and version upgrades. Use for mod implementation, investigation, reviews and contributor documentation.
---

# RimWorld Modding

Read the repository's AGENTS.md and owning feature/build documentation first.
Apply a project specialization when one exists; for Rimgate use the sibling
[rimgate-development](../rimgate-development/SKILL.md). Repository contracts and
user scope govern the work. Keep release, publication and engine migration work
separate unless requested.

## Establish the actual target

Record the RimWorld version/build, active DLC, dependency versions and load order
needed for the affected behavior. Check About.xml, LoadFolders.xml, project
references and the actual installed assemblies. A folder name, successful build
or supportedVersions entry alone does not establish runtime compatibility.

For engine/version upgrades or asset-pipeline changes, read
[version and asset boundaries](references/versions-and-assets.md). Verify changing
API claims against the target installation and primary upstream sources. Label
announced behavior, observed behavior and unresolved assumptions separately.

## Follow the feature through the game

Trace the owning def/patch, DefOf references, C# class, comps, jobs, Harmony hooks,
localization and persisted state. Prefer an existing game or mod extension point
over another patch or parallel subsystem. Check XML inheritance, cross-references
and patch order; well-formed XML can still be invalid game content.

Keep Harmony targets narrow and inspect the target method in the exact game or
dependency build. Preserve original behavior outside the mod's intended case.
For optional integrations, inspect both XML activation and C# type access; a
conditional patch does not make an unconditional assembly/type dependency safe.
Exercise the affected path with the optional mod absent and present.

Use the game's numerical types, random APIs and lifecycle where appropriate.
Preserve random-state discipline, meaningful ordering and tick-driven gameplay.
Do not introduce wall-clock or render-frame dependencies into simulation logic.
LSF-specific fixed-point, collections and engine-independent-library policies do
not automatically apply to a RimWorld mod. Multiplayer support is a separate
contract when requested.

## Prove the smallest complete behavior

Reproduce defects before choosing a fix. Where practical, demonstrate a failing
regression against the old behavior and verify it after the change. Prove one
representative gameplay path before broadening it. New infrastructure needs an
actual consumer in the change; prefer hardening the current owner.

For persisted state, inspect ExposeData/Scribe ownership, defaults, reference
resolution and post-load reconstruction. Preserve save keys, defName identity
and object ownership or provide a deliberate migration. Check an existing save
as well as a newly created case when changing stored state. Use a copy of a
player's save and preserve the original.

Scale checks to the change:

- Docs: compare instructions and links with the current scripts/source.
- XML: parse, inspect def references and patch applicability, then load the
  affected content in-game when runtime validation is needed.
- C#: compile against the selected target; exercise the actual job, building,
  UI, incident or world transition that consumes the change.
- Stateful behavior: save, exit/reload and continue the affected operation.
- Assets: rebuild through the owning pipeline and verify loading, visuals/audio
  and platform constraints in the target game.

Use pure automated tests where they exercise real separable logic. Do not build
a mock RimWorld runtime merely to obtain passing tests. A debugger helps diagnose
a failure; attaching one is not an acceptance check. Compilation, startup,
gameplay and save/load are distinct evidence. State what could not be run.

When a live bridge is available, discover the installed tool schemas and verify
the loaded mod paths and save-data profile before mutations. Check both operation
status and the returned gameplay payload: a handler can complete successfully
while reporting `success: false`. Reacquire selection-scoped UI IDs after changes.
Keep repeatable scenarios in the repository and assert observable state across
save/load; do not add a bridge runtime dependency to the player mod for testing.
Before changing a local launch environment, preserve actual player configuration
files as well as their hashes. After shutdown, compare them with the baseline;
an isolated save-data argument alone does not prove all player files stayed intact.

## Review and hand off

Measure the executed workload before optimizing tick/job/pathing code. Include
game build, colony/mod-list context, units and repeated observations for claims.
Preserve the agreed behavior and performance baseline. Resolve defects and
confirmed regressions introduced by this change before handoff; only the user
can accept a measured tradeoff. Inconclusive measurements establish neither a
regression nor zero overhead.
When repeated fixes fail, identify the shared assumption and run an experiment
that can distinguish causes before adding another fallback.

Review connected callers, patch scope, save compatibility, optional dependencies,
allocations in hot paths and redundant machinery. Use independent review when
the change's risk warrants it and delegation is authorized; identify self-review
as such. Record reproducible out-of-scope defects in the repository's existing
tracking convention without unrelated runtime changes.

Keep concise reproduction and validation evidence in the owning docs/issue or
existing plan, including missing evidence. Report source/config changes apart
from generated artifacts. Leave changes unstaged/uncommitted unless instructed
otherwise; publication requires its own authorization.

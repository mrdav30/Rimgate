---
name: rimgate-development
description: Develop Rimgate, the RimWorld mod in mrdav30/Rimgate. Applies to its gameplay, defs, compatibility patches, saved state, build and asset tooling, contributor docs and reviews. Specializes the shared rimworld-modding workflow.
---

# Rimgate Development

Apply the sibling [rimworld-modding](../rimworld-modding/SKILL.md) workflow and
the repository's [AGENTS.md](../../../AGENTS.md). Resolve links from this versioned
skill directory; when loaded through a junction, follow its physical target.
Rimgate is a public source repository used locally, without a published mod
release. Preserve user changes and leave work unstaged/uncommitted unless asked.

## Read the owner for the task

- [Developer and Contributor Notes](../../../docs/guides/developer-and-contributor-notes.md)
  own builds, version/content ownership, assets, isolated runtime setup and scenario
  protocols. Read the affected section rather than copying its procedures here.
- [Compatibility Matrix](../../../docs/guides/compatibility-matrix.md) owns required
  and optional integrations; verify against metadata and connected source.
- [Guides](../../../docs/guides/index.md) describe intended gameplay. Trace the
  implementation before treating a discrepancy as a defect.
- [Feature work overview](../../../docs/feature-work/feature-work-overview.md)
  routes to owning plans, including migration status and the version-freeze
  milestone. [Issue tracker](../../../docs/feature-work/issue-tracker.md) owns
  defects; [performance backlog](../../../docs/feature-work/performance-signal-backlog.md)
  owns measured performance and measurement reliability.

Keep decisions, dated results, limitations and reopening criteria in one owning
plan/tracker. Preserve issue/performance IDs through closure and archive completed
plans in `docs/feature-work/done/`. Routine fixes need no new feature plan. Guides
stay evergreen; tracking records stay outside DocFX navigation/content.

## Choose the source owner before editing

Only 1.6 is publicly supported during the current migration. `Source/1.6/Rimgate/`
owns its retained net48 implementation; `Source/1.7/Rimgate/` owns the experimental
port. The build resolver, metadata and contributor guide own configured targets,
reference pins and outputs. Version selection or compilation alone does not
establish support; keep experimental outputs in ignored development locations.

Private 1.7 setup reuses authored content from `Mod/1.6/`. Correct shared
Rimgate-owned XML, localization and content directly when both versions can use
the correction with their existing DLLs. Routine content maintenance does not
require a DLL rebuild or another approval for each named def. Preserve identities,
save contracts and unrelated behavior; feature/balance/assembly/schema changes
still follow the requested scope. Confirm every referenced type and field exists
in the consuming assemblies, then validate the affected native behavior.

Prefer an authored source correction, then an existing def/comp/hook. Use XML
patches for dependency-owned content or genuine version differences that cannot
be handled through authored version-owned content and load folders. Use Harmony
only when existing extension points cannot provide the behavior. Do not add a
version patch for a shared source fix, or introduce patch/deployment machinery
without an actual consumer.

Keep C# changes in the owning version project. Preserve retained 1.6 C#/DLLs
unless maintenance is explicitly scoped. The migration plan owns when accepted
1.6 source/content is frozen and active tooling/dependencies/docs move to 1.7;
later frozen-version changes require explicit maintenance scope. Keep the local
insurance snapshot untouched outside Git; it is not a maintained restore workflow.

## Verify the actual operation and saved state

Trace affected defs, comps/buildings, jobs, UI, world owners and Harmony callers
together. Verify research, power, fuel, access and optional dependencies before
diagnosing missing behavior. Compilation references and runtime mod requirements
are distinct; inspect guarded call paths before making an integration mandatory.

For stateful work, run the actual operation, interruption/cleanup and save/load
continuation. Preserve def/save identities, exact quantities and pawn/container
ownership across maps and worlds. Queue/job references are not deep ownership;
use reference equality when detecting duplicate objects. Keep temporary Scribe
data through the phases that populate, resolve and consume it. Check native save
representations as well as custom serialization; successful loading alone does
not establish quantity or identity preservation.

Use the contributor guide's bounded RimBridge scenarios on the expected private
profile and loaded mod paths. Companions prepare inputs and observe; real jobs
must produce the outcome. Validate bridge operation status and gameplay payloads.
Compile changed Lua through the installed SDK, use fresh selection-scoped UI IDs,
and apply host pause/cleanup on failure. Record failed fixtures separately from
production defects; stop bounded investigations when their required evidence is
unavailable. Keep recoverable player-config bytes and verify them after shutdown.

Prefer hidden graphics-enabled input/capture; coordinate foreground computer use.
The guide owns launch parameters, provider/redraw initialization and diagnostics.
Keep asset resolution, rendered pixels, sampled audio and audible playback as
separate claims. Registration of a Harmony patch does not validate its behavior.

## Keep build and asset boundaries intact

Keep PowerShell/Lua in `.assets/scripts/`; root build logs remain ignored.
Use the version-aware build helpers and pinned reference restoration. `AssetsRaw/`
owns authored art/audio. Rebuild through the owning version's pipeline; keep
generated bundles/packs out of commits unless requested and preserve baseline
bytes for path-only changes. Unity builder settings come from its project and the
contributor guide. Native pack files and their sidecars must stay together.

Documentation is authored here under `docs/`; the former wiki is not maintained.
Build with `.assets/scripts/build-docs.ps1` for the supported library, strict
DocFX and generated-site checks. Keep guide/API changes with their implementation;
use relative `.md` links or API xrefs. Keep `docs/obj/` and reference caches ignored.
The hosted site follows `main` and the configured supported target. Documentation
work does not imply permission to commit, publish or change support metadata.

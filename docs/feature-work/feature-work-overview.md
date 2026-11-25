# Feature Work Overview

## Purpose and ownership

This is the entry point for significant Rimgate feature work. Keep a curated view
of active scope, agreed deferred work and significant completed work. Design,
decisions, phase results and acceptance evidence belong in the owning plan.
Small fixes and routine documentation edits do not need separate plans.

- **Features:** Use a descriptive `<feature>-plan.md` in this directory; features
  do not receive bug IDs. Link related investigations rather than copying them.
- **Defects and investigations:** Use the [issue tracker](issue-tracker.md).
- **Measured performance and measurement reliability:** Use the
  [performance signal backlog](performance-signal-backlog.md).

Keep status in one owning record and update this overview when priorities or
milestones change. Active and deferred plans stay here. Move completed plans to
`done/YYYY-MM-DD-<feature>-plan.md`, record their disposition and update incoming
and relative links. Retain a short completion summary here; historical results
do not imply a fresh validation run or a published mod release.

These records are repository-only. The [contributor guide](../guides/developer-and-contributor-notes.md)
owns current development instructions; [Guides](../guides/index.md) and the
generated API site describe supported behavior. Keep dated implementation and
verification history here rather than appending it to those guides. Do not add
tracking records to the DocFX content or navigation.

## Active feature work

### RimWorld 1.7 migration

[Migration plan](rimworld-1.7-migration-plan.md) — in progress. Separate version-owned
1.6 and experimental `net10.0`/C# 14 projects compile. The plan owns the 1.6
source/content freeze milestone and remaining runtime, asset, dependency and save
gates. Active development will follow accepted 1.7; the existing local snapshot
is temporary migration insurance without maintained capture/restore tooling.
Only 1.6 remains declared supported; 1.7 outputs and metadata stay in a private
test package. Licensed 1.7 startup, profile isolation and read-only bridge calls
now work. The hidden-window smoke passes gate selection, iris mutation/save/reload,
120 exact ticks and screenshot capture. [RG-Issue-002](issue-tracker.md#rg-issue-002)
was reopened for recurring capture/input timeouts and is now closed with a
verified private-profile occlusion/redraw correction. The regression proves
failure with redraw suppressed and repeated real UI/capture plus save/reload
success with it enabled; the previously blocked discovery visit also passes.
The asset pass resolves all 814 bundled textures and 26 clips and
renders the powered iris closed/open through real pawn jobs; the two dev red Xs
are inherited null-icon defaults. Known-address DHD dialing, lazy map generation,
pawn/inventory transit, both in-flight save checkpoints, redraft and closure now
pass. The 1.7-only address-cache and pending-ownership corrections close
[RG-Issue-003](issue-tracker.md#rg-issue-003) and
[RG-Issue-004](issue-tracker.md#rg-issue-004). Planetary glyph decoding, generated
quest/site/address persistence, lazy quest-map generation and visitation pass.
Gate open/idle/close and iris open/close produce finite nonzero production-voice
PCM with master output muted. Broader effects, orbital discovery, device/listening
checks and optional integrations remain open. Native gate presentation now passes
25 sampled states across four ancient-controller rotations and North with Tau'ri.
[RG-Issue-005](issue-tracker.md#rg-issue-005) corrects duplicate overlay altitude
and mismatched mesh pitch that hid layers at close zoom; 1.6 is unchanged.
Conditional iris glow and continuous animation remain separate gates.
Native pack-only loading and the
same iris scenario also pass; the larger pack and changed sampling/clip metadata
are recorded in the owning plan. Native packs are now the owner's selected 1.7
setup default; Unity remains the retained 1.6 pipeline and an explicit 1.7
comparison override. Broader visual/effect/audio acceptance remains open.
Public dependency restoration and CI also compile the experimental 1.7 project.

## Deferred work

| Work | Agreed boundary and next planning step |
| --- | --- |
| API documentation enrichment | Deferred. Inventory authored public/protected contracts, select useful extension points, and plan ownership, save/load and failure documentation with rendered API checks. No runtime change is implied. |

## Recently completed

| Work | Outcome | Owning record |
| --- | --- | --- |
| Development tooling hardening | Version-aware 1.6 builds, preserved version-owned bundles, agent workflows, isolated RimBridge validation and script consolidation. The historical preferences anomaly was closed by decision without a reproduced cause in [RG-Issue-001](issue-tracker.md#rg-issue-001). | [November 23, 2026 hardening record](done/2026-11-23-development-tooling-hardening.md) |
| DocFX migration | Wiki articles moved into Guides, compiled API reference and portable CI added, and hosted build/deploy verified for the implementation commit. | [November 23, 2026 DocFX record](done/2026-11-23-docfx-migration.md) |

## Plan shape and completion

Use only the sections the work needs:

1. **Outcome and boundaries:** Observable player/developer behavior, current
   baseline, prerequisites and explicitly excluded work.
2. **Owners and contracts:** Affected defs, C#/Harmony hooks, UI/localization,
   saved keys, object ownership and optional integrations.
3. **Implementation and decisions:** The smallest complete slice first, then
   phases where useful. Keep decisions and results in this plan.
4. **Acceptance and evidence:** Selected game/build and dependency context,
   relevant compile/XML checks, actual affected gameplay, interruption and
   save/load continuation. Record missing checks and link related issue or
   performance records. Documentation-only work needs documentation checks.
5. **Disposition:** What was implemented and verified, remaining limitations,
   linked deferred concerns and the condition for revisiting a decision.

Completion normally ends at implementation, hardening, relevant verification and
documentation. Publication is a gate when it is the requested outcome, as with
the hosted documentation migration; other features do not acquire a Workshop
release or downstream adoption requirement implicitly. For changes spanning
repositories, record the tested source graph and remind maintainers to update
the owning CI reference pins after the dependency changes land.

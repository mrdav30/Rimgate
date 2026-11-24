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

No active feature implementation plans are currently recorded.

## Deferred work

| Work | Agreed boundary and next planning step |
| --- | --- |
| API documentation enrichment | Deferred. Inventory authored public/protected contracts, select useful extension points, and plan ownership, save/load and failure documentation with rendered API checks. No runtime change is implied. |
| RimWorld 1.7 migration | Deferred and separate from 1.6 hardening. Verify the actual target API, asset pipeline and dependencies before planning the port and the 1.6 freeze. No 1.7 support is currently configured. |

## Recently completed

| Work | Outcome | Owning record |
| --- | --- | --- |
| Development tooling hardening | Version-aware 1.6 builds, preserved version-owned bundles, agent workflows, isolated RimBridge validation and script consolidation. The preferences anomaly remains a deferred investigation in [RG-Issue-001](issue-tracker.md#rg-issue-001). | [November 23, 2026 hardening record](done/2026-11-23-development-tooling-hardening.md) |
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

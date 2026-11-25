# Issue Tracker

## Purpose and intake

Track Rimgate bugs, correctness risks and defect investigations, including
gameplay, persistence, compatibility, documentation and tooling. Feature scope
belongs in the [overview and owning plans](feature-work-overview.md); measured
performance signals belong in the [performance backlog](performance-signal-backlog.md).

- IDs use `RG-Issue-NNN`. The next available ID is **RG-Issue-013**. Assign at
  intake, never reuse an ID, and check this file's history before repairing the
  counter. Keep an explicit `rg-issue-nnn` anchor through closure or compaction.
- Record discovery date/revision, affected owners, impact, status/priority,
  expected and observed behavior, reproduction and the next useful action.
  Label concerns needing reproduction separately from confirmed defects.
- For game cases, include the RimWorld build, platform, DLC, dependency versions
  and load order, settings, and save/fresh-colony setup. Check progression,
  power/fuel, access and pathing prerequisites before attributing a defect.
- Preserve enough tracked reproduction and evidence to understand and reopen a
  concern without temporary logs surviving. Use minimal reproducible scenarios
  where possible; identify any external/private save dependency and its limits.
  Keep original saves, private logs and bridge tokens outside version control.
- Link an existing GitHub report, feature plan or performance record rather than
  duplicating its discussion or progress. The owning record holds the technical
  status, decisions and evidence. A feature does not receive a bug ID.
- After verification or a documented closure decision, replace the active entry
  with a compact closed row: same ID/anchor, date, disposition, evidence link and
  reopening condition. Keep detailed evidence in the existing plan/completion
  record, regression or fix commit. Preserve prior findings when reopening.
- Distinguish **verified fix**, **mitigated**, **not reproduced**, and **closed by
  decision**. Closure does not establish an unobserved cause, fresh validation or
  a mod release. Verify the actual affected behavior and save/load path when
  relevant; compilation or a generic smoke check alone is insufficient.

## Active and deferred investigations

No active investigations.

## Closed issues

| Issue | Closed | Disposition and evidence | Reopening condition |
| --- | --- | --- | --- |
| <a id="rg-issue-001"></a>RG-Issue-001 - Normal player preferences changed during initial isolated launch | November 24, 2026 | **Closed by decision; historical cause not reproduced.** Different hashes established a byte change, but the original preferences bytes and deterministic trigger are unavailable. Later byte-backed checks passed; they do not explain that first change. [Original observation and closure](done/2026-11-23-development-tooling-hardening.md#live-rimbridge-adoption-verification). | A new mismatch with retained before/after bytes, verified profile/arguments and game/Steam/bridge process context. Compare actual XML differences before choosing a bounded reproduction. |
| <a id="rg-issue-002"></a>RG-Issue-002 - Hidden Waylith input/capture timeouts | November 24, 2026 | **Verified private-profile workflow correction after reopening.** The 1.7 companion initializes during SDK discovery and requests public redraws when occlusion suppresses GUI/capture frames. Controlled negative failure, repeated occluded UI/capture, save/reload, exact ticks and cold startup pass. [Original acceptance](rimworld-1.7-migration-plan.md#november-24-background-input-and-capture-acceptance), [recurrence](rimworld-1.7-migration-plan.md#discovery-and-presentation-results), [correction and limits](rimworld-1.7-migration-plan.md#rg-issue-002-automatic-hidden-redraw-correction). Earlier natural OS occlusion reasons remain unrecorded; 1.6 unchanged. | UI/capture fails with the hook installed on the recorded hidden graphics profile and a valid nonzero surface. Retain render predicate, pending requests, actual window/session/display state, runtime/pins and operation payloads. |
| <a id="rg-issue-003"></a>RG-Issue-003 - Gate addresses stay bound to a previous world | November 24, 2026 | **Verified 1.7 correction.** Resolve the current world component instead of a static cached instance. Same-process reload/registration plus queued and active saved address books pass. [Reproduction and acceptance](rimworld-1.7-migration-plan.md#complete-operation-result-and-persistence-corrections). Retained 1.6 is unchanged. | Current-world identity or registered-address persistence fails after loading another world/save. |
| <a id="rg-issue-004"></a>RG-Issue-004 - Saving a gate-buffered pawn loses it and its inventory | November 24, 2026 | **Verified 1.7 correction.** Deep-owned pending contents and phase-correct receive/redraft reconstruction preserve exact pawn/item identities through both in-flight checkpoints and resumed arrival. Pending cancellation and normal closure pass. [Failed save, correction, acceptance and limits](rimworld-1.7-migration-plan.md#complete-operation-result-and-persistence-corrections). Already-lost matter is not recoverable from missing definitions; 1.6 unchanged. | Loss, duplication, stranded queue ownership or redraft failure recurs across admission, save/load, arrival or closure; broaden with a retained failing checkpoint. |
| <a id="rg-issue-005"></a>RG-Issue-005 - Gate overlays disappear at close zoom | November 24, 2026 | **Verified 1.7 correction.** Reuse incoming draw altitude, match overlay/base pitch and place chevrons above the iris. Same-save zoom control and 25 native presentation states pass. [Reproduction, correction and limits](rimworld-1.7-migration-plan.md#native-presentation-acceptance-and-rg-issue-005). Retained 1.6 and authored assets unchanged. | Horizon, iris or chevrons disappear, intersect or misalign at normal zoom/rotation. Retain paused state, camera depth/pitch report, pixels, runtime and exact pack. Special iris glow remains a separate gate. |
| <a id="rg-issue-006"></a>RG-Issue-006 - Gate condition reload loses its owner and strands a hold | November 24, 2026 | **Verified 1.7 correction.** Scribe the held-gate reference; narrowly recover an unambiguous missing-key local hold without incrementing it. New saves, retained old-key input, ownership-policy countercases, remote nonowner and normal end pass. [Phase-2 lifecycle evidence](rimworld-1.7-migration-plan.md#phase-2-completion). | A condition loses its gate, leaks/doubles a hold or claims another connection across save/load/end/resave. Already-removed conditions and ambiguous legacy ownership require separate recovery evidence. |
| <a id="rg-issue-007"></a>RG-Issue-007 - Conditional iris glow intersects the iris | November 24, 2026 | **Verified 1.7 correction.** Match the manual glow mesh to the iris's pitch and lift it above the iris, below chevrons. All four rotations pass before/condition/reload/end visual checks. [Reproduction and acceptance](rimworld-1.7-migration-plan.md#phase-2-completion). | A real gate condition produces a clipped, misaligned or lingering glow. Retain condition state, rotation, zoom, native pack and captures. |
| <a id="rg-issue-008"></a>RG-Issue-008 - Bring selected rejects ordinary loose cargo | November 25, 2026 | **Verified 1.7 correction.** Restrict the Claimable guard to the mobile-container branch; reject ordinary buildings/plants/fires. The same retained unique autopistol now starts the real haul job and arrives on the receiving map with one object/owner. [Phase-3 evidence](rimworld-1.7-migration-plan.md#phase-3-gameplay-and-current-save-acceptance). 1.6 unchanged. | Real Bring selected targeting fails for eligible loose cargo or admits an unsupported target. Retain item ID, actual job/target, before-save and menu. |
| <a id="rg-issue-009"></a>RG-Issue-009 - Sarcophagus reload loses pending treatment | November 25, 2026 | **Verified 1.7 correction.** Preserve ordered patient-owned hediff references and trait def/degree keys. Fresh healing reload and real completion pass, including VTE Lush trait references and removal. [Reproduction and acceptance](rimworld-1.7-migration-plan.md#phase-3-gameplay-and-current-save-acceptance). No re-diagnosis or timer reset; historical missing-queue repair excluded; 1.6 unchanged. | Treatment order/targets disappear, duplicate or change owner across a current-save checkpoint, or resumed treatment fails. |
| <a id="rg-issue-010"></a>RG-Issue-010 - Moving cart saves retain an unsaved original target | November 25, 2026 | **Verified 1.7 correction.** Release the original reservation, retarget the job to the deep-owned proxy and avoid premature map access during toil reconstruction. Fresh loaded pushing preserves proxy/cargo/job/visual across reload, reaches the exact chosen cell, unloads, and restores the cart cleanly on draft interruption. [Retained failure and replay](rimworld-1.7-migration-plan.md#phase-3-gameplay-and-current-save-acceptance). Old broken checkpoints lack the original target; historical repair excluded; 1.6 unchanged. | A loaded push loses its job, cargo, visual or reservation integrity across a current-save checkpoint, or leaves duplicate contents/slowdown after cleanup. |
| <a id="rg-issue-011"></a>RG-Issue-011 - Layouts contain stale or malformed symbols | November 25, 2026 | **Verified shared XML correction.** With owner approval, correct 34 cells in 27 authored rows across four layout files reused by 1.6 and private 1.7, preserving row widths; the initial 1.7 patch was removed. Native resolved-symbol report changes from 34 bad cells to zero across 44 layouts. [Evidence and inferred SmokeleafJoint intent](rimworld-1.7-migration-plan.md#phase-3-gameplay-and-current-save-acceptance). This is definition resolution, not acceptance of every rendered structure or live 1.6 behavior. | A layout has missing/wrong symbols or an authored row regresses after source updates. Retain exact layout/layer/row and loaded resolved definitions. |
| <a id="rg-issue-012"></a>RG-Issue-012 - DBH thirst-disabled treatment throws | November 25, 2026 | **Verified 1.7 correction.** Null-check the optional thirst need, matching other DBH setters. DBH 3.2.3260 with Bladder/Hygiene and no Thirst now treats, reloads and discharges the real patient successfully. [Before log and replay](rimworld-1.7-migration-plan.md#phase-3-gameplay-and-current-save-acceptance). This does not establish all DBH variants; 1.6 unchanged. | An optional need's absence breaks a real treatment/restore path, or a present thirst need is restored incorrectly. |

## Intake template

```markdown
<a id="rg-issue-nnn"></a>

### RG-Issue-NNN - Concise observable problem

- **Status / priority:** Needs reproduction | Confirmed | Planned | Deferred; P1/P2/P3
- **Discovered / revision:** Date and source revision; link an external report if present
- **Owners / impact:** Affected feature, source/defs and observable consequence
- **Context:** Game/build, platform, DLC/mod versions/load order, settings, save or fresh setup
- **Reproduction / expected / observed:** Minimal steps, controls and important output
- **Evidence / limits:** Durable observations, unavailable inputs and uncertainty
- **Next action:** Smallest useful investigation/fix and relevant acceptance checks
```

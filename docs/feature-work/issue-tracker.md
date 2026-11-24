# Issue Tracker

## Purpose and intake

Track Rimgate bugs, correctness risks and defect investigations, including
gameplay, persistence, compatibility, documentation and tooling. Feature scope
belongs in the [overview and owning plans](feature-work-overview.md); measured
performance signals belong in the [performance backlog](performance-signal-backlog.md).

- IDs use `RG-Issue-NNN`. The next available ID is **RG-Issue-003**. Assign at
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

No active or deferred investigations.

## Closed issues

| Issue | Closed | Disposition and evidence | Reopening condition |
| --- | --- | --- | --- |
| <a id="rg-issue-001"></a>RG-Issue-001 - Normal player preferences changed during initial isolated launch | November 24, 2026 | **Closed by decision; historical cause not reproduced.** Different hashes established a byte change, but the original preferences bytes and deterministic trigger are unavailable. Later byte-backed checks passed; they do not explain that first change. [Original observation and closure](done/2026-11-23-development-tooling-hardening.md#live-rimbridge-adoption-verification). | A new mismatch with retained before/after bytes, verified profile/arguments and game/Steam/bridge process context. Compare actual XML differences before choosing a bounded reproduction. |
| <a id="rg-issue-002"></a>RG-Issue-002 - Waylith bridge synthetic selection and capture time out | November 24, 2026 | **Verified workflow fix.** Experimental setup now uses graphics-enabled `-NoWindow` with `-WindowSize=1280,720`. The unchanged smoke passed selection, iris mutation/save/reload, 120 exact ticks and screenshot capture with an empty error journal. [Reproduction, launch change and acceptance](rimworld-1.7-migration-plan.md#november-24-background-input-and-capture-acceptance). This does not attribute the original engine pause internally or establish full asset/transit compatibility. | Selection/capture fails again under the recorded hidden graphics profile; retain argv, build/dependencies, payloads and frame/render evidence. |

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

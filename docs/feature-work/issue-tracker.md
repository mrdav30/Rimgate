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

<a id="rg-issue-001"></a>

### RG-Issue-001 - Normal player preferences changed during initial isolated launch

- **Status / priority:** Deferred; needs reproduction / P2. A file change was
  observed; attribution to the game, bridge, Steam or Rimgate is unconfirmed.
- **Discovered / revision:** November 23, 2026 during the tooling-hardening work
  later committed as `dfdb384`. Original observations are retained in the
  [live adoption record](done/2026-11-23-development-tooling-hardening.md#live-rimbridge-adoption-verification).
- **Owners / impact:** Isolated development launch and normal-player config
  preservation. The normal `Config/Prefs.xml` hash changed at the first launch;
  normal `ModsConfig.xml` remained unchanged. The exact preference changes are
  unknown because the initial snapshot preserved its hash, not its bytes.
- **Historical context:** Windows, RimWorld 1.6.4871 rev591; GABS 1.1.2,
  RimBridgeServer package 2.5.0 with frozen 1.6 bridge/SDK 2.3.0.0. The ten-entry
  isolated mod list and private config/mod paths were verified. Exact original
  preferences and a deterministic trigger were not recovered.
- **Observed / expected:** With no intentional normal-profile edit, the normal
  preferences were expected to stay byte-identical. Before SHA-256:
  `cc3a5e0ba55006f07efe528d588dd800c8593e4ebff82dca02cfc2fe7c327832`;
  retained changed-file SHA-256:
  `7ecb8b582fda45c6388f4b8754423496f23e9a63aac7766b1ef460e189d3b4f4`.
  These hashes identify the observation; they do not reconstruct the old file.
- **Mitigation / evidence:** Setup now backs up both config files' actual bytes
  and hashes before preparing tools. The later stdio smoke run and subsequent
  script-layout smoke preserved both files and passed `-VerifyPlayerConfig`.
  This verified the guard and those runs; the original cause remains unknown.
- **Next action / revisit:** Investigate on recurrence or explicit
  prioritization. With normal RimWorld stopped, use the contributor guide's
  [isolated workflow](../guides/developer-and-contributor-notes.md#live-validation-with-rimbridge)
  to capture a byte-backed baseline, launch the game once, confirm live profile
  and process arguments, stop it, then verify config preservation. On a mismatch,
  retain the before/after XML, game/Steam/bridge context and process timeline;
  compare the fields before selecting a bounded control. Review differences
  before restoration so concurrent player edits are preserved. A passing replay
  does not resolve the original observation.

<a id="rg-issue-002"></a>

### RG-Issue-002 - Waylith bridge synthetic selection and capture time out

- **Status / priority:** Observed; cause unconfirmed / P2. Blocks acceptance of
  the experimental 1.7 gameplay smoke; no Rimgate runtime fix is inferred.
- **Discovered / revision:** November 24, 2026, migration work based on `6c0eab2`.
  Related [migration record](rimworld-1.7-migration-plan.md#november-24-runtime-bring-up-and-portable-17-builds).
- **Owners / context:** Windows; Steam `unstable`, RimWorld **1.7.5014 rev1195**,
  custom .NET **10.0.15-waylithclr.dev**, GABS **1.1.2**, bridge **2.5.0.0**,
  SDK **2.0.0.0**, 130 tools. Isolated `Profile-1.7`, `Game-1.7` private overlay;
  ten active entries: Harmony, Core, five DLC, VEF, Rimgate and RimBridge.
  No DBH runtime mod. Config/session match with zero dependency/version issues.
- **Reproduction / expected:** Run the guide's isolated setup and the guarded
  `.assets/scripts/rimbridge-smoke.lua` from the main menu. A fresh debug colony
  reaches visual readiness, spawns the Dwarfgate at `(110,130)` and DHD at
  `(110,134)`, and jumps the camera. `click_cell` should select the gate.
- **Observed:** The script halts at `Gate selection failed`, operation
  `op_a6aff743a4a0490d82c2877cb1b81156`. A direct retry times out after 2,000 ms,
  with empty selection before/after. Read-only cell inspection confirms a live
  `Rimgate.Building_Gate` at the target; UI state has no blocking windows or world
  view. A separate `take_screenshot` times out after approximately ten seconds
  without writing the expected PNG. The bridge reports completed operations with
  failed gameplay payloads. Its error journal is empty.
  Bridge status reports essential, 58 optional and six late-input hooks applied
  without failures; that report does not establish that queued input was processed.
- **Evidence / limits:** Clean restart log has zero ERROR headers; native Vulkan
  layer warnings are present. Private log copy:
  `%LOCALAPPDATA%/RimgateDev/evidence/1.7/startup-and-smoke.log`. Save/load, exact
  ticks, gate transit and rendered-asset checks were not reached. Root cause may
  involve frame/foreground handling or bridge/Waylith compatibility; neither was
  established. Computer Use was stopped with Escape before the window-activation
  control could run. The private game was stopped; all 125 normal config files
  matched their byte-backed pre-launch hashes.
- **Next action:** With permission to resume UI work, compare the same bridge
  click/capture with the native game window active and occluded, recording frame
  progress and original payloads. If foreground does not distinguish it, inspect
  the exact released bridge's input/capture hooks against this runtime. Keep the
  investigation bounded; do not change Rimgate gameplay or weaken smoke assertions
  merely to obtain a passing automation result.

## Closed issues

No closed issue entries are recorded yet. The completed hardening and DocFX
records describe delivered work, without retroactively assigning bug IDs to
features or implying every historical finding was fixed.

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

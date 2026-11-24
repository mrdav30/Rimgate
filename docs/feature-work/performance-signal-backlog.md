# Performance Signal Backlog

## Purpose and intake

Track measured timing, allocation, scaling and measurement-reliability concerns
in Rimgate's real workloads. Evidence may come from in-game profiling, a focused
benchmark or a repeated validation run. This ledger does not require introducing
a benchmark suite. Feature design belongs in the [overview and plans](feature-work-overview.md);
correctness defects and unverified defect reports belong in the [issue tracker](issue-tracker.md).

- IDs use `RG-Perf-NNN`. The next available ID is **RG-Perf-001**. Assign at
  intake, never reuse an ID, and keep an explicit `rg-perf-nnn` anchor through
  closure or compaction. Check history before repairing the counter.
- Open a signal from actual measurement or an observed measurement failure.
  Suspected hot paths, missing coverage and a successful smoke case alone do not
  establish a measured regression. The 120-tick RimBridge case proves functional
  progress; timing claims need a controlled workload and additional measurement.
- Record source/dependency revisions, RimWorld build, platform/runtime, hardware,
  DLC/mod versions/load order, settings, profiler/harness version, scenario/save
  setup and complete commands or Lua steps. State pawn/map/feature counts,
  simulation speed and whether startup, warmup, rendering, transitions or
  save/load are included in the measured interval.
- Compare equivalent work and results. Record baseline/current values with
  units, repetitions and variability: for example ms/tick, ticks/s, transition
  duration or measured allocations. Separate cold and warm observations and
  simulation cost from bridge, rendering or profiler overhead. Do not infer a
  simulation improvement from reduced test-harness wall time alone.
- Check expected operation counts, returned gameplay payloads, exit status and
  logs before accepting a run. Exclude incomplete or failed measurements from
  comparisons. Preserve failed-run observations as reliability evidence without
  treating them as valid timing samples or attributing an unproven cause.
- Keep inputs/replay recipe, baseline, correctness controls, important results,
  conclusion, limits and next experiment in tracked text. Captures are supporting
  evidence; record any external fixture dependency explicitly. Keep private
  player data, credentials and bridge tokens outside version control.
- Start with the smallest representative experiment. Link substantial
  implementation to a feature plan and any verified functional defect to its
  issue. Keep each concern's status and evidence in its owning record.
- Close after a verified improvement, accepted measured tradeoff or documented
  no-change decision. Keep a compact closed row with ID/anchor, date, outcome,
  evidence link and reopening condition. Preserve the agreed behavior and
  performance baseline; only the user can accept a measured regression.

## Active signals

No measured performance signals are recorded yet. This states the ledger's
contents; no project-wide performance audit or zero-overhead claim is implied.

## Closed signals

None recorded.

## Entry template

```markdown
<a id="rg-perf-nnn"></a>

### RG-Perf-NNN - Concise measured workload or reliability concern

- **Discovered / status:** Date; investigating, planned, deferred or experimental
- **Owner:** Affected runtime source and scenario/profiler/harness
- **Revisions / context:** Source/dependency revisions, game/build, platform/runtime, hardware, mod list/settings
- **Workload / replay:** Save or setup recipe, counts, speed, timed lifecycle, commands/Lua and warmup
- **Measurement:** Baseline/current values, units, repetitions/variability and completed operation counts
- **Correctness controls:** Equivalent results and independent affected-behavior checks
- **Interpretation / limits:** Established result, overhead, missing inputs and uncertainty
- **Next experiment:** Smallest measurement that distinguishes the remaining causes
- **Closure / revisit:** Disposition, evidence link and reopening condition
```

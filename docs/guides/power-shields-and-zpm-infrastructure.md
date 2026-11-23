# Power, Shields, and ZPM Infrastructure

This guide covers Rimgate's high-impact infrastructure loop: how to power your base, when to transition into ZPMs, and how to keep that power online without turning your colony into a raid beacon.

For broader progression context, see [Research and Progression](research-and-progression.md).  
For the raid pressure tied to active ZPM signals, see [Incidents, Quests, and Sites](incidents-quests-and-sites.md).

## At a Glance

- Early-mid power options are fuel generators (`naquadria` or `biomass`) with strong output but fuel logistics pressure.
- `zero point modules` (ZPMs) are endgame-scale batteries but start an ongoing threat loop when broadcasting.
- `subspace shroud emitter` is your key risk-control tool for active ZPM grids.
- `subspace phase diverter` is your long-run sustain tool for ZPM reserve recovery.
- `ancient shield emitter` is your top-tier defensive layer, but power demand scales hard with radius.
- Best results come from staged rollout: power stability first, then ZPM integration, then shield scaling.

## Unlock Path (Recommended)

1. Stabilize baseline grid:
   - `naquadria power` -> `naquadria generator`
   - and/or `biomass generator` -> `advanced bioengineering` for boosted biomass output
2. Analyze a `zero point module` and complete `ZPM core` (`Rimgate_ZPMIntegration`).
3. Add safety + sustain:
   - `subspace masking` -> `subspace shroud emitter`
   - `subspace coupling` -> `subspace phase diverter`
4. Scale up:
   - `ZPM housing` -> multi-ZPM containment
   - `ancient shield technology` -> `ancient shield emitter`

## Generator Comparison

| Generator | Output | Fuel | Practical Use |
| --- | --- | --- | --- |
| `naquadria generator` | `4600W` | `naquadria` | Compact high-output core for mid-game grids; volatile if hit. |
| `industrial naquadria generator` | `9600W` | `naquadria` | Megabase anchor with high heat/explosion risk; place behind layered protection. |
| `biomass generator` | `3000W` (`+20%` with `advanced bioengineering`) | `biomass` | Strong alternative path if you are already in Wraith bio loops. |

## ZPM Lifecycle

### 1) Acquire and analyze

- A ZPM can be analyzed to unlock `ZPM core`.
- Analysis has catastrophic failure risk, so use a high-Intellectual colonist.
- Before `ZPM core`, player-owned ZPMs are effectively inert when connected.

### 2) Activate and broadcast

- After `ZPM core`, a connected player ZPM starts broadcasting.
- Broadcasting ZPMs can trigger the `Protect the ZPM` raid loop.
- If the signal stops (disconnect/destroy/uninstall/suppress), the loop ends.

### 3) Scale with ZPM Housing

- `ZPM containment housing` holds up to 3 ZPMs.
- Effective storage scales with loaded modules:
  - 1 ZPM: `48,000`
  - 2 ZPM: `96,000`
  - 3 ZPM: `144,000`
- At 3 ZPMs, housing gets a recharge-rate bonus for dark-energy recovery.
- You can insert/eject modules through pawn context actions (`insert into housing`, `eject one/all`).

### 4) Mask and sustain

- `subspace shroud emitter`:
  - Map-wide suppression while powered and properly linked.
  - Must be linked to at least one placed ZPM to activate suppression.
  - Cheap power draw (`200W`) and should be treated as mandatory once ZPMs are online.
- `subspace phase diverter`:
  - Enables gradual dark-energy recovery for linked ZPM infrastructure.
  - Requires `subspace coupling` research and power (`200W`).
  - One powered, valid link is enough to enable recharge; add extras mainly for layout flexibility/redundancy.

## Ancient Shield Emitter

`ancient shield emitter` is your endgame umbrella defense.

What it does:

- Intercepts hostile incoming projectiles (ground + overhead).
- Can block hostile skyfallers/drop pods entering shield radius.
- Handles large burst pressure but builds stress as it absorbs hits.

Operational notes:

- Radius is player-adjustable (`5` to `99` tiles).
- Power usage scales with radius (`4500W` to `29000W`).
- Overload triggers a cooldown window (base `3000` ticks) where protection is offline.
- EMP and sustained focus fire can force overload faster.

## Tok'ra Climate Infrastructure (Adjacent Utility)

`climate crystal` is not a power producer, but it is strong base infrastructure for harsh biomes:

- Runs on `naquadah` fuel (not electricity).
- Handles both heating and cooling using a target temperature slider.
- Fuel use scales with actual temperature work performed.
- Great for stabilizing key rooms when your electrical grid is under combat load.

## Important Grid Behavior

- ZPM energy is handled differently from normal batteries.
- Standalone `ZPM` batteries are excluded from vanilla-style battery equalization and short-circuit battery drain behavior.
- `ZPM containment housing` does not share the same short-circuit exemption, so housing placement/protection still matters.
- In practice: standalone ZPM reserve is harder for random short-circuit events to wipe, but you still need protection from direct combat damage and raid pressure.

## Setup Patterns That Work

### Pattern A: Safe first ZPM

1. Build baseline fuel power (naquadria/biomass) with redundancy.
2. Bring one ZPM online.
3. Immediately add powered `subspace shroud emitter`.
4. Add one `subspace phase diverter` for slow reserve sustain.
5. Keep shield radius small until the grid has real headroom.

### Pattern B: Fortress transition

1. Build `ZPM containment housing`.
2. Load 2-3 ZPMs and keep masking active.
3. Deploy `ancient shield emitter` near base center.
4. Tune radius to cover only critical footprint first (reactors, hospital, gate room, stockpiles).
5. Expand radius only after confirming stable surplus under raid conditions.

## Common Mistakes

- Activating ZPM power before preparing masking/defenses.
- Running shield radius too large for current grid, causing unstable uptime.
- Treating one giant generator as "enough" without backup power.
- Ignoring heat/explosion placement risks on naquadria infrastructure.
- Forgetting that suppression requires an active, valid shroud link state.

## Strategy Checklist

- Keep at least two independent power lanes before scaling shields.
- Pair every active ZPM plan with immediate shroud coverage.
- Add diverters as sustain tools, not emergency fixes.
- Keep shield radius only as large as your defended footprint truly needs.
- Stage expansion: generator backbone -> ZPM core -> masking/coupling -> housing -> shield scaling.

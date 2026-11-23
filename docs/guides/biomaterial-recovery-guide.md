# Biomaterial Recovery Guide

This guide explains the corpse biomaterial recovery workflow in Rimgate: what you need, how to start the job, what controls success, and what you actually gain.

For progression context, see [Research and Progression](research-and-progression.md).
For item-level references, see [Items and Resources](items-and-resources.md).

## At a Glance

- Biomaterial recovery is a right-click corpse action, not a bench bill.
- You need a `biomaterial procurement kit` for every attempt.
- The kit is consumed on attempt (success or failure).
- Success chance is `base chance x surgery success stat`.
- Recovery options are grouped by the research that unlocks them.
- Each recovery def can target multiple implants; the job uses the first matching one on the corpse.

## Unlock Path

1. Progress Tau'ri research to `Tau'ri medical procedures`.
2. Complete `biomaterial recovery`.
3. Build/use `Drug Lab` or `Fabrication Bench` for kit crafting.
4. Unlock specific recovery categories via their own research (table below).
5. For archotech-tier extraction, complete `advanced biomaterial recovery` and its prerequisites.
6. Acquire a valid corpse that contains at least one targeted implant for the selected recovery option.

## Crafting the Recovery Kit

`biomaterial procurement kit` now has two recipe variants:

| Recipe | Medicine Input | Other Inputs | Skills | Work | Output |
| --- | --- | --- | --- | --- | --- |
| `assemble bio-procurement kit (medicine)` | `4` industrial medicine | `5 steel`, `5 cloth`, `5 chemfuel` | `Crafting 5`, `Medicine 6` | `2400` | `1 kit` |
| `assemble bio-procurement kit (glitter)` | `1` ultratech medicine | `5 steel`, `5 cloth`, `5 chemfuel` | `Crafting 4`, `Medicine 4` | `1800` | `1 kit` |

Both recipes currently require `biomaterial recovery` research and can be crafted at `Drug Lab` or `Fabrication Bench`.

## How to Start Recovery In-Game

1. Select a colonist.
2. Right-click a corpse.
3. Choose a recovery option if available (for example `recover cortical stack` or `recover bionic implant`).

The float-menu option appears only when the corpse is valid and has at least one implant listed in that recovery def.

## Eligibility Rules

A recovery option is enabled only if all checks pass:

- Corpse is valid (not destroyed, not forbidden, has an inner pawn).
- Corpse has at least one targeted implant from that recovery def’s `removesHediffs` list.
- Required research for that recovery def is complete.
- The selected pawn meets skill requirements defined on the recovery def.
- At least one reachable/reservable procurement kit is available.
- Rotten corpses are rejected.

## What Happens During the Job

1. Pawn reserves the corpse.
2. If not already carrying a kit, pawn finds and picks up the nearest valid kit.
3. Pawn goes to the corpse and performs timed recovery work.
4. On finish, one kit is consumed and success is rolled.
5. On success:
   - The first matching implant from that def’s list is selected.
   - Output item for that matched implant is spawned (or `spawnThingOverride` if configured).
   - The matched implant is removed from the corpse’s inner pawn.
6. On failure:
   - No output item.
   - Corpse implants remain unchanged.
   - You still lose the kit.

## Work Time and Success Chance

Work duration uses:

- `workAmount / workSpeedStat`
- Current base defs use:
  - `workAmount = 2000`
  - `workSpeedStat = MedicalOperationSpeed`
  - baseline skill requirements: `Medicine 5`, `Intellectual 5`

Success roll uses:

- `successChance x actor.GetStatValue(successChanceStat)`

Practical takeaway: higher surgery success improves expected output per kit.

## Recovery Categories

Current definitions are grouped by unlock research:

| Recovery Option | Target Scope | Research Gate |
| --- | --- | --- |
| `recover cortical stack` | Unas cortical implants | `Unas biology` |
| `recover prosthetic` | Basic prosthetics/cochlear set | `prosthetics` |
| `recover bionic implant` | Core bionics set (eyes, limbs, spine, organs, jaw, etc.) | `bionics` |
| `recover specialized limb implant` | Drill arm / field hand / power claw | `specialized limbs` |
| `recover compact weaponry implant` | Talon / elbow blade / knee spike | `compact weaponry` |
| `recover venom synthesis implant` | Venom fangs / venom talon | `venom synthesis` |
| `recover detox implant` | Detoxifier lung / kidney | `tox filtration` |
| `recover flesh shaping implant` | Aesthetic / social-body mods | `flesh shaping` |
| `recover artificial metabolism implant` | Stomach metabolism mods | `artificial metabolism` |
| `recover circadian influence implant` | Circadian assistant / half-cycler | `circadian influence` |
| `recover neural implant` | Neurocalculator / learning assistant | `neural computation` |
| `recover molecular implant` | Gastro analyzer / immunoenhancer | `molecular analysis` |
| `recover healing implant` | Coagulator / healing enhancer | `healing factors` |
| `recover skin implant` | Tough/armor/stone/vac skin glands | `skin hardening` |
| `recover brain wiring implant` | Mindscrew / joywire / painstopper | `brain wiring` |
| `recover archotech implant` | Archotech eyes/arms/legs | `advanced biomaterial recovery` |
| `recover ghoul implant` | Ghoul enhancement implant set | `ghoul enhancements` |

## Common Failure Cases

- No kit on-map or kit is forbidden/unreachable.
- Corpse is rotten for defs that disallow rotten recovery.
- Pawn fails skill checks.
- Corpse does not contain any targeted implant for the selected recovery option.
- Research prerequisite is not complete.

## Strategy Checklist

- Keep kits near kill-processing/storage areas to reduce travel overhead.
- Assign a dedicated high-medical operator for better success efficiency.
- Prioritize fresh corpse processing before rot risk blocks the option.
- Keep at least a small emergency buffer of kits (`3-6`) for opportunistic recoveries.

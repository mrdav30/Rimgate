# Factions and Pawn Types

This page is a player-facing breakdown of who you will meet in Rimgate, what kinds of pawns they field, and what to expect from each faction in combat and world encounters.

## Faction Overview

| Faction | Tech Tier | Typical Behavior | Core Pawn Types |
| --- | --- | --- | --- |
| Tau'ri | Spacer | Armed expeditions, settlements, traders, orbital teams | SGC Soldiers, Medics, Riflemen, Special Forces, Commanders, MALP support |
| Goa'uld (Ba'al) | Ultra | Heavy Jaffa military presence, staged attacks, elite guards | Jaffa Warriors, Guards, First Primes, Kull Warriors, Under Lords |
| Wraith Empire | Ultra | Aggressive strike groups, no siege focus, strong elites | Soldiers, Assassins, Commanders, Queens, Tracking Drones |
| Replicator Hive | Ultra | Hidden, permanent enemy, swarm-oriented mech pressure | Replicator variants (base/material), Humanoid Replicators |
| Treasure Hunters (quest faction) | Ultra | Quest/encounter combatants, not normal world-raiders | Treasure Hunters, Treasure Hunter Primus |

Notes:

- Orbital pawn groups (for example Tau'ri Guardians and Wraith orbit variants) depend on Odyssey-enabled orbital content.
- Replicator raids are configured as a later threat (earliest around day 45).

## Playable Faction Starts

Rimgate includes three themed player-faction scenarios:

- `Free Jaffa Nation`:
  - Player faction: `Rimgate_GoauldPlayerColony`
  - Default starting roster: 3 Free Jaffa + 2 slaves
- `Rogue SGC Team`:
  - Player faction: `Rimgate_TauriPlayerColony`
  - Default starting roster: 4 SGC members
- `Wraith Co-Op`:
  - Player faction: `Rimgate_WraithPlayerColony`
  - Default starting roster: 2 Wraith colonists + 3 Wraith slaves

You can still customize pawns at scenario start; these are the default compositions.

## Tau'ri Pawn Types

Role identity:

- Gunline + tactical specialists with occasional mech support.

Common units:

- `soldier` (combat power ~100): baseline infantry.
- `medic` (~90): armed support with better medicine focus.
- `demolition` (~130): explosive/launcher loadouts.
- `rifleman` (~130): stronger ranged profile.
- `operator` / Special Forces (~150): higher-end all-rounders.
- `guardian` (~150): orbital/vacuum-ready combat variant.
- `commander` (~150): faction leader-class unit.
- `scientist` (~20) and `trader` (~40): non-frontline/support types.

Special support:

- `MALP` units can appear with Tau'ri groups.
- Captured/repurposed `scyther` support can also appear in Tau'ri combat groups.

## Goa'uld Pawn Types

Role identity:

- Hierarchical military caste with escalating elite tiers and strong themed armor sets.

Common units:

- `Anubis/Apophis/Ra Jaffa warrior` (~100): core line fighters.
- `Anubis/Apophis/Ra guard` (~130): heavier guard tier.
- `First Prime` variants (~150): elite guard leadership tier.
- `Kull warrior` (~150): engineered supersoldier class.
- `underlord` (leader class): high-end command pawn.

Non-combat/support classes:

- `colonist` (~25), `slave` (~22), `slave driver` (~55), `trader` (~40).

Important gameplay note:

- Many Goa'uld military pawns are tied to symbiote biology (Jaffa pouch/Prim'ta systems).

## Wraith Pawn Types

Role identity:

- Fast pressure, elite melee/ranged hybrids, telepathic/high-end command units.

Common units:

- `soldier` (~100): core military unit.
- `assassin` (~170): elite threat with stronger close-combat pressure.
- `commander` (~150): advanced command combatant.
- `queen` (~170): top-tier leader presence.
- `orbit soldier` / `orbit commander`: vacuum/space-oriented variants.

Non-combat/support classes:

- `colonist`, `slave`, `trader`.

Special support:

- `tracking drone` (~120) support units appear in Wraith groups.

## Replicator Pawn Types

Role identity:

- Mechanoid swarm pressure with material-tier variants and one very dangerous humanoid tier.

Core variants:

- `Replicator` (~90)
- `Replicator (plasteel)` (~110)
- `Replicator (uranium)` (~110)
- `Replicator (naquadah)` (~125)
- `Replicator (humanoid)` (~210)

Threat profile:

- Early-mid waves are usually base/material replicators.
- Humanoid replicators are much higher impact and should be treated as priority targets.
- Expect no normal diplomatic safety net: this faction is configured as a permanent enemy.

## Other Notable Pawn Types

- `Unas` (~220): powerful reptilian combat fauna with special roar behavior; can appear wild and can be a major early-mid map threat.
- `berserk Unas`: permanently violent variant.
- `cranky brown bear`: custom permanent manhunter bear variant.
- `treasure hunter` (~140) and `treasure hunter primus` (~260): encounter/quest combatants, not standard faction raiders.

## Recruitment and Capture Notes

- Several faction leader-tier pawns are flagged as non-standard recruits (for example some commander/leader archetypes carry unrecruitable behavior).
- Elite classes are often more practical to defeat than to plan around recruiting.
- Replicator classes are mechanoid-based and should be treated as mech threats, not normal prisoner-conversion targets.

## Quick Threat Priorities

- Against Tau'ri: neutralize demolition and high-skill rifle units first.
- Against Goa'uld: focus Kull/Prime units before line Jaffa if you can isolate them.
- Against Wraith: expect high-pressure elites; avoid letting assassins close distance freely.
- Against Replicators: control swarm density early and spike down humanoid variants immediately.

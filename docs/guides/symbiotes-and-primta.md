# Symbiotes and Prim'ta

This guide covers the player-facing Goa'uld biotech loop: how to create and sustain hosts, run symbiote production, handle Prim'ta maturation safely, and avoid colony-ending failures.

For faction context, see [Factions and Pawn Types](factions-and-pawn-types.md).  
For related research gates, see [Research and Progression](research-and-progression.md).

## At a Glance

- The full loop is: `symbiote pouch` host -> `Prim'ta` growth -> timed intervention -> mature symbiote cycle.
- `symbiote spawning pool` is your production core and safe storage hub.
- Mature Prim'ta is a hard timer, not flavor text: if ignored, it can kill the host.
- Removing symbiotes without replacement causes `symbiote withdrawal` unless Tretonin support is active.
- Tretonin can replace symbiote dependence, but creates its own permanent dependency.
- Queen lineage and symbiote memory can turn this into a long-term power engine if managed carefully.

## Unlock Path (Recommended)

1. Obtain at least one `mature symbiote` and analyze it.
2. Complete `symbiote basics` (`Rimgate_SymbioteStudy`).
3. Build a `symbiote spawning pool` and insert a `symbiote queen`.
4. Keep feed stocked, then begin controlled Prim'ta host rotation.
5. Add Tretonin production as emergency backup before scaling host count.

## Getting a Valid Host

A pawn needs a `symbiote pouch` to carry Prim'ta.

Common ways to get one:

- Start with Jaffa/Tok'ra pawn types that already have pouch/symbiote setup.
- Use genes/xenotypes that include `Rimgate_SymbiotePouchMutation`.
- Use a `primogenitor sphere` to grant the pouch mutation.

Hard host limits:

- Prim'ta refuses hosts who already have a mature symbiote.
- Prim'ta requires a pouch.
- Host age limit is `110` biological years.

## Host Benefit Snapshot

| State | Major Benefits | Major Risk |
| --- | --- | --- |
| `Prim'ta` | Higher healing/immunity, much lower hunger pressure, reduced rest pressure | Matures on a timer and becomes dangerous if overstayed |
| `mature symbiote` | Strong healing/immunity and body-capacity boosts, lower hunger/rest pressure, periodic chronic-heal effect | Removal can trigger severe withdrawal without replacement support |

## Prim'ta Lifecycle Timeline

### Growth phase

- Prim'ta maturation is randomized to about `75-150` in-game days.
- Around `85%` maturity, host gets `Krin'tak fever` warning signs.
- Prim'ta gives strong early buffs while growing (better healing/immunity, lower hunger/rest pressure).

### Mature phase

- Once mature, you get a warning and a short grace period of about `15-20` in-game days.
- After grace, overstay outcome is checked daily:
  - `35%` chance: Prim'ta takes over and becomes a mature symbiote implant.
  - `65%` chance: host and Prim'ta die.

Practical rule:

- Do not leave mature Prim'ta unattended. Plan replacement/extraction before the grace window ends.

## Spawning Pool Operations

`symbiote spawning pool` is your production backbone.

Core behavior:

- Holds queen + brood items (`Prim'ta`, mature symbiote, queen).
- Max held contents: `15`.
- With a queen and feed, produces one Prim'ta about every `3` in-game days.
- Production cost: `2` feed per produced Prim'ta.
- Feed type is `Kibble` (`Symbiote feed`), pool capacity `25`.

Upkeep behavior:

- Pool feed upkeep scales with brood volume.
- If feed runs out, brood starts taking rot damage.
- Severe neglect damages the queen; if queen health drops too low, the pool can collapse into a hostile feral basin.

## Storage and Handling Rules

- Outside host/pool/jar, symbiotes decay quickly:
  - Prim'ta starts rotting after about `0.5` day.
  - Mature symbiotes/queens start rotting after about `1` day.
- If exposed to freezing temperatures (`<= 0C`), they can die outright.
- Storing in `symbiote spawning pool` or `canopic jar` prevents normal rot progression.

## Renewal, Removal, and Replacement

### Prim'ta renewal ritual

- `Prim'ta renewal` can swap a host's current Prim'ta with a fresh larva.
- Requires a valid pouch host and access to Prim'ta (usually from the selected pool).
- If host had a mature Prim'ta, the ritual safely ejects the resulting mature symbiote item.
- Useful for controlled lifecycle management without rolling takeover/death checks.

### Surgical extraction

- `remove Prim'ta symbiote` and `remove mature symbiote` require `Medicine 15`.
- Extraction spawns the corresponding item if the host survives.

## Mature Symbiotes: Memory and Lineage

Mature symbiotes are more than a single-use implant.

### Symbiote memory

- Symbiotes can carry skill memory from prior hosts.
- On install, current host can receive inherited skill bonuses.
- On removal, symbiote learns from host skills (best results on high-skill hosts).
- A symbiote cannot be reinstalled into a previous host.
- Transfer warnings start at `3` prior hosts; after the next transfer cycle, the symbiote burns out and perishes.

### Queen lineage

- Queens generate lineage traits that pass through Prim'ta/mature symbiotes.
- Hosts with mature symbiotes can gain lineage stat offsets (for example healing, immunity, dodge, work/sensitivity variants).
- This makes queen selection and brood tracking a real long-term optimization layer.

## Withdrawal and Tretonin Path

If a host loses Prim'ta/mature symbiote without replacement:

- They can develop `symbiote withdrawal` (often lethal if unmanaged).
- Empty pouch hosts can also spiral into `pouch degeneration` over time.

Tretonin fallback:

- Tretonin can stabilize hosts and clear withdrawal effects.
- It creates Tretonin dependency (`Tretonin addiction`/chemical need).
- Unmet Tretonin dependency becomes life-threatening.
- Later synthesis upgrades allow Tretonin production without consuming symbiotes.

## Threats You Must Plan For

- `symbiote pathogen` incidents target current hosts and can spread between nearby hosts.
- Severe infections can kill the symbiote outright before host treatment stabilizes.
- Feral pools can inflict direct ambush damage or wild takeover hediffs.

## Strategy Checklist

- Keep at least one stocked pool and one emergency canopic jar chain before scaling host count.
- Treat mature Prim'ta alerts as immediate action items, not optional warnings.
- Use rituals for planned renewals; use surgery when you need precise extraction.
- Keep Tretonin reserve for emergency withdrawals, even if you prefer symbiote-first play.
- Quarantine infected hosts during pathogen events to prevent chain spread.
- Rotate top-skill hosts through elite symbiotes carefully to build memory value before hitting host-transfer limits.

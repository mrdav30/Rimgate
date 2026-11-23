# Base Defense and Fortification

This guide covers how to build resilient defenses in Rimgate: lane control, turret doctrine, and when to transition into fortress-grade structures.

For shield-grid and ZPM power requirements, see [Power, Shields, and ZPM Infrastructure](power-shields-and-zpm-infrastructure.md).  
For raid pressure and hostile site flow, see [Incidents, Quests, and Sites](incidents-quests-and-sites.md).

## At a Glance

- Rimgate defense is strongest when you layer structures, not just stack turret DPS.
- Early value comes from cheap traffic control (`concrete barrier`, `military barrier`, fences, and gates).
- Midgame defense branches into:
  - `Tau'ri`: long-range manned ballistic emplacements.
  - `Goa'uld`: heavy plasma emplacements, then autonomous turret lines.
- `durable wall` and `durable door` variants are true fortress tools, but they are intentionally hard to undo.
- Endgame defense is best run as a package: hard shell + turret overlap + shield infrastructure.
- Enemy raids can sometimes enter through your `Dwarfgate` and temporarily lock it in incoming mode, so your gate room must be a combat-ready zone.

## Unlock Path (Recommended)

1. Build lane control first:
   - `Tau'ri manufacturing` for barriers and fence-line structures.
2. Add first serious turret tier:
   - `Tau'ri turrets I` for `manned M2 .50 cal machine gun`
   - or `staff cannon` for `manned staff cannon`
3. Transition to advanced turrets:
   - `Tau'ri turrets II` for `manned naquadah railgun` (techprint-gated)
   - `AI defense` for `auto staff cannon` (techprint-gated)
4. Move into fortress shell:
   - `ancient structures` for `durable wall` and `durable door` variants
5. Add elite automated coverage:
   - `shielded defense` for `auto staff cannon (shielded)` (requires `ancient shield technology`)

## Layered Fortification Doctrine

| Layer | Core Tools | What It Solves |
| --- | --- | --- |
| Lane shaping | `concrete barrier`, `military barrier`, `chain-link fence`, `concrete slab fence`, `chain-link gate` | Forces enemies into predictable fire corridors and improves defender uptime. |
| Main shell | `Goa'uld wall`, `hive wall` | Durable general wall lines with good anti-breach multipliers for routine perimeter defense. |
| Fortress shell | `durable wall` + durable door variants | Holds against sustained heavy pressure when you need a true inner citadel. |
| Active kill layer | turret families with overlap | Converts pathing control into actual attrition and anti-push stopping power. |
| Endgame umbrella | `ancient shield emitter` integration | Reduces projectile saturation and protects key infrastructure during major raids. |

## Fortification Comparison

| Structure | Durability Snapshot | Unlock | Practical Use |
| --- | --- | --- | --- |
| `concrete barrier` | `500 HP` | `Tau'ri manufacturing` | Cheapest early lane control and cover shaping. |
| `military barrier` | `750 HP` | `Tau'ri manufacturing` | Better frontline barrier where repeated contact is expected. |
| `chain-link fence` | `350 HP`, no roof support | `Tau'ri manufacturing` + `smithing` | Perimeter funneling with visibility; not a true hard wall. |
| `concrete slab fence` | `650 HP`, no roof support | `Tau'ri manufacturing` + `stonecutting` | Stronger fence perimeter and better stall value. |
| `Goa'uld wall` | `850 HP`, reduced `Bomb`/`Thump` damage | `Goa'uld basic` | Solid all-purpose combat wall with faction flavor. |
| `hive wall` | `1000 HP`, reduced `Bomb`/`Thump` damage | `Wraith basic` | Tough biological wall line with high baseline HP. |
| `durable wall` | `7500 HP`, not deconstructible | `ancient structures` | Endgame bunker shell; commit only after layout is finalized. |
| `durable door` family | `4000/6000/8000 HP`, slow open, powered | `ancient structures` | Vault/chokepoint control where survival matters more than traffic speed. |

## Turret Families and Use Cases

| Turret | Type | Envelope | Upkeep Profile | Best Role |
| --- | --- | --- | --- | --- |
| `manned M2 .50 cal machine gun` | Manned | Strong sustained fire, `49.9` range | Uses `steel` as shot/barrel feed | General anti-personnel lane anchor. |
| `manned naquadah railgun` | Manned | Very long reach, `99.9` range, high penetration | Uses `naquadah` feed | Long-range anti-armor and siege suppression. |
| `manned staff cannon` | Manned | Heavy plasma shot, `38` range | Uses `naquadah` feed | Mid-long lane punisher with strong impact volleys. |
| `auto staff cannon` | Autonomous | Burst plasma, `34` range | Requires both power (`250W`) and `naquadah` | Reliable autonomous anchor where manning is a bottleneck. |
| `auto staff cannon (shielded)` | Autonomous + shield | Same offensive profile with local survivability boost | Higher power draw (`600W`) + `naquadah` | High-threat bastion turret for sustained sieges. |

Operational notes:

- `M2` and `railgun` are manned emplacements; plan safe operator access and rotation.
- Autonomous staff cannons can break down, be EMP-stunned, and may explode when destroyed.
- Shielded auto staff cannons trade raw HP for a personal shield layer and higher grid demand.

## Durable Wall and Door Commitment Rules

- Treat `durable wall` and `durable door` placement as near-permanent layout decisions.
- They cannot be normally deconstructed; repositioning means destroying your own structure.
- Use them on critical rings:
  - reactor and power cores
  - hospital + biotech cores
  - gate room control zones
  - high-value stockpile vaults

## Gate Room Hardening

- Plan for gate-borne raids as an interior breach vector, not just map-edge pressure.
- Build hard cover and overlapping arcs that face the gate mouth directly.
- Keep a fallback line behind the gate room so colonists can reset without losing the whole base core.
- Avoid storing your most fragile infrastructure directly in the gate room.
- Assume the gate can stay locked in incoming mode for the duration of the raid.

## Encounter Security Features (Not Normal Base Builds)

- `proximity alarm`, `force field`, `jammed blast door`, `secret door`, and `aegis door` variants are mainly encounter/quest security content.
- Practical expectation:
  - hostile facilities may require hacking, forced opening, or trigger management.
  - proximity alarms can wake nearby stasis defenders once tripped.
- These are map obstacle mechanics, not used for player fortification progression.

## Base Layout Patterns That Work

### Pattern A: Midgame kill-lane shell

1. Build a funnel with barriers/fences and staggered corners.
2. Place 1-2 manned turrets with overlapping arcs.
3. Keep fallback doors behind the gun line for safe operator rotation.
4. Add interior reserve firing points for breach response.

### Pattern B: Late fortress core

1. Wrap critical sectors in `durable wall`.
2. Gate entrances with durable doors sized to traffic needs.
3. Anchor lanes with autonomous staff cannon pairs.
4. Add shield coverage only after confirming sustained power surplus under combat load.

## Common Mistakes

- Building expensive turret tiers before pathing and lane control are solved.
- Overusing fences as if they are full bunker walls.
- Relying on autonomous turrets without preparing power plus fuel logistics.
- Clustering explosive-end turrets too tightly and causing chain losses.
- Committing to durable wall geometry before base logistics are stable.
- Treating the gate room as logistics-only space instead of a likely combat entry point.

## Strategy Checklist

- Build path control first, then add turret DPS.
- Keep overlapping fire arcs so no lane is defended by a single emplacement.
- Pair autonomous defenses with dedicated power and `naquadah` staging.
- Reserve durable walls/doors for your true inner ring, not every exterior segment.
- Integrate shield infrastructure only after the fortification shell is already reliable.
- Keep a rapid response group near the gate room for gate-entry raids.

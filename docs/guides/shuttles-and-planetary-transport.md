# Shuttles and Planetary Transport

This guide covers player-facing shuttle transport in Rimgate: what each craft is best at, how launch fuel actually scales, and how to avoid wasting fuel and sortie windows.

For cargo staging and short-range hauling, see [Mobile Logistics and Carts](mobile-logistics-and-carts.md).  
For gate movement and wormhole logistics, see [Gate Network Guide](gate-network-guide.md).

## At a Glance

- Rimgate currently provides three player shuttles: `Tel'tak`, `Dart`, and `Puddle Jumper`.
- Shuttle builds are in Architect under `Odyssey`.
- All three launch up to `120` world tiles per trip.
- Each launch consumes `max(min fuel cost, fuel per tile * distance)`.
- Launch groups are one-shuttle only (`max1PerGroup`).
- `Tel'tak` is the heavy transport option.
- `Dart` is the fast light insert option with very low payload.
- `Puddle Jumper` is the balanced option with a built-in toggled shield.

## Unlock Path

1. `Tel'tak` research (`Rimgate_GoauldTelTak`)  
   - requires analysis of `deteriorating Tel'tak`
2. `Wraith Dart` research (`Rimgate_WraithDart`)  
   - requires analysis of `decaying Dart`
3. `Puddle Jumper` research (`Rimgate_AncientPuddleJumper`)  
   - requires analysis of `dilapitated Puddle Jumper`
   - also requires `ancient research bank` as a research facility

## Shuttle Comparison

| Shuttle | Cargo Capacity | Fuel Type | Fuel Tank | Fuel / Tile | Min Fuel / Launch | Cooldown | Max Range |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Tel'tak` | `2200` | `naquadria` | `3000` | `4.0` | `75` | `4000` ticks (`1.6h`) | `120` |
| `Dart` | `150` | `biomass` | `1500` | `2.2` | `45` | `2500` ticks (`1.0h`) | `120` |
| `Puddle Jumper` | `1100` | `naquadria` | `2500` | `2.8` | `65` | `3000` ticks (`1.2h`) | `120` |

## Launch Fuel Math

Fuel cost per trip:

- `trip fuel = max(min fuel cost, fuel per tile * world distance)`

Examples at full `120`-tile range:

- `Tel'tak`: `480` fuel
- `Dart`: `264` fuel
- `Puddle Jumper`: `336` fuel

Practical takeaway:

- Short hops are governed by minimum launch fuel.
- Long hops are governed by fuel-per-tile scaling.
- Round trips require planning both outbound and return fuel, not just the first leg.

## Role Breakdown

### `Tel'tak` (heavy logistics shuttle)

- Best for bulk payload, colony relocation chunks, and multi-pawn deployments.
- Highest transport capacity by a wide margin.
- Highest per-tile fuel burn, so route planning matters on long repeated flights.

### `Dart` (light strike/insertion shuttle)

- Best for rapid insertion, extraction, and low-mass specialist teams.
- Very low cargo capacity (`150`) means it saturates quickly with gear.
- Most fuel-efficient launch profile among the three.

### `Puddle Jumper` (balanced tactical shuttle)

- Good middle ground between capacity and fuel economy.
- Adds defensive shielding for contested operations.
- Intended for planetary/orbital transport, not dwarfgate transit.

## `Puddle Jumper` Shield Operations

- Shield is toggled via a dedicated command (pawn performs the toggle job).
- While active, it intercepts ground and air projectiles.
- Shield radius is adjustable (`4` to `6`), and larger radius increases fuel draw.
- Under sustained pressure (especially EMP), the shield can overload and go offline until reset.

Operational rule:

- Keep shield off during safe transit/loading windows, then toggle on before entering combat-risk zones.

## Build and Pad Planning

- All shuttles must be placed unroofed.
- All are large-footprint structures and should be given clear takeoff/landing lanes.
- Staging layout that works:
  - nearby fuel stockpile matched to shuttle fuel type
  - nearby armory and medicine shelves for fast loadouts
  - direct route to your gate room or perimeter exit

## Common Mistakes

- Choosing `Dart` for cargo-heavy missions and hitting mass cap immediately.
- Launching on minimum fuel without budgeting return legs.
- Treating cooldown as negligible and overcommitting to back-to-back sorties.
- Leaving `Puddle Jumper` shield active continuously and bleeding fuel between fights.
- Mixing `naquadria` and `biomass` fueling lanes and creating refuel bottlenecks.

## Strategy Checklist

- Use `Tel'tak` for freight-heavy or multi-colonist sorties.
- Use `Dart` for fast low-mass operations where speed matters more than payload.
- Use `Puddle Jumper` when you expect hostile projectile pressure.
- Plan launch fuel with round-trip math before committing distant flights.
- Build dedicated refuel pads and keep the correct fuel staged next to each shuttle class.

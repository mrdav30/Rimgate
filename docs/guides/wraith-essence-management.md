# Wraith Essence Management

This guide covers the Wraith essence loop: how essence is gained/spent, which abilities participate in the loop, and how auto-feeding now works with both pods and prisoners.

For biomass production and biomass power, see [Wraith Biomass and Cocoon Operations](wraith-biomass-and-cocoon-operations.md).
For overall unlock flow, see [Research and Progression](research-and-progression.md).
For cloning biomass demand, see [Cloning Systems](cloning-systems.md).

## Essence Resource Basics

`essence metabolism` (`Rimgate_WraithEssenceMetabolism`) is the core Wraith resource gene:

- Disables the normal `Food` need.
- Adds an essence resource gizmo (`resourceGizmoType`: `Rimgate.GeneGizmo_ResourceEssence`).
- Decays at baseline `0.02` per day (`resourceLossPerDay`).
- Applies escalating `Essence deficit` pressure when essence is low.

Operationally, Wraith pawns act against a configurable target level in the gizmo. Auto-feed logic only triggers when essence is below that target.

## Essence Abilities and Costs

The table below summarizes Wraith abilities tied to essence management.

| Ability | Source Gene | Essence Cost | Essence Gain | Cooldown | Practical Use |
| --- | --- | --- | --- | --- | --- |
| `drain life` (`Rimgate_WraithLifeDrainAbility`) | `essence metabolism` | `0` | `+0.25` base (`essenceGainAmount`) | `30000` ticks | Main refill tool; can target pawns and filled cocoon pods in melee. |
| `web shot` (`Rimgate_WraithCocoonTrapAbility`) | `webbing protein glands` | `0.1` | None | `420` ticks | Combat control; indirect essence support by creating safer captures. |
| `cocoon wrap` (`Rimgate_WraithCocoonWrapAbility`) | `webbing protein glands` | `0.4` | None | `1800~2400` ticks | Converts a colony prisoner into a cocoon pod target. |
| `regenerative trance` (`Rimgate_WraithEssenceMendingAbility`) | `essence metabolism` | `0.45` | None | `90000` ticks | Converts stored essence into long-form self-repair utility. |

Notes:

- `drain life` is the only direct essence gain action in this set.
- `regenerative trance` uses deferred payment (`payCostAtStart=false`), so essence is charged when the job completes successfully.

## Feeding Targets

Wraith can refill from two target types:

- **Filled cocoon pods** (`Rimgate_WraithCocoonPod`)
- **Colony prisoners**

Both routes use the same `drain life` ability for cooldown and effect resolution.

## Auto-Feed Behavior

When essence is below target and `drain life` is castable:

1. If `auto use pods` is enabled, AI searches for a reachable filled cocoon pod first.
2. If no pod is selected and `auto use prisoners` is enabled, AI searches reachable colony prisoners and casts `drain life`.
3. If both are disabled, no autonomous essence feeding is attempted.

This means pods are always prioritized over prisoners when both toggles are enabled.

## Per-Pawn Toggle Controls

The essence gizmo now has two separate auto-feed toggles:

- `auto use pods` for filled cocoon pod feeding.
- `auto use prisoners` for prisoner feeding fallback (Baseliner icon).

Each toggle is stored per pawn through the `Gene_WraithEssenceMetabolism` state:

- `FilledPodsAllowed`
- `PrisonersAllowed`

## Practical Setup

- Keep at least one valid prisoner lane available even when pods are part of your primary loop.
- Enable pod automation on core Wraith fighters to reduce micromanagement.
- Disable prisoner automation on pawns you do not want entering prison blocks.
- Treat `regenerative trance` as planned spending; it competes with emergency drain reserves.

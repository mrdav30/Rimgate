# Mobile Logistics and Carts

This guide covers Rimgate's mobile hauling system: how carts are unlocked, how to load them, how to push them efficiently, and when to use refrigerated or gate-transfer workflows.

For gate operations, see [Gate Network Guide](gate-network-guide.md).  
For overall unlock sequencing, see [Research and Progression](research-and-progression.md).

## At a Glance

- Carts are short-range logistics multipliers, not caravan vehicles.
- Every cart uses the same load radius (`10` tiles from the cart).
- Loading uses `Hauling` work; designated pushing uses `Basic` work.
- `push to...` keeps cargo loaded; `push and dump...` unloads at destination.
- Enabled carts can be used as mobile stockpiles for construction materials and food.
- Refrigerated carts preserve perishables only while fueled.
- You can push a loaded cart through an active gate for map-to-map transfer.

## Unlock Path

1. `wheelbarrow` via `smithing`.
2. `tracked cart` via `tracked cart` research (`Rimgate_TrackedCart`).
3. Refrigeration upgrade via `refrigerated lining` (`Rimgate_RefrigeratedCarts`).
4. `hover skid` tier via `hover skid` (`Rimgate_HoverSkid`, Goa'uld branch, techprint-gated).

## Cart Comparison

| Cart | Capacity | Push Speed Penalty | Fuel Model | Notes |
| --- | --- | --- | --- | --- |
| `wheelbarrow` | `136 kg` | Heavy (`Rimgate_PushingCart` severity `0.85`) | None | Cheapest early option; no breakdown/fuel complexity. |
| `tracked cart` | `320 kg` | Medium (`0.55`) | `chemfuel`, push drain scales with load | Strong midgame hauler. |
| `refrigerated tracked cart` | `300 kg` | Medium (`0.55`) | `chemfuel`, continuous refrigeration drain + load-scaled push drain | Preserves perishables while fueled. |
| `hover skid` | `520 kg` | Light (`0.15`) | `naquadah`, push drain scales with load | Highest raw capacity; explosive on kill. |
| `refrigerated hover skid` | `500 kg` | Light (`0.15`) | `naquadah`, continuous refrigeration drain + load-scaled push drain | Best cold-chain hauler; explosive on kill. |

## Fuel and Runtime Notes

- Push fuel use is load-scaled with a baseline: carts always consume at least the listed base push rate while moving, and at full load consume about double that base rate.
- Non-refrigerated carts (`tracked cart`, `hover skid`) consume fuel only while being pushed.
- Refrigerated carts consume fuel continuously when parked to maintain preservation, and also use load-scaled push fuel while moving.
- If a refrigerated cart runs out of fuel, sealed preservation drops and normal spoilage ticking resumes.
- If fuel runs out mid-push, the push job fails and the cart is restored nearby.
- Full-tank baseline endurance:
  - `tracked cart`: base `67.5/day` while moving (`15` fuel = `~5.3` in-game hours at empty load), up to `135/day` at full load (`~2.7` in-game hours)
  - `hover skid`: base `57.5/day` while moving (`10` fuel = `~4.2` in-game hours at empty load), up to `115/day` at full load (`~2.1` in-game hours)
  - `refrigerated tracked cart`: `25` at `5/day` (`~5` in-game days)
  - `refrigerated hover skid`: `25` at `4/day` (`~6.25` in-game days)

## Loading Workflow

1. Select cart and click `Load`.
2. Build a load manifest from reachable, non-forbidden items within `10` tiles.
3. Keep total requested mass at or below cart capacity.
4. Confirm; haulers begin `load cart` jobs automatically.
5. When loading finishes, you get a completion message.

Important behavior:

- The system can partially complete manifests.
- If no pawn can continue loading for about `10` seconds, the manifest auto-finalizes and remaining shortfall is reported.
- You can cancel loading at any time with `Cancel load`.
- `Unload` drops all contents near the cart.
- `Allow use cart contents` toggles whether colonists can pull items directly from the cart without unloading.
- Carts cannot be pushed while a load manifest is active.

## Using Cart Contents Without Dumping

### `Allow use cart contents` toggle

- Each cart has a per-cart allow/forbid toggle for direct access.
- `Allow` means colonists can treat the cart as a valid source for supported jobs.
- `Forbid` means the cart behaves like sealed transit storage until manually unloaded.
- The setting persists when the cart is pushed and restored after transit.

### Construction materials

- If a cart is set to `Allow`, builders can pull required resources directly from it.
- Blueprint placement/readouts and right-click construct checks treat enabled cart contents as available resources.
- This removes the need to pre-dump cargo before construction work begins.

### Food access

- If a cart is set to `Allow`, hungry colonists can take ingestible food directly from the cart.
- This supports practical field use (for example, survival meals left in a working cart).

## Pushing Modes

### `push to...`

- Moves the cart to a chosen destination cell.
- Cargo stays in the cart.
- Usable for repositioning stock near production, killboxes, or gate rooms.

### `push and dump...`

- Moves the cart and immediately dumps contents near destination.
- Best for one-way warehouse refill or field resupply.

### Pushing through a gate

- Use `push to...`, then target an active gate.
- Requirements:
  - gate must be active
  - iris must be open
  - pusher must be able to reach gate interaction cell
- Result: cart and pusher are transferred through the gate, then respawn on the connected map.
- `push and dump...` is for cell destinations, not gate transfer.

## Work Priority and Control

- `load cart` is `Hauling` work type.
- `push cart` is `Basic` work type.
- Manual float-menu push options are available for direct pawn orders.
- Cart work can be issued in drafted contexts, but regular automation still depends on your work priorities and availability.

## Common Mistakes

- Building manifests from items outside the `10`-tile load radius.
- Setting up load manifests without enough `Hauling` priority.
- Expecting `Basic`-disabled pawns to execute push designations.
- Starting long pushes with low fuel and stalling mid-route.
- Using `push and dump...` when you meant to preserve cargo for gate transfer.
- Forgetting a cart is set to `Forbid` and expecting builders/eaters to pull from it.
- Treating carts as caravan cargo animals (they are explicitly non-caravan).

## Strategy Checklist

- Keep at least one `wheelbarrow` early to flatten hauling spikes in construction phases.
- Use `tracked cart` for dense base logistics; step up to `hover skid` for mega-hauls.
- Reserve refrigerated variants for food/medicine lanes where spoilage prevention pays for fuel.
- Keep a fuel stockpile near high-traffic cart corridors.
- Pair one cart lane with each high-volume cluster (freezer, fabrication, hospital, gate room).
- For gate logistics, pre-stage loaded carts near the DHD/gate to reduce transfer delays.
- Keep high-throughput carts on `Allow` when you want them to function as mobile stockpiles.

# Wraith Biomass and Cocoon Operations

This guide focuses on the Wraith `biomass` economy: growing protein, cocoon-processing meat, and converting output into power and downstream systems like cloning.

For essence feeding, drain abilities, and prisoner/pod automation, see [Wraith Essence Management](wraith-essence-management.md).
For broader unlock flow, see [Research and Progression](research-and-progression.md).
For grid scaling and endgame power defense, see [Power, Shields, and ZPM Infrastructure](power-shields-and-zpm-infrastructure.md).
For deep cloning mechanics, see [Cloning Systems](cloning-systems.md).

## At a Glance

- `meat lab` is your reliable Wraith protein source and does not depend on sunlight or blight safety.
- `cocoon spot` turns raw meat into preserved `cocooned meat`, then into `biomass`.
- Cocoon bills are Wraith-specialized: only pawns with `webbing protein glands` can run them.
- `biomass generator` gives strong midgame power (`3000W`) with simple fuel handling.
- `biomass` demand spikes hard once you start large-scale cloning cycles.

## Unlock Path (Recommended)

1. Finish `Wraith science` (`Rimgate_WraithBasic`).
2. Research `meat cultivation` (`Rimgate_MeatCultivation`) for `meat lab` and `cocoon spot` bills.
3. Research `biomass generator` (`Rimgate_WraithGenerator`) to convert biomass into stable power.
4. Add `cloning` (`Rimgate_WraithCloneGenome`) when you are ready for heavy biomass spending.
5. Finish `advanced bioengineering` (`Rimgate_BiomassOptimization`) for key efficiency upgrades.

## Biomass Production Loop

### 1) Grow cultured meat

`meat lab` basics:

- Build cost: `100 biomass`, `2 industrial components`
- Power draw: `450W` (`270W` after `advanced bioengineering`)
- Crop output: `28 cultured meat` every `3.5` days
- Crop is configured to ignore normal blight/sunlight/vacuum failure patterns

### 2) Cocoon and preserve

`cocoon spot` basics:

- Build cost: effectively free spot placement
- Work type: `Crafting`
- Work skill used by recipes: `Cooking`
- Worker requirement: pawn must have `webbing protein glands` (`Rimgate_WraithWebbingGene`)

This step is your preservation layer: `cocooned meat` has no rot deterioration and stores cleanly for large batch processing.

### 3) Convert to biomass

| Recipe | Inputs | Output | Notes |
| --- | --- | --- | --- |
| `make Biomass from cocooned meat` | `5 cocooned meat` + `1 naquadah` | `10 biomass` | Best conversion path for stockpiled cocooned batches. |
| `make Biomass from cocooned meat x4` | `20 cocooned meat` + `4 naquadah` | `40 biomass` | Better for fewer pawn work interruptions. |
| `make Biomass from meat` | `10 nutrition` of raw meat + `1 naquadah` | `10 biomass` | Flexible fallback when you have mixed raw meat but no cocoon stock. |
| `make Biomass from meat x4` | `40 nutrition` of raw meat + `4 naquadah` | `40 biomass` | Bulk fallback variant. |

Practical takeaway:

- The direct raw-meat recipe is nutrition-based.
- The cocooned recipe is fixed-count based.
- In normal play, cocooned processing is usually the more efficient long-run path.

## Biomass Power Operations

`biomass fired generator` (`Rimgate_WraithGenerator`) is your main biomass sink before cloning scale-up:

- Output: `3000W` base, `3600W` after `advanced bioengineering`
- Fuel usage: `0.75 biomass/day`
- Fuel tank: `25 biomass` (about `33.3` in-game days at full tank)

This makes biomass power very stable if your cocoon/meat throughput is consistent and `naquadah` supply is secure.

## Cloning Tie-In

- Cloning uses the same `biomass` economy, so it competes directly with generators and gear production.
- `advanced bioengineering` is partially gated behind the cloning branch (`Rimgate_WraithCloneGenome` prerequisite).
- Treat cloning as phase two of your biomass strategy, not day-one consumption.

Detailed pod modes, clone types, and optimization facilities are covered in [Cloning Systems](cloning-systems.md).

## Common Mistakes

- Assigning only `Cooking` and forgetting cocoon bills run under `Crafting` work type.
- Expecting non-Wraith colonists to run cocoon bills without the webbing gene.
- Burning all biomass on gear before power and cloning infrastructure is stable.
- Ignoring naquadah logistics while scaling biomass conversion.

## Strategy Checklist

- Designate at least one dedicated webbing-gene crafter for cocoon station throughput.
- Keep a preserved `cocooned meat` reserve before major raid windows or power spikes.
- Stabilize biomass power first, then scale into cloning draw.
- Keep naquadah as a protected supply line, not an afterthought input.

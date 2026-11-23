# Cloning Systems

This guide covers the player-facing cloning loop in Rimgate: unlocking cloning, running pods safely, managing biomass and power costs, and understanding failure outcomes before they ruin a run.

For the biomass economy feeding this system, see [Wraith Biomass and Cocoon Operations](wraith-biomass-and-cocoon-operations.md).  
For global progression gates, see [Research and Progression](research-and-progression.md).  
For random world pressure, see [Incidents, Quests, and Sites](incidents-quests-and-sites.md).

## At a Glance

- Cloning is a high-power, high-biomass system built around the `cloning pod`.
- Mode unlocks are research-gated (`genome` -> `full` -> `enhanced` -> `reconstruction`).
- Calibration needs a qualified pawn (default `Medicine 10` minimum).
- `biomass stabilizers` reduce biomass cost but degrade and must be replaced.
- `cyclic wave inducer` speeds cloning cycles if powered.
- Minor failures are enabled by default; major failures are optional but dangerous.

## Unlock Path

1. Progress Wraith research to `Wraith science` (`Rimgate_WraithBasic`).
2. Analyze a recovered `Wraith cloning pod` object to unlock `cloning` (`Rimgate_WraithCloneGenome`).
3. Build your own `cloning pod` after `cloning` research is complete.
4. Extend modes with:
   - `full-body replication` (`Rimgate_WraithCloneFull`)
   - `clone enhancement` (`Rimgate_WraithCloneEnhancement`)
   - `reconstruction` (`Rimgate_WraithCloneCorpse`)
5. Add `cloning pod optimization` (`Rimgate_CloningPodOptimization`) for major efficiency gains.

## Core Infrastructure

### Cloning pod (base)

- Build cost: `140 steel`, `100 biomass`, `6 industrial components`, `4 naquadria`, `2 spacer components`
- Power draw: `9200W` active, `500W` idle
- Power after optimization research: `5980W` active, `325W` idle
- Biomass tank: `250`
- Biomass is only consumed when a cycle actually starts

Important:

- Pod can hold living pawns or corpses, but clone generation only succeeds for `humanlike` targets.
- Putting in invalid targets (for example non-humanlike flesh) can waste setup time and fail at calibration.

### Optimization facilities

| Facility | Effect | Limits | Cost to run |
| --- | --- | --- | --- |
| `biomass stabilizer` | Reduces biomass needed per cycle | Up to `4`, adjacent placement | `5W` each |
| `cyclic wave inducer` | Speeds calibration and incubation | `1` per pod, up to `12.9` tiles | `200W` active (`40W` idle) |

Stabilizer notes:

- Default reduction is `5%` each (`20%` at 4 units, settings can change this).
- Hard cap is `50%` total reduction.
- Stabilizers are non-repairable and decay during use.

## Clone Modes (Default Settings)

Defaults below assume unchanged mod settings.

| Mode | Unlock | Host Type | Biomass Cost | Calibration | Base Incubation |
| --- | --- | --- | --- | --- | --- |
| `Genome` | `cloning` | Living pawn | `100` | `4500` ticks (`1.8h`) | `120000` ticks (`2.0d`) |
| `Full` | `full-body replication` | Living pawn | `150` | `6750` ticks (`2.7h`) | `180000` ticks (`3.0d`) |
| `Enhanced` | `clone enhancement` | Living pawn | `200` | `9000` ticks (`3.6h`) | `240000` ticks (`4.0d`) |
| `Reconstruction` | `reconstruction` | Corpse | `250` | `11250` ticks (`4.5h`) | `300000` ticks (`5.0d`) |

Incubation time then shifts again by calibration outcome:

- No issues: `x0.75` (faster)
- Minor issues: `x1.25`
- Major issues: `x1.5`

## What Clones Inherit

### Shared baseline

- Clones copy host genes (xeno + endogenes).
- Clones receive visible `Generation X clone` tracking.
- Clones usually spawn with `clone pod sickness` and `clone activation shock`.
- `clone activation shock` hard-limits consciousness short-term, so post-spawn medical support is mandatory.

### By mode

- `Genome`:
  - Starts as a replicant profile (new learning-focused setup)
  - Skills reset, then gets random minor passions
- `Full`:
  - Closest to original profile
  - Keeps passions, skills are reduced by default (`80%`, or full if `NoSkillLoss` setting is enabled)
- `Enhanced`:
  - Combat-biased profile (fixed high combat focus)
  - Gets the `enduring` hediff bonus package
- `Reconstruction`:
  - Corpse-based recovery route
  - More expensive and slower than other modes

Additional risk:

- Clones can receive `cellular degradation` gene as a side effect (chance varies by mode).

## Failure Model

Default behavior:

- Minor failures: enabled, default `10%` chance
- Major failures: disabled by default (can be enabled in settings)

Minor failure pool:

- Gender swap
- Albino mutation package
- Bonus random genes
- Randomized trait package

Major failure pool (if enabled):

- Clone dies on emergence
- Host dies during process (living-host modes)
- Damaged clone profile
- Insane clone profile (can emerge berserk)

Reconstruction exception:

- Host-death major failure is removed for corpse reconstruction runs.

## Operating Workflow

1. Select clone mode on the pod (`Genome`, `Full`, `Enhanced`, or `Reconstruction` when unlocked).
2. Load a valid subject:
   - Living pawn for normal modes
   - Corpse for reconstruction
3. Assign calibration.
   - Calibration uses the cloning workgiver and requires meeting medicine threshold.
4. On calibration finish, host is ejected and incubation begins (if biomass is available).
5. Pod consumes biomass at incubation start, then runs until emergence.
6. Clone spawns at pod interaction cell with outcome letter + side effects.

## Interruption Behavior

- No biomass at incubation start: pod goes `Paused` until refueled.
- Power loss during calibration: cycle fails and must be restarted.
- Power loss during incubation: progress pauses (does not advance while unpowered).
- `Abort incubation` command: cancels cycle and discards pending clone.
- Manual `Eject` command: removes occupant and resets cycle state.

## Incident Risk

If clone incidents are enabled, `mass clone failure` can occur and kill all pawns flagged as clones.

Practical meaning:

- Clone-heavy colonies should treat that setting as a major difficulty lever.

## Common Mistakes

- Turning on cloning before your grid can handle sustained `~9kW` active draw.
- Forgetting that stabilizers degrade and cannot be repaired.
- Running reconstruction without enough biomass reserve for full cycle completion.
- Using low-skill calibrators and assuming failures are impossible.
- Ignoring `Basic` work priority and wondering why calibration never starts.

## Strategy Checklist

- Build cloning in phases: power reserve -> biomass reserve -> first pod -> optimization facilities.
- Keep one pod lane for stable production and one lane for riskier experiments.
- Use stabilizers for expensive modes (`Enhanced`, `Reconstruction`) where savings matter most.
- Keep inducer powered continuously during active runs to avoid timeline drift.
- Decide early whether clone incidents stay enabled and plan colony doctrine around that choice.
